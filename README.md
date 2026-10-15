# PawCare — Veterinary Hospital Clinical & Inpatient Management System

PawCare is a full-stack veterinary hospital clinical management platform built with **C# / ASP.NET Core 10 Web API**, **PostgreSQL / AWS RDS**, **AWS SES**, **AWS S3**, and a modern **React + TypeScript + Tailwind CSS** frontend.

---

## 🚀 Key Features

- **Inpatient & Ward Management:** Admission, ward assignments, real-time bed capacity monitoring, and occupancy metrics.
- **Clinical Records & Treatments:** Comprehensive treatment plans, medications, clinical notes, and chronological audit trails with staff attribution.
- **Medical Media & S3 Attachments (Day 14):** Direct file uploads for patient photos, lab reports, and treatment documents powered by Amazon S3 and simulated local fallback.
- **Status Workflow & Safety Gates:** Validated admission & clinical status transitions with safety confirmation modals for critical events.
- **Automated AWS SES Notifications:** Decoupled owner email dispatch for status updates (Under Treatment, Discharged, Deceased) with admin retry and audit tracking.
- **Live Dashboard Telemetry:** Live inpatient counters, treated today counts, MTD statistics, and ward capacity visualizations with auto-refresh.
- **Role-Based Access Control:** Secure JWT authentication with ADMIN and STAFF roles.
- **Cloud Ready & Containerized (Day 14):** Multi-stage Dockerfiles, Docker Compose, AWS RDS PostgreSQL 16 infrastructure, and CloudFormation templates.

---

## 🛠 Tech Stack

- **Backend:** ASP.NET Core 10 Web API, Entity Framework Core 10, Npgsql
- **Database:** PostgreSQL 16 (Local Docker & Amazon RDS)
- **Cloud & Storage:** AWS S3 (Media/Attachments), AWS SES (Email Notifications), AWS EC2 (App Host)
- **Frontend:** React 18, TypeScript, Vite, Tailwind CSS, Lucide React
- **Testing:** xUnit, EF Core In-Memory, Coverlet

---

## 📦 Running Locally with Docker

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Hyperluzo/PawCare.git
   cd PawCare
   ```

2. **Start all services with Docker Compose:**
   ```bash
   docker compose up -d --build
   ```

3. **Access the application:**
   - **Frontend App:** [http://localhost:3000](http://localhost:3000)
   - **Backend API:** [http://localhost:8080](http://localhost:8080)
   - **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)

4. **Default Admin Login (Dev Seed):**
   - **Email:** `admin@pawcare.local`
   - **Password:** `ChangeMe123!` (seeded on initial startup in Development environment)

---

## ☁️ AWS Deployment (Day 14)

See the full deployment guide in [`docs/DAY14_AWS_DEPLOYMENT.md`](docs/DAY14_AWS_DEPLOYMENT.md).

### Quick CloudFormation Deploy:
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

### Database Migrations against RDS:
```bash
# Run idempotent SQL script
PGPASSWORD="YOUR_DB_PASSWORD" psql -h <rds-endpoint> -U pawcareadmin -d pawcare -f deploy/sql/migrations.sql
```

---

## 🧪 Testing

Run backend test suite (29+ unit & integration tests):
```bash
dotnet test backend/PawCare.Api.Tests/PawCare.Api.Tests.csproj
```

Build and validate frontend:
```bash
cd frontend && npm run build
```

---

## 📜 15-Day Roadmap Progress

- [x] Day 1: Inspect Frontend + Environment Setup
- [x] Day 2: ASP.NET Core Project Scaffold
- [x] Day 3: PostgreSQL + EF Core Setup
- [x] Day 4: Migrations + Seed Data
- [x] Day 5: Authentication & JWT
- [x] Day 6: Staff Management (Admin Only)
- [x] Day 7: Ward Management
- [x] Day 8: Patient Admission Workflow
- [x] Day 9: Patient Search, Filter, Sort, Pagination
- [x] Day 10: Treatment Records & Staff Attribution
- [x] Day 11: Patient Status Workflow & Confirmation Gates
- [x] Day 12: AWS SES Email Notifications & Decoupled Dispatch
- [x] Day 13: Live Dashboard Statistics & Ward Occupancy Telemetry
- [x] **Day 14: AWS Deployment, RDS PostgreSQL, S3 Storage, Docker Containerization, & IaC**
- [ ] Day 15: Hardening, Global Error Handling Middleware, & Milestone Test