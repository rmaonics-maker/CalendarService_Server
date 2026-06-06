# 1. СБОРКА (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["src/Contracts/Calendar.Contracts.csproj", "src/Calendar.Contracts/"]

RUN dotnet restore "src/NotificationService/NotificationService.csproj"

COPY . .

WORKDIR "/src/src/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. ЗАПУСК (Runtime Stage)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD pgrep -f dotnet || exit 1
ENTRYPOINT ["dotnet", "NotificationService.dll"]