#!/usr/bin/env bash
# ==============================================================================
# PawCare — EC2 Bootstrap & Production Container Runner
# Installs Docker, Docker Compose, sets up systemd service, and runs the PawCare
# backend API container connected to AWS RDS and AWS S3 with IAM role auth.
# ==============================================================================
set -euo pipefail

echo "=== [1/6] Installing dependencies (Docker, Git, PostgreSQL client) ==="
if command -v dnf &>/dev/null; then
    dnf update -y
    dnf install -y docker git postgresql16 libpq-devel
    systemctl start docker
    systemctl enable docker
    usermod -a -G docker ec2-user
elif command -v apt-get &>/dev/null; then
    apt-get update -y
    apt-get install -y docker.io docker-compose git postgresql-client
    systemctl start docker
    systemctl enable docker
    usermod -a -G docker ubuntu
fi

echo "=== [2/6] Preparing application directory ==="
APP_DIR="/opt/pawcare"
mkdir -p "${APP_DIR}"
cd "${APP_DIR}"

# Ensure .env is present (defaults or populated by CloudFormation / Parameter Store)
if [ ! -f "${APP_DIR}/.env" ]; then
    echo "Creating template .env configuration..."
    cat << 'EOF' > "${APP_DIR}/.env"
ASPNETCORE_ENVIRONMENT=Production
ApplyMigrationsOnStartup=true
SeedDefaultData=true
EnableSwagger=true
Cors__AllowedOrigins__0=*
Aws__Region=us-east-1
Aws__SimulationMode=false
Aws__S3__SimulationMode=false
EOF
fi

echo "=== [3/6] Verifying PostgreSQL RDS connectivity ==="
# Test database connection if RDS_HOST is defined
if [ -n "${RDS_HOST:-}" ]; then
    echo "Testing connection to RDS at ${RDS_HOST}:${RDS_PORT:-5432}..."
    until PGPASSWORD="${RDS_PASSWORD:-}" pg_isready -h "${RDS_HOST}" -p "${RDS_PORT:-5432}" -U "${RDS_USER:-postgres}"; do
        echo "Waiting for RDS database to accept connections..."
        sleep 5
    done
    echo "RDS connection confirmed!"
fi

echo "=== [4/6] Setting up PawCare API systemd service ==="
cat << 'EOF' > /etc/systemd/system/pawcare-api.service
[Unit]
Description=PawCare Web API Container
After=docker.service
Requires=docker.service

[Service]
TimeoutStartSec=0
Restart=always
WorkingDirectory=/opt/pawcare
ExecStartPre=-/usr/bin/docker stop pawcare-api
ExecStartPre=-/usr/bin/docker rm pawcare-api
ExecStart=/usr/bin/docker run --name pawcare-api \
  --restart unless-stopped \
  -p 8080:8080 \
  --env-file /opt/pawcare/.env \
  pawcare-api:latest
ExecStop=/usr/bin/docker stop pawcare-api

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload

echo "=== [5/6] Setup script completed successfully ==="
echo "To build and start the application container:"
echo "  cd /opt/pawcare && docker build -t pawcare-api:latest -f backend/Dockerfile backend/"
echo "  systemctl start pawcare-api"
echo "  systemctl status pawcare-api"
