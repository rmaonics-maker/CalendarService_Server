FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["tests/Calendar.LoadTests/Calendar.LoadTests.csproj", "tests/Calendar.LoadTests/"]
RUN dotnet restore "tests/Calendar.LoadTests/Calendar.LoadTests.csproj"
COPY . .
WORKDIR "/src/tests/Calendar.LoadTests"
RUN dotnet build "./Calendar.LoadTests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Calendar.LoadTests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Calendar.LoadTests.dll"]
