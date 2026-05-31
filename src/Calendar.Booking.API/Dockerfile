# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Calendar.Booking.API/Calendar.Booking.API.csproj", "src/Calendar.Booking.API/"]
COPY ["src/Contracts/Calendar.Contracts.csproj", "src/Contracts/"]

RUN dotnet restore "src/Calendar.Booking.API/Calendar.Booking.API.csproj"

COPY . .

WORKDIR "/src/src/Calendar.Booking.API"

RUN dotnet build "Calendar.Booking.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Calendar.Booking.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
USER app

COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Calendar.Booking.API.dll"]
