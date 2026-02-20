# Инструкция по запуску приложения на сервере Beget

После успешного деплоя через GitHub Actions файлы уже загружены на сервер в `public_html`. Теперь нужно запустить приложение.

---

## Вариант 1: Запуск через SSH (рекомендуется)

### Шаг 1: Подключитесь к серверу по SSH

```bash
ssh ваш_логин@timurm5h.beget.tech
# или
ssh ваш_логин@ваш-домен.beget.tech
```

**Если SSH не работает:**
- Проверьте, включён ли SSH в панели Beget
- Убедитесь, что используете правильный логин и пароль
- Попробуйте использовать терминал в панели Beget (если доступен)

### Шаг 2: Перейдите в директорию сайта

```bash
cd ~/www/timurm5h.beget.tech/public_html
# или
cd ~/public_html
```

### Шаг 3: Проверьте наличие файлов

```bash
ls -la
```

Должны быть видны:
- `PDF_Parser_WEB_TS_Server` (исполняемый файл)
- `appsettings.Production.json` (если вы его создали)
- `wwwroot/` (папка с фронтендом)
- `certs/` (папка с сертификатом)

### Шаг 4: Создайте appsettings.Production.json (если его нет)

```bash
nano appsettings.Production.json
```

Вставьте следующий контент (замените значения на свои):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AllowedOrigins": [
    "https://timurm5h.beget.tech"
  ],
  "GigaChat": {
    "AuthUrl": "https://ngw.devices.sberbank.ru:9443/api/v2/oauth",
    "ApiUrl": "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
    "Authorization": "Basic ВАШ_BASE64_КЛЮЧ_ЗДЕСЬ",
    "Scope": "GIGACHAT_API_PERS",
    "Model": "GigaChat"
  }
}
```

Сохраните: `Ctrl+O`, `Enter`, `Ctrl+X`

### Шаг 5: Сделайте файл исполняемым

```bash
chmod +x PDF_Parser_WEB_TS_Server
```

### Шаг 6: Запустите приложение

**Вариант A: Простой запуск (для теста)**

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
./PDF_Parser_WEB_TS_Server
```

Приложение запустится и будет работать, пока вы не закроете терминал (`Ctrl+C`).

**Вариант B: Запуск в фоне (рекомендуется)**

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
nohup ./PDF_Parser_WEB_TS_Server > app.log 2>&1 &
```

Приложение запустится в фоне. Логи будут в файле `app.log`.

**Вариант C: Использование screen (лучший вариант)**

```bash
# Установите screen (если нет)
# На Beget обычно уже установлен

# Создайте новую сессию screen
screen -S pdf-parser

# Внутри screen запустите приложение
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
./PDF_Parser_WEB_TS_Server

# Отключитесь от screen: Ctrl+A, затем D
# Вернуться к сессии: screen -r pdf-parser
```

### Шаг 7: Проверьте, что приложение работает

Откройте в браузере:
- `https://timurm5h.beget.tech/` — должен открыться фронтенд
- `https://timurm5h.beget.tech/swagger` — должен открыться Swagger UI

---

## Вариант 2: Запуск через systemd (для VPS)

Если у вас VPS и есть права root, можно настроить systemd-сервис:

### Создайте файл сервиса

```bash
sudo nano /etc/systemd/system/pdf-parser.service
```

Вставьте:

```ini
[Unit]
Description=PDF Parser Web Application
After=network.target

[Service]
Type=notify
User=ваш_логин
WorkingDirectory=/home/ваш_логин/www/timurm5h.beget.tech/public_html
ExecStart=/home/ваш_логин/www/timurm5h.beget.tech/public_html/PDF_Parser_WEB_TS_Server
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

### Запустите сервис

```bash
sudo systemctl daemon-reload
sudo systemctl enable pdf-parser
sudo systemctl start pdf-parser
sudo systemctl status pdf-parser
```

---

## Вариант 3: Настройка Nginx reverse proxy

Если приложение работает на `localhost:5000`, но не открывается по домену, нужно настроить Nginx.

### Создайте конфигурацию Nginx

```bash
sudo nano /etc/nginx/sites-available/timurm5h.beget.tech
```

Вставьте:

```nginx
server {
    listen 80;
    listen [::]:80;
    server_name timurm5h.beget.tech;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Включите конфигурацию и перезапустите Nginx

```bash
sudo ln -s /etc/nginx/sites-available/timurm5h.beget.tech /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## Проверка работы

1. **Проверьте логи приложения:**
   ```bash
   tail -f app.log
   # или если используете systemd:
   sudo journalctl -u pdf-parser -f
   ```

2. **Проверьте, что процесс запущен:**
   ```bash
   ps aux | grep PDF_Parser_WEB_TS_Server
   ```

3. **Проверьте порт:**
   ```bash
   netstat -tlnp | grep 5000
   ```

4. **Откройте в браузере:**
   - `https://timurm5h.beget.tech/`
   - `https://timurm5h.beget.tech/swagger`

---

## Остановка приложения

**Если запущено в терминале:**
- Нажмите `Ctrl+C`

**Если запущено через nohup:**
```bash
pkill -f PDF_Parser_WEB_TS_Server
```

**Если запущено через screen:**
```bash
screen -r pdf-parser
# Затем Ctrl+C
```

**Если запущено через systemd:**
```bash
sudo systemctl stop pdf-parser
```

---

## Решение проблем

### Приложение не запускается

1. Проверьте, что файл исполняемый: `chmod +x PDF_Parser_WEB_TS_Server`
2. Проверьте логи: `cat app.log` или `journalctl -u pdf-parser`
3. Убедитесь, что порт 5000 свободен: `netstat -tlnp | grep 5000`

### Ошибка "appsettings.Production.json not found"

Создайте файл `appsettings.Production.json` в директории `public_html` (см. Шаг 4 выше).

### Приложение запускается, но не открывается в браузере

1. Проверьте, что приложение слушает на `localhost:5000`
2. Настройте Nginx reverse proxy (см. Вариант 3)
3. Проверьте файрвол: порт 5000 должен быть доступен локально

### Ошибка с сертификатом GigaChat

Убедитесь, что файл `certs/russian_trusted_root_ca_pem.crt` присутствует в `public_html/certs/`.

---

## Автоматический перезапуск после деплоя

После каждого деплоя через GitHub Actions нужно перезапустить приложение:

```bash
# Остановите старое
pkill -f PDF_Parser_WEB_TS_Server

# Запустите новое
cd ~/www/timurm5h.beget.tech/public_html
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
nohup ./PDF_Parser_WEB_TS_Server > app.log 2>&1 &
```

Или создайте скрипт `restart.sh`:

```bash
#!/bin/bash
pkill -f PDF_Parser_WEB_TS_Server
sleep 2
cd ~/www/timurm5h.beget.tech/public_html
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
nohup ./PDF_Parser_WEB_TS_Server > app.log 2>&1 &
echo "Приложение перезапущено"
```

Сделайте его исполняемым: `chmod +x restart.sh`
