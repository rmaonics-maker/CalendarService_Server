# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["src/Calendar/Calendar.csproj", "src/Calendar/"]

# 2. Исправляем путь восстановления
RUN dotnet restore "src/Calendar/Calendar.csproj"

# 3. Копируем всё остальное
COPY . .

# 4. Переходим в правильную рабочую директорию внутри контейнера
WORKDIR "/app/src/Calendar"

# 5. Собираем и публикуем
RUN dotnet build "Calendar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Calendar.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
USER app

COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Calendar.dll"]