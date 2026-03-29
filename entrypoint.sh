#!/bin/sh
set -e

echo "[INIT] Running scraper once on container startup..." | tee -a /var/log/cron.log
cd /app
dotnet turbo-scraper.dll >> /var/log/cron.log 2>&1 || echo "[WARN] Initial run failed" | tee -a /var/log/cron.log

echo "[INIT] Starting cron..." | tee -a /var/log/cron.logdocker compose logs -f
exec cron -f