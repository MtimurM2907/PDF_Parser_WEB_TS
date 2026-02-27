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

COPY . .

# Подменяем wwwroot результатом сборки фронтенда
COPY --from=frontend /workspace/PDF_Parser_WEB_TS.Server/wwwroot ./PDF_Parser_WEB_TS.Server/wwwroot

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

