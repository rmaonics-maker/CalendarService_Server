# 1. СБОРКА (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["tests/NotificationService/NotificationService.csproj", "tests/NotificationService/"]
COPY ["src/Contracts/Calendar.Contracts.csproj", "src/Calendar.Contracts/"]

RUN dotnet restore "tests/NotificationService/NotificationService.csproj"

COPY . .

WORKDIR "/src/tests/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. ЗАПУСК (Runtime Stage)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]