# Деплой приложения через GitHub и Beget

Краткая пошаговая инструкция: как выложить проект в GitHub и развернуть его на хостинге Beget.

---

## Часть 1. Настройка GitHub

### 0. Установка Git (если команда `git` не найдена)

В PowerShell ошибка **«Имя "git" не распознано»** означает, что Git не установлен или не добавлен в PATH.

**Способ 1 — установщик (рекомендуется):**

1. Скачайте установщик: [https://git-scm.com/download/win](https://git-scm.com/download/win) (или [Git for Windows](https://gitforwindows.org/)).
2. Запустите установщик, при установке можно оставить настройки по умолчанию (в том числе пункт **«Add Git to PATH»**).
3. **Закройте и заново откройте терминал** (или перезапустите Cursor/VS Code), чтобы подхватился новый PATH.
4. Проверьте: в терминале выполните `git --version` — должна отобразиться версия Git.

**Способ 2 — через winget (если установлен):**

```powershell
winget install --id Git.Git -e --source winget
```

После установки снова откройте терминал и переходите к п. 1.1.

---

### 1.1. Создание репозитория на GitHub

1. Зайдите на [github.com](https://github.com), войдите в аккаунт.
2. Нажмите **«New»** (или **«Create repository»**).
3. Укажите имя репозитория (например, `PDF_Parser_WEB_TS`).
4. Можно сделать репозиторий **приватным** (Private).
5. **Не** добавляйте README, .gitignore и лицензию — они уже есть в проекте.
6. Нажмите **«Create repository»**.

### 1.2. Инициализация Git в проекте и первый коммит

Откройте терминал в **корне проекта** (папка `PDF_Parser_WEB_TS`):

```bash
# Инициализация репозитория
git init

# Добавить все файлы (исключения — из .gitignore)
git add -A

# Проверить, что не попали секреты (не должно быть appsettings.Production.json с ключами)
git status

# Первый коммит
git commit -m "Исходный проект PDF Parser"

# Переименовать ветку в main (если нужно)
git branch -M main
```

### 1.3. Связь с GitHub и первый push

Подставьте свой логин и имя репозитория:

```bash
git remote add origin https://github.com/ВАШ_ЛОГИН/ИМЯ_РЕПОЗИТОРИЯ.git
git push -u origin main
```

Если репозиторий приватный, GitHub попросит авторизацию (логин/пароль или токен).  
Рекомендуется использовать [Personal Access Token](https://github.com/settings/tokens) вместо пароля.

**Важно:** в репозиторий **не** должны попадать:
- `appsettings.Production.json` (с ключом GigaChat) — он уже в `.gitignore`;
- папки `node_modules/`, `bin/`, `obj/`, `publish/` — они тоже в `.gitignore`.

---

## Часть 2. Деплой на Beget

На shared-хостинге Beget обычно **нет .NET 8**, поэтому сборку делаем **у себя на компьютере** (или в GitHub Actions), а на сервер загружаем уже **готовую папку** `publish`.

### 2.1. Сборка проекта локально

В корне проекта:

```powershell
# 1. Сборка фронтенда (React → wwwroot)
cd pdf_parser_web_ts.client
npm install
npm run build
cd ..

# 2. Публикация бэкенда (в папку publish)
cd PDF_Parser_WEB_TS.Server
dotnet publish -c Release -o ./publish
cd ..
```

Готовые файлы для деплоя лежат в:  
`PDF_Parser_WEB_TS.Server\publish\`

### 2.2. Конфигурация для продакшена

Перед загрузкой на Beget:

1. В папке `publish` создайте или отредактируйте **`appsettings.Production.json`** (он не в Git).
2. Укажите ваш домен и ключ GigaChat, например:

```json
{
  "AllowedOrigins": ["https://ваш-домен.beget.tech", "https://www.ваш-домен.beget.tech"],
  "GigaChat": {
    "AuthUrl": "https://ngw.devices.sberbank.ru:9443/api/v2/oauth",
    "ApiUrl": "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
    "Authorization": "Basic ВАШ_КЛЮЧ_В_BASE64",
    "Scope": "GIGACHAT_API_PERS",
    "Model": "GigaChat"
  }
}
```

### 2.3. Загрузка на Beget

**Вариант A: Файловый менеджер в панели Beget**

1. Войдите в [панель Beget](https://cp.beget.com).
2. Откройте **Файловый менеджер**.
3. Перейдите в корень сайта (например, `www/ваш-домен.beget.tech/public_html/`).
4. Загрузите **все содержимое** папки `PDF_Parser_WEB_TS.Server\publish\` (файлы и папки `wwwroot`, `certs` и т.д.) в эту директорию.

**Вариант B: FTP/SFTP (FileZilla, WinSCP)**

1. В панели Beget найдите данные для FTP/SFTP (хост, логин, пароль).
2. Подключитесь к серверу и откройте корень сайта.
3. Загрузите содержимое `PDF_Parser_WEB_TS.Server\publish\` в корень сайта.

**Вариант C: Клонирование с GitHub на сервер (только если на Beget есть .NET 8)**

Если у вас **VPS** или у Beget есть .NET 8 и SSH:

```bash
ssh ваш_логин@ваш-домен.beget.tech
cd ~/www/ваш-домен.beget.tech/public_html
git clone https://github.com/ВАШ_ЛОГИН/ИМЯ_РЕПОЗИТОРИЯ.git .
# Дальше: установить .NET 8, Node, выполнить npm install, npm run build, dotnet publish
```

На обычном shared-хостинге Beget так делать обычно **нельзя** (нет .NET SDK), поэтому используйте варианты A или B.

### 2.4. Запуск приложения на Beget

- Если у вас **shared-хостинг**: часто нельзя запустить свой процесс `dotnet`. Нужно уточнить в поддержке Beget, есть ли возможность запуска .NET 8 приложений или отдельный продукт (VPS) для этого.
- Если есть **SSH и .NET Runtime**:
  - перейдите в каталог сайта: `cd ~/www/ваш-домен.beget.tech/public_html`;
  - запуск: `dotnet PDF_Parser_WEB_TS.Server.dll` (или настройка systemd, как в `DEPLOY_PODROBNO.md`).
- Нужно настроить **Nginx** (или аналог) как reverse proxy на порт, который слушает приложение (например, 5000). Подробнее — в `DEPLOY_PODROBNO.md`.

---

## Часть 3. Обновление после изменений (через GitHub)

1. Вносите изменения в код локально.
2. Коммит и push в GitHub:

```bash
git add -A
git status   # убедитесь, что нет лишнего (ключей, publish)
git commit -m "Описание изменений"
git push
```

3. **Сборка заново** (как в п. 2.1): `npm run build` в клиенте, `dotnet publish` в сервере.
4. **Обновить `appsettings.Production.json`** в папке `publish`, если меняли настройки.
5. **Загрузить обновлённое содержимое** `publish` на Beget (повторить п. 2.3).
6. Если приложение запущено как служба — перезапустить её на сервере.

---

## Краткий чек-лист

| Шаг | Действие |
|-----|----------|
| 1 | Создать репозиторий на GitHub |
| 2 | `git init`, `git add -A`, `git commit`, `git remote add origin ...`, `git push` |
| 3 | Собрать проект: `npm run build` (клиент), `dotnet publish` (сервер) |
| 4 | Настроить `appsettings.Production.json` в папке `publish` |
| 5 | Загрузить содержимое `publish` на Beget (FTP/файловый менеджер) |
| 6 | На сервере: запустить приложение и при необходимости Nginx (см. DEPLOY_PODROBNO.md) |

Подробности по настройке сервера, Nginx и запуску .NET-приложения — в файле **DEPLOY_PODROBNO.md**.
