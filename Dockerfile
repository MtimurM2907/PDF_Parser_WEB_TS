FROM node:20 AS frontend

WORKDIR /workspace

# Копируем весь репозиторий (клиенту нужны package.json и исходники)
COPY . .

WORKDIR /workspace/pdf_parser_web_ts.client

# Сборка фронтенда. Vite сконфигурирован так, что outDir указывает в wwwroot бэкенда.
RUN npm ci && npm run build


# -----------------------------
# Этап сборки backend (.NET 8)
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /workspace

# Устанавливаем Node.js и npm, т.к. Microsoft.VisualStudio.JavaScript.Sdk
# при публикации проекта проверяет наличие node/npm и без них publish падает.
RUN apt-get update && apt-get install -y nodejs npm

COPY . .

# Подменяем wwwroot результатом сборки фронтенда
COPY --from=frontend /workspace/PDF_Parser_WEB_TS.Server/wwwroot ./PDF_Parser_WEB_TS.Server/wwwroot

# Очищаем возможные артефакты прошлых сборок (bin/obj), чтобы
# Static Web Assets не ссылались на несуществующие файлы.
RUN rm -rf ./PDF_Parser_WEB_TS.Server/bin ./PDF_Parser_WEB_TS.Server/obj

RUN dotnet restore PDF_Parser_WEB_TS.Server/PDF_Parser_WEB_TS.Server.csproj
RUN dotnet publish PDF_Parser_WEB_TS.Server/PDF_Parser_WEB_TS.Server.csproj -c Release -o /app/publish


# -----------------------------
# Финальный рантайм-образ
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

# Render по умолчанию ожидает, что сервис будет слушать порт из переменной PORT.
# Мы используем 10000 (безопасный порт >1024) и на Render укажем PORT=10000.
ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "PDF_Parser_WEB_TS_Server.dll"]

