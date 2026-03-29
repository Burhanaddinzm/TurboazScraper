# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0

RUN apt-get update && apt-get install -y \
    cron \
    chromium \
    chromium-driver \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY cronjob /etc/cron.d/turbo-scraper
COPY entrypoint.sh /entrypoint.sh

RUN printf '\n' >> /etc/cron.d/turbo-scraper && \
    chmod 0644 /etc/cron.d/turbo-scraper && \
    chmod +x /entrypoint.sh && \
    touch /var/log/cron.log

CMD ["/entrypoint.sh"]