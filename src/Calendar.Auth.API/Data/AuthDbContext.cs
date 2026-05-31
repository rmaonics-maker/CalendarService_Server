using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MassTransit;
using Calendar.Auth.API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Calendar.Auth.API.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) 
: IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.UseOpenIddict();

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Replace table names
            var tableName = entity.GetTableName();
            if (tableName != null)
            {
                entity.SetTableName(ConvertToSnakeCase(tableName));
            }

            // Replace column names
            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema()));
                if (columnName != null)
                {
                    property.SetColumnName(ConvertToSnakeCase(columnName));
                }
            }

            // Replace keys
            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName != null)
                {
                    key.SetName(ConvertToSnakeCase(keyName));
                }
            }

            // Replace foreign keys
            foreach (var key in entity.GetForeignKeys())
            {
                var keyName = key.GetConstraintName();
                if (keyName != null)
                {
                    key.SetConstraintName(ConvertToSnakeCase(keyName));
                }
            }

            // Replace indexes
            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName != null)
                {
                    index.SetDatabaseName(ConvertToSnakeCase(indexName));
                }
            }
        }
    }

    private string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscore = System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2");
        return startUnderscore.ToLower();
    }
}