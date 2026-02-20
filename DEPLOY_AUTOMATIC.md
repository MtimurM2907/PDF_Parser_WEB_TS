# Автоматический деплой через GitHub Actions и Beget

Эта инструкция поможет настроить автоматический деплой: при каждом push в GitHub проект будет автоматически собираться и загружаться на Beget.

---

## Как это работает

1. Вы делаете изменения в коде локально
2. Делаете `git commit` и `git push` в GitHub
3. **GitHub Actions автоматически:**
   - Собирает фронтенд (React)
   - Публикует бэкенд (ASP.NET)
   - Загружает готовые файлы на Beget через FTP

---

## Настройка

### Шаг 1: Получите данные FTP от Beget

1. Войдите в панель Beget: https://cp.beget.com
2. Найдите раздел **«FTP»** или **«Доступы»**
3. Запишите:
   - **FTP-хост** (например, `timurm5h.beget.tech` или IP-адрес)
   - **FTP-логин** (обычно `timurm5h`)
   - **FTP-пароль** (может отличаться от пароля панели)

### Шаг 2: Добавьте секреты в GitHub

1. Откройте ваш репозиторий на GitHub: https://github.com/MtimurM2907/PDF_Parser_WEB_TS
2. Перейдите в **Settings** → **Secrets and variables** → **Actions**
3. Нажмите **«New repository secret»**
4. Добавьте три секрета:

   **Секрет 1:**
   - Name: `BEGET_FTP_HOST`
   - Value: ваш FTP-хост (например, `timurm5h.beget.tech`)

   **Секрет 2:**
   - Name: `BEGET_FTP_USER`
   - Value: ваш FTP-логин (например, `timurm5h`)

   **Секрет 3:**
   - Name: `BEGET_FTP_PASSWORD`
   - Value: ваш FTP-пароль

### Шаг 3: Файл workflow уже создан

В проекте уже есть файл `.github/workflows/deploy-to-beget.yml` — он автоматически запустится при следующем push.

### Шаг 4: Первый деплой

Сделайте любой коммит и push:

```powershell
cd C:\Users\User\Desktop\PDF_Parser_WEB_TS
git add .
git commit -m "Настройка автоматического деплоя"
git push
```

После push:
1. Перейдите на GitHub в ваш репозиторий
2. Откройте вкладку **«Actions»**
3. Вы увидите запущенный workflow **«Deploy to Beget»**
4. Дождитесь завершения (обычно 2-5 минут)

---

## Что делает workflow

1. ✅ Проверяет код из GitHub
2. ✅ Устанавливает .NET 8 и Node.js
3. ✅ Собирает фронтенд: `npm ci` → `npm run build`
4. ✅ Публикует бэкенд: `dotnet publish -c Release -o ./publish`
5. ✅ Загружает файлы из `publish` на Beget через FTP в папку `/public_html/`

---

## Важные замечания

### appsettings.Production.json

Файл `appsettings.Production.json` **не попадает в Git** (он в `.gitignore`), поэтому:

**Вариант A:** Создайте его вручную на сервере Beget после первого деплоя:
- Через файловый менеджер Beget создайте файл `appsettings.Production.json` в корне сайта
- Вставьте туда ваш конфиг с доменом и ключом GigaChat

**Вариант B:** Используйте GitHub Secrets для конфигурации:
- Добавьте секрет `BEGET_APP_SETTINGS` с содержимым `appsettings.Production.json`
- Модифицируйте workflow, чтобы он создавал этот файл на сервере

### Первый запуск

После первого автоматического деплоя:
1. Убедитесь, что файлы загружены в `/public_html/` на Beget
2. Создайте `appsettings.Production.json` на сервере (если его нет)
3. Запустите приложение на сервере (через SSH или терминал панели)
4. Настройте Nginx reverse proxy (если нужно)

---

## Ручной запуск деплоя

Если нужно запустить деплой вручную (без коммита):

1. Откройте репозиторий на GitHub
2. Перейдите в **Actions**
3. Выберите workflow **«Deploy to Beget»**
4. Нажмите **«Run workflow»** → **«Run workflow»**

---

## Обновление приложения

Теперь для обновления приложения достаточно:

```powershell
# Внесли изменения в код
git add .
git commit -m "Описание изменений"
git push
```

GitHub Actions автоматически соберёт и загрузит новую версию на Beget!

---

## Проверка работы

После деплоя проверьте:
1. Файлы на Beget обновились (через файловый менеджер)
2. Приложение работает: откройте https://timurm5h.beget.tech
3. Логи workflow в GitHub Actions (если были ошибки)

---

## Решение проблем

### Workflow не запускается
- Проверьте, что файл `.github/workflows/deploy-to-beget.yml` есть в репозитории
- Убедитесь, что вы пушите в ветку `main`

### Ошибка FTP подключения
- Проверьте секреты `BEGET_FTP_HOST`, `BEGET_FTP_USER`, `BEGET_FTP_PASSWORD`
- Убедитесь, что FTP включен в панели Beget

### Файлы не загружаются
- Проверьте путь `server-dir` в workflow (должен быть `/public_html/` или ваш корень сайта)
- Посмотрите логи в GitHub Actions для деталей ошибки

---

## Альтернатива: Ручной деплой через GitHub

Если автоматический деплой не подходит, можно использовать GitHub как хранилище кода:

1. **Храните код в GitHub** (уже сделано ✅)
2. **Собирайте локально:**
   ```powershell
   cd pdf_parser_web_ts.client
   npm run build
   cd ..
   cd PDF_Parser_WEB_TS.Server
   dotnet publish -c Release -o ./publish
   ```
3. **Загружайте `publish` на Beget** через FTP/файловый менеджер

Этот способ требует ручной загрузки файлов, но не требует настройки секретов.
