using Microsoft.EntityFrameworkCore;
using MassTransit;
using Calendar.Auth.API.Data;
using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Identity;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(providerBuilder => providerBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("Calendar.Auth.Api"))
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();
    
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetIntrospectionEndpointUris("connect/introspect")
            .SetUserInfoEndpointUris("connect/userinfo");

        // Фиксированный issuer — чтобы токены были валидны
        // как для клиентов внутри Docker-сети, так и с хоста.
        var issuer = builder.Configuration["OpenIddict:Issuer"];
        if (!string.IsNullOrEmpty(issuer))
        {
            options.SetIssuer(new Uri(issuer));
        }

        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();

        // Resource Owner Password Credentials — только для dev/load-тестов.
        if (builder.Environment.IsDevelopment())
        {
            options.AllowPasswordFlow();
        }

        options.RequireProofKeyForCodeExchange();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        options.DisableAccessTokenEncryption();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
        
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AuthDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
        o.QueryDelay = TimeSpan.FromSeconds(2);
        o.QueryMessageLimit = 100;
    });

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseEntityFrameworkOutbox<AuthDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "127.0.0.1";
        var username = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication(); 
app.UseAuthorization();  
app.MapPrometheusScrapingEndpoint();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    var context = services.GetRequiredService<AuthDbContext>();
    await context.Database.MigrateAsync();
    
    // А) Создаем роли
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Б) Создаем тестового пользователя
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = "admin@calendar.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(adminUser, "Password123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // В) Создаем scopes
    var scopeManager = services.GetRequiredService<OpenIddict.Abstractions.IOpenIddictScopeManager>();
    foreach (var scopeName in new[] { "calendar_api", "booking_api" })
    {
        if (await scopeManager.FindByNameAsync(scopeName) is null)
        {
            await scopeManager.CreateAsync(new OpenIddict.Abstractions.OpenIddictScopeDescriptor
            {
                Name = scopeName,
                DisplayName = scopeName
            });
        }
    }

    // Г) Создаем клиента для Postman 
    var manager = services.GetRequiredService<OpenIddict.Abstractions.IOpenIddictApplicationManager>();
    if (await manager.FindByClientIdAsync("postman_client") is null)
    {
        await manager.CreateAsync(new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
        {
            ClientId = "postman_client",
            ClientSecret = "postman-secret",
            DisplayName = "Postman Testing Client",
            RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
            Permissions =
            {
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.ResponseTypes.Code, 
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "booking_api",
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "calendar_api",
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "roles",
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "email",
                OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "profile"
            }
        });
    }

    // Д) Dev-only клиент для нагрузочных тестов (ROPC grant)
    // Upsert: если клиент уже создан предыдущим запуском, синхронизируем его
    // permissions с актуальным списком scope-правил. Иначе добавленный позже scope
    // (например booking_api) не сохранится, и /connect/token вернёт ID2051
    // "This client application is not allowed to use the specified scope".
    if (app.Environment.IsDevelopment())
    {
        var requiredPermissions = new[]
        {
            OpenIddict.Abstractions.OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddict.Abstractions.OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddict.Abstractions.OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "calendar_api",
            OpenIddict.Abstractions.OpenIddictConstants.Permissions.Prefixes.Scope + "booking_api"
        };

        var existing = await manager.FindByClientIdAsync("loadtest_client");
        if (existing is null)
        {
            var createDescriptor = new OpenIddict.Abstractions.OpenIddictApplicationDescriptor
            {
                ClientId = "loadtest_client",
                ClientSecret = "loadtest-secret",
                DisplayName = "NBomber Load Test Client"
            };
            foreach (var permission in requiredPermissions)
            {
                createDescriptor.Permissions.Add(permission);
            }
            await manager.CreateAsync(createDescriptor);
        }
        else
        {
            var descriptor = new OpenIddict.Abstractions.OpenIddictApplicationDescriptor();
            await manager.PopulateAsync(descriptor, existing);

            var changed = false;
            foreach (var permission in requiredPermissions)
            {
                if (!descriptor.Permissions.Contains(permission))
                {
                    descriptor.Permissions.Add(permission);
                    changed = true;
                }
            }

            if (changed)
            {
                await manager.PopulateAsync(existing, descriptor);
                await manager.UpdateAsync(existing);
            }
        }
    }
}

app.Run();