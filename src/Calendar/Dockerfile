# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY ["src/Calendar/Calendar_Service.csproj", "src/Calendar/"]
COPY ["src/Contracts/Calendar.Contracts.csproj", "src/Contracts/"]

# 2. Исправляем путь восстановления
RUN dotnet restore "src/Calendar/Calendar_Service.csproj"

# 3. Копируем всё остальное
COPY . .

# 4. Переходим в правильную рабочую директорию внутри контейнера
WORKDIR "/src/src/Calendar"

# 5. Собираем и публикуем
RUN dotnet build "Calendar_Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Calendar_Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
USER app

COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Calendar_Service.dll"]