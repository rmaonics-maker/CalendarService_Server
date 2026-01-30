# Stage 1: Build
# Используем полноценный SDK для сборки (тут есть шелл и утилиты)
FROM [mcr.microsoft.com/dotnet/sdk:10.0](https://mcr.microsoft.com/dotnet/sdk:10.0) AS build
WORKDIR /src

# Сначала копируем только файлы проекта и восстанавливаем зависимости.
COPY ["Calendar/Calendar.csproj", "Calendar/"]
RUN dotnet restore "Calendar/Calendar.csproj"

# Копируем остальной код
COPY . .
WORKDIR "/src/Calendar"
RUN dotnet build "Calendar.csproj" -c Release -o /app/build

# Публикуем. Флаг UseAppHost=false важен, чтобы не создавать лишний exe-файл,
# так как мы будем запускать dll напрямую.
FROM build AS publish
RUN dotnet publish "Calendar.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime - Chiseled
# Используем тег noble-chiseled (Ubuntu 24.04).
FROM [mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled](https://mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled) AS final
WORKDIR /app

# В Chiseled образах обычно уже настроен non-root user (с UID 1654), 
# но для явности и совместимости с K8s SecurityContext лучше указать USER.
# В .NET 8+ chiseled образах пользователь 'app' (UID 1654) уже создан.
USER app

COPY --from=publish /app/publish .

# Настройка порта. Для non-root пользователей порты < 1024 недоступны.
# .NET по умолчанию слушает 8080.
ENV ASPNETCORE_HTTP_PORTS=8080

# CRITICAL: Только exec-форма! Shell-форма не сработает, так как нет /bin/sh.
ENTRYPOINT ["dotnet", "Calendar.dll"]