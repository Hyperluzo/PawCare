# Day 14 — AWS Deployment Guide & Production Architecture

> **Goal:** Backend running in AWS against Amazon RDS (PostgreSQL 16), files and medical records stored in Amazon S3, and emails dispatched via Amazon SES with EC2 IAM Role authentication.

---

## 1. Architecture Overview

```
                      +---------------------------------------+
                      |         PawCare Frontend SPA          |
                      |     (React + TypeScript + Vite)       |
                      +-------------------+-------------------+
                                          | HTTPS / REST API
                                          v
                      +---------------------------------------+
                      |           Amazon EC2 Host             |
                      |   +-------------------------------+   |
                      |   |    Nginx Reverse Proxy / SSL  |   |
                      |   +---------------+---------------+   |
                      |                   |                   |
                      |   +---------------v---------------+   |
                      |   |     PawCare.Api Container     |   |
                      |   |     (.NET 10 / Port 8080)     |   |
                      |   +---------------+---------------+   |
                      |                   |                   |
                      |   +---------------+---------------+   |
                      |   |    EC2 IAM Instance Profile   |   |
                      |   |  - S3 Object Access           |   |
                      |   |  - SES SendEmail Access       |   |
                      |   +---------------+---------------+   |
                      +----------+--------+---------+---------+
                                 |        |         |
                  TCP 5432       |        |         | HTTPS API
          (Restricted SG Ingress)|        |         | (IAM Authenticated)
                                 v        |         v
  +--------------------------------+      |   +-------------------------------+
  |       Amazon RDS PostgreSQL    |      |   |        Amazon SES             |
  |       (db.t4g.micro / 16.3)    |      |   |  - Status Notification Emails |
  |       Database: pawcare        |      |   |  - HTML & Text Templates      |
  +--------------------------------+      |   +-------------------------------+
                                          v
                         +--------------------------------+
                         |        Amazon S3 Bucket        |
                         |  pawcare-attachments-prod      |
                         |  - Patient Photos              |
                         |  - Lab Documents & Reports     |
                         |  - Treatment Attachments       |
                         +--------------------------------+
```

---

## 2. Infrastructure Step-by-Step

### Step 1: Amazon RDS Setup (PostgreSQL 16)
1. **Create RDS Subnet Group & Instance:**
   - **Engine:** PostgreSQL 16.3+
   - **Instance Class:** `db.t4g.micro` or `db.t3.micro` (Free Tier eligible)
   - **Storage:** 20 GB gp3 with storage autoscaling enabled
   - **DB Name:** `pawcare`
   - **Master Username:** `pawcareadmin`
   - **Master Password:** Set strong password (e.g. 16+ alphanumeric & symbols)
