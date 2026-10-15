#!/usr/bin/env bash
# ==============================================================================
# PawCare — RDS Migration Runner Script (Day 14)
# Runs EF Core migrations or executes the idempotent SQL script against RDS
# ==============================================================================
set -euo pipefail

RDS_HOST="${RDS_HOST:-localhost}"
RDS_PORT="${RDS_PORT:-5432}"
RDS_USER="${RDS_USER:-postgres}"
RDS_DB="${RDS_DB:-pawcare}"
RDS_PASSWORD="${RDS_PASSWORD:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL_FILE="${SCRIPT_DIR}/../sql/migrations.sql"

echo "=========================================================="
echo " PawCare — RDS Database Migration Runner"
echo " Target: ${RDS_USER}@${RDS_HOST}:${RDS_PORT}/${RDS_DB}"
echo "=========================================================="

# 1. Connectivity Check
echo "[1/3] Testing PostgreSQL connection to RDS..."
if ! PGPASSWORD="${RDS_PASSWORD}" pg_isready -h "${RDS_HOST}" -p "${RDS_PORT}" -U "${RDS_USER}" -d "${RDS_DB}"; then
    echo "ERROR: Cannot reach PostgreSQL RDS instance. Check Security Group ingress rules!"
    echo "Remember: RDS Security Group must permit inbound 5432 from this machine's IP or EC2 SG."
    exit 1
fi
echo "Connection verified!"

# 2. Choose migration approach
if command -v dotnet &>/dev/null && [ -f "${SCRIPT_DIR}/../../backend/PawCare.Api/PawCare.Api.csproj" ]; then
    echo "[2/3] Found dotnet SDK. Running 'dotnet ef database update'..."
    export ConnectionStrings__Default="Host=${RDS_HOST};Port=${RDS_PORT};Database=${RDS_DB};Username=${RDS_USER};Password=${RDS_PASSWORD};SslMode=Require;"
    cd "${SCRIPT_DIR}/../../backend/PawCare.Api"
    dotnet ef database update
else
    echo "[2/3] Running idempotent SQL migration script via psql..."
    if [ ! -f "${SQL_FILE}" ]; then
        echo "ERROR: Migration SQL file not found at ${SQL_FILE}"
        exit 1
    fi
    PGPASSWORD="${RDS_PASSWORD}" psql -h "${RDS_HOST}" -p "${RDS_PORT}" -U "${RDS_USER}" -d "${RDS_DB}" -f "${SQL_FILE}"
fi

# 3. Verification
echo "[3/3] Migration verification:"
PGPASSWORD="${RDS_PASSWORD}" psql -h "${RDS_HOST}" -p "${RDS_PORT}" -U "${RDS_USER}" -d "${RDS_DB}" -c "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";"

echo "=== Database migrations completed successfully against AWS RDS! ==="
