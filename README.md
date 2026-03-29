# TurboazScraper

TurboazScraper is a .NET 8 Selenium-based scraper for [turbo.az](https://turbo.az) that:

- applies filters from environment variables
- optionally skips UI filtering when a pre-filtered URL is provided
- scrapes only recent listings
- stores seen listing IDs in SQLite to avoid duplicate alerts
- sends newly discovered cars to a Discord webhook
- can run locally, in Docker, or on a daily schedule with cron

---

## Features

- .NET 8 console application
- Selenium + ChromeDriver
- SQLite persistence for seen car IDs
- Discord webhook notifications
- Environment-based configuration
- Optional `FILTERED_URL` mode
- Cross-platform support for Windows and Linux
- Docker support
- Daily scheduled execution with cron

---

## Project Structure

```text
TurboazScraper/
├── Program.cs
├── Scraper.cs
├── TurboazScraper.cs
├── TurboazScraperConfig.cs
├── Helpers.cs
├── CarModel.cs
├── SeenCarsRepository.cs
├── DiscordNotifier.cs
├── .env
├── Dockerfile
├── docker-compose.yml
├── cronjob
├── entrypoint.sh
├── turbo-scraper.csproj
├── turbo-scraper.sln
└── README.md
```

---

## Requirements

### Local development
- .NET 8 SDK
- Google Chrome or Chromium
- ChromeDriver compatible with your browser version

### Docker
- Docker
- Docker Compose plugin (`docker compose`)

### NuGet packages
This project uses:

- DotNetEnv
- DotNetSeleniumExtras.WaitHelpers
- Microsoft.Data.Sqlite
- Selenium.Support
- Selenium.WebDriver

---

## Installation

### 1. Clone the repository

```bash
git clone <your-repo-url>
cd TurboazScraper
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Create `.env`

Example:

```env
FILTERED_URL=https://turbo.az/autos?filtered-query

DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/XXX/YYY

SCRAPER_MAX_VIEWS=500

SCRAPER_MIN_PRICE=3000
SCRAPER_MAX_PRICE=12000

SCRAPER_MIN_MILEAGE=0
SCRAPER_MAX_MILEAGE=250000

SCRAPER_YEAR_MIN=2012
SCRAPER_YEAR_MAX=2021

SCRAPER_CREDIT=true
SCRAPER_BARTER=false

SCRAPER_MARKETS=Rəsmi diler,Avropa
SCRAPER_WHITELIST_CITY=Bakı,Sumqayıt
```

---

## Configuration

### `FILTERED_URL`
If set, the scraper uses this URL directly and skips `AddFilters()`.

Example:

```env
FILTERED_URL=https://turbo.az/autos?...filtered-query...
```

If empty, the scraper starts from `https://turbo.az` and applies filters through the UI.

### `SCRAPER_MAX_VIEWS`
Maximum allowed view count for a listing.

### `SCRAPER_MIN_PRICE`
Minimum price.

### `SCRAPER_MAX_PRICE`
Maximum price.

### `SCRAPER_MIN_MILEAGE`
Minimum mileage.

### `SCRAPER_MAX_MILEAGE`
Maximum mileage.

### `SCRAPER_YEAR_MIN`
Minimum model year.

### `SCRAPER_YEAR_MAX`
Maximum model year.

### `SCRAPER_CREDIT`
Enable the `Kredit` checkbox.

Accepted truthy values:
- `true`
- `1`
- `yes`
- `on`

### `SCRAPER_BARTER`
Enable the `Barter` checkbox.

Accepted truthy values:
- `true`
- `1`
- `yes`
- `on`

### `SCRAPER_MARKETS`
Comma-separated market list.

Example:

```env
SCRAPER_MARKETS=Rəsmi diler
```

Use the real option text from turbo.az.

### `SCRAPER_WHITELIST_CITY`
Comma-separated city whitelist.

Example:

```env
SCRAPER_WHITELIST_CITY=Bakı
```

If this list is provided, only cars from those cities are returned.

### `DISCORD_WEBHOOK_URL`
Discord webhook URL for notifications.

Each new car is sent as `car.ToString()` inside a code block.

---

## How It Works

1. Load environment variables from `.env`
2. Determine URL:
   - use `FILTERED_URL` if present
   - otherwise use `https://turbo.az`
3. Optionally apply UI filters
4. Scrape listing cards
5. Open each listing page to fetch:
   - views
   - transmission
6. Ignore already seen cars using SQLite
7. Send only new cars to Discord

---

## SQLite Deduplication

The scraper stores seen car IDs in:

```text
turbo_scraper.db
```

This prevents sending the same listing multiple times across runs.

Stored fields:
- `Id`
- `Url`
- `FirstSeenAt`
- `LastSeenAt`

Old rows are cleaned automatically with:

```csharp
seenRepo.DeleteOlderThan(DateTime.UtcNow.AddDays(-30));
```

---

## Running Locally

```bash
dotnet run
```

If a new matching car is found:
- it is printed to the console
- it is sent to Discord

---

## Discord Integration

### 1. Create a Discord webhook
In Discord:

- Open the target channel
- Go to **Edit Channel**
- Open **Integrations**
- Open **Webhooks**
- Create a webhook
- Copy the webhook URL

### 2. Put it in `.env`

```env
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/XXX/YYY
```

### 3. Run the scraper

```bash
dotnet run
```

## Example Discord Message

Each new car is sent using `car.ToString()`:

```text
City: Bakı
Name: Toyota Corolla
Details: 2015, 1.8 L, 210 000 km
Transmission: Avtomat (Variator)
Price: 17800 AZN
Date: 29-03-2026 13:42
Views: 123
Url: https://turbo.az/autos/1234567
Id: 1234567
```

---

## Docker

### cronjob

Create `cronjob`:

```cron
0 0 * * * root cd /app && dotnet turbo-scraper.dll >> /var/log/cron.log 2>&1
```

This runs the scraper every day at `00:00`.

### Build and start

```bash
docker compose up -d --build
```

### View logs

```bash
docker compose logs -f
```

or:

```bash
cat logs/cron.log
```

### Stop

```bash
docker compose down
```

---

## Windows Notes

On Windows, Selenium uses default ChromeDriver discovery.

Make sure:
- Chrome is installed
- ChromeDriver version matches Chrome version

---

## Linux Notes

On Linux, the scraper is configured to use:
- `/usr/bin/chromium`
- `/usr/bin/chromedriver`

and adds:
- `--no-sandbox`
- `--disable-dev-shm-usage`
- `--window-size=1920,1080`

---

## Build

```bash
dotnet build
```

## Publish

```bash
dotnet publish -c Release
```

---

## Troubleshooting

### Discord messages are not sent
Check:
- `DISCORD_WEBHOOK_URL` is valid
- webhook is still active
- server has internet access

### ChromeDriver errors
Check:
- browser and ChromeDriver versions match
- Linux paths are correct
- Windows has Chrome installed

### No cars found
Check:
- filter values are not too strict
- `SCRAPER_MARKETS` contains valid turbo.az options
- `FILTERED_URL` is valid if used

### Docker container runs but nothing happens
Check:
- cron is running
- `cronjob` file permissions are correct
- logs in `/var/log/cron.log`

---

## Disclaimer

This project is for educational purposes only.
Users are responsible for complying with turbo.az terms of service.