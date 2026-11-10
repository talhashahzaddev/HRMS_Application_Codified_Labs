# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["HRMS.API/HRMS.API.csproj", "HRMS.API/"]
COPY ["HRMS.AI/HRMS.AI.csproj", "HRMS.AI/"]

# Restore dependencies
RUN dotnet restore "HRMS.API/HRMS.API.csproj"

# Copy everything else (make sure build context is solution root!)
COPY . .

# Debug: list files
RUN ls -R /src

# Build and publish
WORKDIR "/src/HRMS.API"
RUN dotnet publish "HRMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# pg_dump for Platform Admin database backups. pg_dump refuses servers newer than
# itself (live runs PostgreSQL 18), so install the newest client from PGDG.
# A newer pg_dump can still back up older servers.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc https://www.postgresql.org/media/keys/ACCC4CF8.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${VERSION_CODENAME}-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-18 \
    && apt-get purge -y --auto-remove curl gnupg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Backups are written here. Mount a Railway volume at /app/backups to keep them across redeploys.
ENV Backup__StoragePath=/app/backups
RUN mkdir -p /app/backups

ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "HRMS.API.dll"]