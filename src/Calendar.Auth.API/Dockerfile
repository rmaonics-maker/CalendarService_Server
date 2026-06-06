# 1. Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY ["src/Calendar.Auth.API/Calendar.Auth.API.csproj", "src/Calendar.Auth.API/"]
COPY ["src/Contracts/Calendar.Contracts.csproj", "src/Calendar.Contracts/"]

RUN dotnet restore "src/Calendar.Auth.API/Calendar.Auth.API.csproj"

COPY . .

WORKDIR "/src/src/Calendar.Auth.API"

RUN dotnet build "Calendar.Auth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Calendar.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app
USER app

COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8081

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8081/health || exit 1

ENTRYPOINT ["dotnet", "Calendar.Auth.API.dll"]