2. **Security Group Configuration (Crucial):**
   - Create `pawcare-rds-sg`.
   - **Inbound Rule:** Port `5432`, Type: `PostgreSQL`.
   - **Source:** Point directly to `pawcare-ec2-sg` (the EC2 instance's Security Group ID), **never** `0.0.0.0/0`.
   > **Watch out:** RDS security group misconfiguration is the #1 deployment blocker. Test that the EC2 host can communicate via `pg_isready -h <rds-endpoint> -p 5432 -U pawcareadmin` before launching the application.

---

### Step 2: Database Migration Strategy
Choose one of the two supported migration paths:

#### Option A: Automatic Startup Migration (Recommended for EC2)
Set the environment variable on the container:
```bash
ApplyMigrationsOnStartup=true
```
The API automatically applies pending EF Core migrations (`AppDbContext.Database.Migrate()`) on startup before accepting incoming HTTP requests.

#### Option B: Standalone Script Execution
Run the idempotent SQL script generated in `deploy/sql/migrations.sql` directly against RDS:
```bash
# Using the helper script
export RDS_HOST="<rds-endpoint>.rds.amazonaws.com"
export RDS_PASSWORD="<master-password>"
export RDS_USER="pawcareadmin"
bash deploy/aws/setup-rds-migrations.sh
```
Or with standard `psql`:
```bash
PGPASSWORD="<password>" psql -h <rds-endpoint> -U pawcareadmin -d pawcare -f deploy/sql/migrations.sql
```

---

### Step 3: Amazon S3 Storage Setup
1. **Create Bucket:**
   - Bucket Name: `pawcare-attachments-production` (or globally unique name)
   - Region: `us-east-1` (match your RDS and EC2 region)
2. **Configure CORS:**
   Paste the following JSON in S3 -> Permissions -> Cross-origin resource sharing (CORS):
   ```json
   [
     {
       "AllowedHeaders": ["*"],
       "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"],
       "AllowedOrigins": ["*"],
       "ExposeHeaders": ["ETag"],
       "MaxAgeSeconds": 3600
     }
   ]
   ```
3. **Database Schema Integration:**
   PawCare stores only the object key / URL in Postgres:
   - `Patients.PhotoUrl` (e.g. `https://pawcare-attachments.s3.amazonaws.com/patients/photos/...`)
   - `Patients.DocumentUrl` (e.g. lab report PDF)
   - `Treatments.AttachmentUrl` (e.g. X-ray scans, prescription documents)
   - `Treatments.AttachmentFileName` (display name)

---

### Step 4: Docker Containerization

#### Build & Run Locally
```bash
# Build API Image
docker build -t pawcare-api -f backend/Dockerfile backend/

# Test with environment variables
docker run -d --name pawcare-api -p 8080:8080 --env-file .env pawcare-api

# Or use docker-compose
docker compose up -d --build
```

#### Push to Amazon ECR (Elastic Container Registry)
```bash
# Authenticate Docker to AWS ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <aws-account-id>.dkr.ecr.us-east-1.amazonaws.com

# Tag and Push
docker tag pawcare-api:latest <aws-account-id>.dkr.ecr.us-east-1.amazonaws.com/pawcare-api:latest
docker push <aws-account-id>.dkr.ecr.us-east-1.amazonaws.com/pawcare-api:latest
```

---

### Step 5: Amazon EC2 Launch & IAM Role Authentication
1. **Launch EC2 Instance:**
   - AMI: Amazon Linux 2023 (or Ubuntu 24.04 LTS)
   - Instance Type: `t3.small` / `t4g.small`
   - Security Group: `pawcare-ec2-sg` (Inbound HTTP 80, HTTPS 443, port 8080, and SSH 22)
2. **Attach IAM Role:**
   Attach `PawCareEC2AppRole` with policies:
   - `AmazonS3FullAccess` (or scoped policy for `pawcare-attachments-*`)
   - `AmazonSESFullAccess` (for email notifications)
   > **Security Best Practice:** By using an IAM Role on the EC2 instance, you **never** need to store AWS Access Keys or Secrets in configuration files or container images! The AWS .NET SDK automatically retrieves temporary credentials via the EC2 Instance Metadata Service (IMDSv2).

---

### Step 6: Production Environment Variables (.env)
On the EC2 instance, write `/opt/pawcare/.env`:
```ini
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Host=<rds-endpoint>.rds.amazonaws.com;Port=5432;Database=pawcare;Username=pawcareadmin;Password=<strong-password>;SslMode=Require;
ApplyMigrationsOnStartup=true
SeedDefaultData=false
EnableSwagger=true

Jwt__Key=<your-secure-256-bit-key>
Jwt__Issuer=pawcare-api
Jwt__Audience=pawcare-clients

Aws__Region=us-east-1
Aws__SenderEmail=no-reply@pawcare.local
Aws__SimulationMode=false
Aws__S3__BucketName=pawcare-attachments-production
Aws__S3__SimulationMode=false
```

---

### Step 7: Automated Deployment via CloudFormation
A production-ready AWS CloudFormation template is provided in `deploy/aws/cloudformation.yaml`.

To deploy the entire stack in one command:
```bash
aws cloudformation create-stack \
  --stack-name pawcare-production \
  --template-body file://deploy/aws/cloudformation.yaml \
  --parameters \
      ParameterKey=DBMasterPassword,ParameterValue="YOUR_STRONG_DB_PASSWORD" \
      ParameterKey=DBMasterUsername,ParameterValue="pawcareadmin" \
      ParameterKey=EnvironmentName,ParameterValue="production" \
  --capabilities CAPABILITY_NAMED_IAM \
  --region us-east-1
```

---

## 3. Production Verification Checklist

1. **API Health Ping:**
   ```bash
   curl -i http://<ec2-public-ip>:8080/api/ping
   # Expected: HTTP 200 {"status":"ok","time":"..."}
   ```

2. **Database Connectivity Check:**
   ```bash
   curl -i http://<ec2-public-ip>:8080/api/ping/db
   # Expected: HTTP 200 {"databaseConnected":true}
   ```

3. **S3 Photo & Attachment Upload:**
   ```bash
   curl -X POST http://<ec2-public-ip>:8080/api/patients/1/photo \
     -H "Authorization: Bearer <token>" \
     -F "file=@pet_photo.jpg"
   # Expected: HTTP 200 with FileUploadResponse and S3 URL
   ```

4. **SES Notification Dispatch:**
   - Change patient status to `CRITICAL` or `DISCHARGED`.
   - Verify email arrives at owner address.
   - Verify notification record is logged as `SENT` in `Notifications` table.
