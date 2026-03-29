#!/bin/sh
set -e

log() {
  printf '%s\n' "$1" | tee -a /var/log/cron.log
}

log "[INIT] Running scraper once on container startup..."
cd /app

dotnet turbo-scraper.dll >> /var/log/cron.log 2>&1 || log "[WARN] Initial run failed"

log "[INIT] Starting cron..."
exec cron -f