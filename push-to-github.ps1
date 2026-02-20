# Скрипт: первый push проекта в GitHub
# Запускайте после установки Git и перезапуска терминала.
# Репозиторий: https://github.com/MtimurM2907/PDF_Parser_WEB_TS

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Проверка Git..." -ForegroundColor Cyan
$gitVersion = git --version 2>$null
if (-not $gitVersion) {
    Write-Host "Ошибка: Git не найден. Установите Git с https://git-scm.com/download/win и перезапустите терминал." -ForegroundColor Red
    exit 1
}
Write-Host $gitVersion -ForegroundColor Green

if (-not (Test-Path .git)) {
    Write-Host "`nИнициализация репозитория..." -ForegroundColor Cyan
    git init
}

Write-Host "`nДобавление файлов..." -ForegroundColor Cyan
git add -A

Write-Host "`nСтатус (проверьте, что нет appsettings.Production.json с ключами):" -ForegroundColor Yellow
git status

Write-Host "`nСоздание коммита..." -ForegroundColor Cyan
git commit -m "Исходный проект PDF Parser"

Write-Host "`nВетка main..." -ForegroundColor Cyan
git branch -M main

$remote = "origin"
$url = "https://github.com/MtimurM2907/PDF_Parser_WEB_TS.git"
try {
    git remote get-url $remote 2>$null
    Write-Host "`nУдалённый репозиторий уже задан: $remote" -ForegroundColor Yellow
} catch {
    Write-Host "`nДобавление remote $remote..." -ForegroundColor Cyan
    git remote add origin $url
}

Write-Host "`nОтправка на GitHub (потребуется авторизация)..." -ForegroundColor Cyan
git push -u origin main

Write-Host "`nГотово." -ForegroundColor Green
