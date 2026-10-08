# PawCare — 15-Day Build Roadmap (Detailed)
*Based on Project Plan v1.1 — C# / ASP.NET Core / PostgreSQL / AWS*

Milestone target (Day 15): Admin creates Staff → Staff logs in → Admit Patient → Assign Ward → Add Treatment → Change Status → Owner gets emailed → Notification logged.

Each day below has: **Goal**, **Steps** (with actual commands/code where useful), **Watch out for**, and **Definition of done**.

---

## Day 1 — Inspect Frontend + Environment Setup

**Goal:** Know exactly what you're integrating with, and have a working local toolchain.

**Steps:**
1. Open the existing frontend project. Look for `package.json` (React/Vue/Angular), or plain `index.html` + JS files. Note:
   - Framework/build tool (React+Vite, Angular CLI, plain JS, etc.)
   - Where API calls would go — is there an `api/` or `services/` folder with placeholder fetch calls, or nothing yet?
   - Any existing auth token handling (localStorage, cookies) so your JWT approach matches what it expects.
2. Install tooling:
   ```bash
   # .NET SDK — check version
   dotnet --version   # want 8.x or later

   # PostgreSQL via Docker (simplest for local dev)
   docker run --name pawcare-db -e POSTGRES_PASSWORD=devpass -e POSTGRES_DB=pawcare -p 5432:5432 -d postgres:16

   # Verify it's up
   docker exec -it pawcare-db psql -U postgres -d pawcare -c "\dt"
   ```
3. Install pgAdmin or DBeaver (GUI to browse the DB), Postman, and confirm `git --version`.
4. Repo setup:
   ```bash
   mkdir pawcare && cd pawcare
   git init
   mkdir backend frontend
   dotnet new gitignore   # generates a solid .NET .gitignore
   ```
   Add to `.gitignore` explicitly: `appsettings.Development.json`, `appsettings.Production.json`, `*.env`.
5. Push an empty commit and confirm GitHub remote works (`git remote add origin ...`, `git push -u origin main`).

**Watch out for:** Don't commit real credentials even in "Development" config — use `dotnet user-secrets` instead (set up on Day 3).

**Definition of done:** You can describe the frontend's tech stack in one sentence, Postgres is reachable on `localhost:5432`, and you have an empty backend/frontend repo pushed to GitHub.

---

## Day 2 — ASP.NET Core Project Scaffold

**Goal:** A running Web API with Swagger and a clean folder structure.

**Steps:**
1. Scaffold:
   ```bash
   cd backend
   dotnet new webapi -n PawCare.Api -controllers
   cd PawCare.Api
   dotnet run
   ```
   Visit `https://localhost:<port>/swagger` — confirm the default `WeatherForecast` sample loads.
2. Delete the sample `WeatherForecastController.cs` and model. Create folders:
   ```
   Controllers/
   Models/          (EF entities — Day 3)
   DTOs/            (request/response shapes, never expose entities directly)
   Services/        (business logic)
   Repositories/     (optional — or use EF directly in Services)
   Data/            (DbContext)
   ```
3. Add a health check controller as your first real endpoint:
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class PingController : ControllerBase
   {
       [HttpGet]
       public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });
   }
   ```
4. Configure CORS in `Program.cs` so your frontend's dev origin (e.g. `http://localhost:5173`) can call the API:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("Frontend", policy =>
           policy.WithOrigins("http://localhost:5173")
                 .AllowAnyHeader()
                 .AllowAnyMethod());
   });
   // ...
   app.UseCors("Frontend");
   ```

**Watch out for:** CORS must be configured *before* `app.UseAuthorization()` in the middleware pipeline, and the origin must match exactly (no trailing slash).

**Definition of done:** `dotnet run` starts cleanly, `GET /api/ping` returns 200 with JSON, Swagger UI shows it, and a test fetch from the frontend's dev server succeeds (no CORS error in browser console).

---

## Day 3 — PostgreSQL + EF Core Setup

**Goal:** Entities defined, DbContext wired up, connection confirmed.

**Steps:**
1. Install packages:
   ```bash
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   dotnet add package Microsoft.EntityFrameworkCore.Design
   dotnet tool install --global dotnet-ef   # if not already installed
   ```
2. Set the connection string via user-secrets (not appsettings):
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=pawcare;Username=postgres;Password=devpass"
   ```
3. Create entity classes in `Models/` matching Section 8 of the plan. Example for two of them:
   ```csharp
   public class User
   {
       public int Id { get; set; }
       public string Name { get; set; } = default!;
       public string Email { get; set; } = default!;
       public string PasswordHash { get; set; } = default!;
       public string AccountType { get; set; } = default!; // "ADMIN" or "STAFF"
       public bool IsActive { get; set; } = true;
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
       public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
   }

   public class Patient
   {
       public int Id { get; set; }
       public string Name { get; set; } = default!;
       public string Species { get; set; } = default!;
       public string Breed { get; set; } = default!;
       public int Age { get; set; }
       public string Gender { get; set; } = default!;
       public int OwnerId { get; set; }
       public Owner Owner { get; set; } = default!;
       public int WardId { get; set; }
       public Ward Ward { get; set; } = default!;
       public string AdmissionStatus { get; set; } = "ADMITTED";
       public string ClinicalStatus { get; set; } = "UNDER_TREATMENT";
       public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;
       public DateTime? DischargedAt { get; set; }
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   }
   ```
   Repeat similarly for `Owner`, `Ward`, `Treatment`, `StatusHistory`, `Notification` using the field lists in Section 8.
4. Create `Data/AppDbContext.cs`:
   ```csharp
   public class AppDbContext : DbContext
   {
       public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
       public DbSet<User> Users => Set<User>();
       public DbSet<Owner> Owners => Set<Owner>();
       public DbSet<Ward> Wards => Set<Ward>();
       public DbSet<Patient> Patients => Set<Patient>();
       public DbSet<Treatment> Treatments => Set<Treatment>();
       public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
       public DbSet<Notification> Notifications => Set<Notification>();
   }
   ```
5. Register it in `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
   ```

**Watch out for:** Foreign key navigation properties (`Owner`, `Ward`) need the FK id property (`OwnerId`, `WardId`) alongside them, or EF Core can't infer the relationship correctly.

**Definition of done:** Project builds with zero errors; `AppDbContext` is registered; you can inject it into a throwaway controller and call `.Database.CanConnectAsync()` successfully.

---

## Day 4 — Migrations + Seed Data

**Goal:** Real tables in Postgres, with one Admin account to log in with.

**Steps:**
1. Generate and apply the first migration:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
2. Inspect the generated tables in pgAdmin/DBeaver or via psql:
   ```bash
   docker exec -it pawcare-db psql -U postgres -d pawcare -c "\dt"
   ```
3. Seed an admin user + a couple of wards. Simplest approach — a seed method called once at startup in Development:
   ```csharp
   if (app.Environment.IsDevelopment())
   {
       using var scope = app.Services.CreateScope();
       var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
       if (!db.Users.Any())
       {
           db.Users.Add(new User {
               Name = "Admin", Email = "admin@pawcare.local",
               PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
               AccountType = "ADMIN"
           });
           db.Wards.AddRange(
               new Ward { Name = "General Ward", Description = "General care", Capacity = 20 },
               new Ward { Name = "ICU", Description = "Critical care", Capacity = 6 }
           );
           db.SaveChanges();
       }
   }
   ```
   (This anticipates Day 5's BCrypt package — install it now: `dotnet add package BCrypt.Net-Next`.)

**Watch out for:** Never seed this way in Production — gate it strictly behind `IsDevelopment()`.

**Definition of done:** Tables exist in Postgres matching Section 8's schema; querying `SELECT * FROM users;` shows one admin row with a hashed (not plaintext) password.

---

## Day 5 — Authentication (Login + JWT)

**Goal:** A working `/api/auth/login` that returns a real JWT.

**Steps:**
1. Install JWT package:
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   ```
2. Add JWT settings via user-secrets:
   ```bash
   dotnet user-secrets set "Jwt:Key" "a-long-random-secret-at-least-32-chars"
   dotnet user-secrets set "Jwt:Issuer" "PawCareApi"
   dotnet user-secrets set "Jwt:Audience" "PawCareClient"
   ```
3. Configure auth in `Program.cs`:
   ```csharp
   var jwtKey = builder.Configuration["Jwt:Key"]!;
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = builder.Configuration["Jwt:Issuer"],
               ValidAudience = builder.Configuration["Jwt:Audience"],
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
           };
       });
   builder.Services.AddAuthorization();
   // ...
   app.UseAuthentication();
   app.UseAuthorization();
   ```
4. Build the login endpoint:
   ```csharp
   [HttpPost("login")]
   public async Task<IActionResult> Login(LoginRequest request)
   {
       var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
       if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
           return Unauthorized(new { message = "Invalid credentials" });

       var claims = new[] {
           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
           new Claim(ClaimTypes.Email, user.Email),
           new Claim(ClaimTypes.Role, user.AccountType)
       };
       var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256);
       var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
       return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
   }
   ```
5. Test in Postman: `POST /api/auth/login` with the seeded admin credentials → copy the `token` → add a test `[Authorize]` endpoint → call it with `Authorization: Bearer <token>` header.

**Watch out for:** `IsActive` check must happen *before* password verification is reported as failed vs inactive — but return the same generic "Invalid credentials" message either way, so you don't leak which accounts exist.

**Definition of done:** Login returns a JWT for correct credentials, 401 for wrong ones, and a protected endpoint accepts the token.

---

## Day 6 — Authorization + Admin User Management

**Goal:** ADMIN-only endpoints that STAFF genuinely cannot call, even by hand-crafting requests.

**Steps:**
1. Add role-based authorization policy (or just use `[Authorize(Roles = "ADMIN")]` directly since you already put the role in the JWT claims on Day 5).
2. Build `AdminController`:
   ```csharp
   [ApiController]
   [Route("api/admin/users")]
   [Authorize(Roles = "ADMIN")]
   public class AdminUsersController : ControllerBase
   {
       [HttpGet]                          // GET /api/admin/users
       [HttpPost]                         // POST /api/admin/users — create staff
       [HttpPut("{id}")]                  // PUT /api/admin/users/{id} — edit
       [HttpPatch("{id}/status")]         // PATCH .../status — activate/deactivate
       [HttpPost("{id}/reset-password")]  // POST .../reset-password
   }
   ```
3. Reset-password: generate a temp password or a reset token; for a first pass, simplest is admin sets a new password directly (hash it, save it, force nothing fancy yet — you can add "must change on next login" later).
4. Test role protection explicitly: log in as a STAFF user (you'll need to create one via the admin endpoint first, using the admin token), then try calling `/api/admin/users` with the STAFF token — confirm you get **403 Forbidden**, not 401.

**Watch out for:** `[Authorize(Roles = "ADMIN")]` only works if the role claim type matches what `ClaimTypes.Role` expects — double check the claim you set on Day 5 is exactly `ClaimTypes.Role`.

**Definition of done:** Admin token can list/create/edit/deactivate/reactivate users and reset passwords; a STAFF token gets 403 on every one of those routes.

---

## Day 7 — Ward CRUD

**Goal:** Wards manageable via API, wired into the frontend.

**Steps:**
1. `WardsController`:
   - `GET /api/wards` — list all
   - `POST /api/wards` — create (Admin or Staff, per Section 4's table — both allowed)
   - `PUT /api/wards/{id}` — edit
   - `GET /api/wards/{id}/patients` — patients in that ward (will return empty until Day 8)
2. Use DTOs, not raw entities, in responses (`WardDto { Id, Name, Description, Capacity, PatientCount }`) — `PatientCount` computed via `.Include(w => w.Patients).Count()` or a separate count query.
3. Wire the frontend's ward screen to these endpoints — replace any mock/hardcoded data.

**Watch out for:** `[Authorize]` (no role restriction) is enough here since both roles can manage wards per Section 4 — don't accidentally restrict to ADMIN only.

**Definition of done:** Wards can be created/edited/listed from the actual UI, not just Postman.

---

## Day 8 — Patient & Owner Management

**Goal:** Full admission flow working end-to-end.

**Steps:**
1. Design the admission DTO to accept both new-owner and existing-owner cases:
   ```csharp
   public class AdmitPatientRequest
   {
       public string PatientName { get; set; }
       public string Species { get; set; }
       public string Breed { get; set; }
       public int Age { get; set; }
       public string Gender { get; set; }
       public int WardId { get; set; }
       public int? ExistingOwnerId { get; set; }     // if set, reuse
       public OwnerDto? NewOwner { get; set; }        // else create
   }
   ```
2. `POST /api/patients`: if `ExistingOwnerId` given, validate it exists; else create the `Owner` row first, then create the `Patient` row referencing it and the ward.
3. `GET /api/patients/{id}` — return patient + owner + ward + treatment/status summary.
4. `PUT /api/patients/{id}` — edit patient fields (not owner — keep that separate/simple for now).
5. Validate `WardId` exists and has capacity remaining before allowing admission (optional but good practice given Section 8 has `capacity`).

**Watch out for:** Don't let `PUT` silently allow changing `OwnerId`/`WardId` without going through proper checks — treat ward reassignment as a deliberate action if you add it later.

**Definition of done:** You can admit a new patient (with a brand-new owner) through the actual UI form and see it appear correctly in the ward.

---

## Day 9 — Search & Filter

**Goal:** Patient list screen actually searchable against real data.

**Steps:**
1. `GET /api/patients?name=&species=&breed=&owner=&status=&page=1&pageSize=20`
2. Build the query conditionally:
   ```csharp
   var query = _db.Patients.Include(p => p.Owner).Include(p => p.Ward).AsQueryable();
   if (!string.IsNullOrEmpty(name)) query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name}%"));
   if (!string.IsNullOrEmpty(species)) query = query.Where(p => p.Species == species);
   if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.ClinicalStatus == status);
   var total = await query.CountAsync();
   var results = await query.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
   return Ok(new { total, page, pageSize, results });
   ```
3. `EF.Functions.ILike` gives case-insensitive search in Postgres — nicer than `.ToLower().Contains()` which doesn't translate as well to SQL.

**Watch out for:** Add a database index on commonly-filtered columns (`species`, `clinical_status`) once you have real data volume — not critical at 20 test patients, but worth a migration note for later.

**Definition of done:** Typing a partial name or picking a status filter in the UI returns the right subset.

---

## Day 10 — Treatment Records

**Goal:** Treatments logged and attributable.

**Steps:**
1. `POST /api/patients/{id}/treatments`:
   ```csharp
   [HttpPost("{id}/treatments")]
   [Authorize]
   public async Task<IActionResult> AddTreatment(int id, TreatmentRequest request)
   {
       var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
       var treatment = new Treatment {
           PatientId = id, UpdatedByUserId = userId,
           Diagnosis = request.Diagnosis, Treatment = request.Treatment,
           Medication = request.Medication, Notes = request.Notes
       };
       _db.Treatments.Add(treatment);
       await _db.SaveChangesAsync();
       return Ok(treatment);
   }
   ```
2. `GET /api/patients/{id}` response should now include the treatment list (ordered newest-first) with the staff member's name joined in, not just their id.

**Watch out for:** Always pull `updated_by_user_id` from the JWT claims server-side — never trust a `userId` field sent in the request body, or any staff member could log treatments under someone else's name.

**Definition of done:** Adding a treatment from the UI shows up in that patient's history with the correct logged-in staff name attached.

---

## Day 11 — Patient Status Workflow

**Goal:** Status changes are validated, audited, and DECEASED is gated behind confirmation.

**Steps:**
1. Define allowed statuses as constants/enums matching Section 8:
   - Admission: `ADMITTED`, `DISCHARGED`, `DECEASED`
   - Clinical: `UNDER_TREATMENT`, `STABLE`, `CRITICAL`, `RECOVERING`, `UNDER_OBSERVATION`
2. `PATCH /api/patients/{id}/status`:
   ```csharp
   public class StatusChangeRequest
   {
       public string NewClinicalStatus { get; set; }
       public string? NewAdmissionStatus { get; set; }
       public bool Confirm { get; set; } = false;   // must be true for DECEASED
   }
   ```
   ```csharp
   if (request.NewAdmissionStatus == "DECEASED" && !request.Confirm)
       return BadRequest(new { message = "DECEASED requires explicit confirmation." });

   var oldStatus = patient.ClinicalStatus;
   patient.ClinicalStatus = request.NewClinicalStatus;
   if (request.NewAdmissionStatus != null) patient.AdmissionStatus = request.NewAdmissionStatus;

   _db.StatusHistories.Add(new StatusHistory {
       PatientId = patient.Id, OldStatus = oldStatus, NewStatus = request.NewClinicalStatus,
       ChangedByUserId = userId, ChangedAt = DateTime.UtcNow
   });
   await _db.SaveChangesAsync();   // commit BEFORE attempting any email (see Day 12)
   ```
3. On the frontend, DECEASED should trigger a confirmation modal before sending `confirm: true`.

**Watch out for:** This is the step most worth not rushing — everything in Day 12 (notifications) and the audit trail depends on this being correct and committed independently of email success.

**Definition of done:** Status changes save with full before/after/who/when history; attempting DECEASED without `confirm: true` is rejected.

---

## Day 12 — Email Notifications (AWS SES)

**Goal:** Owner emails send automatically and reliably, decoupled from the status save.

**Steps:**
1. AWS console: create an SES identity (verify your own email as sender while in sandbox mode), and verify a second email you control as a test recipient (sandbox SES can only send to verified addresses).
2. Create an IAM user/role with `ses:SendEmail` permission only — don't use root/admin credentials in code.
3. Install SDK: `dotnet add package AWSSDK.SimpleEmail`.
4. Build a `NotificationService`:
   ```csharp
   public async Task SendStatusEmail(Patient patient, string notificationType)
   {
       var notification = new Notification {
           PatientId = patient.Id, OwnerId = patient.OwnerId,
           NotificationType = notificationType, RecipientEmail = patient.Owner.Email,
           DeliveryStatus = "PENDING"
       };
       _db.Notifications.Add(notification);
       await _db.SaveChangesAsync();

       try
       {
           var (subject, body) = BuildTemplate(notificationType, patient);
           await _sesClient.SendEmailAsync(new SendEmailRequest { /* ... */ });
           notification.DeliveryStatus = "SENT";
           notification.SentAt = DateTime.UtcNow;
       }
       catch (Exception ex)
       {
           notification.DeliveryStatus = "FAILED";
           notification.ErrorMessage = ex.Message;
       }
       await _db.SaveChangesAsync();
   }
   ```
5. Call this **after** the status update from Day 11 has already committed — call it in a way that its failure can't roll back the status change (separate try/catch, separate SaveChanges, or fire it via a background task/queue if you want to go further).
6. Build the three templates (UNDER_TREATMENT, TREATED/DISCHARGED, DECEASED) with owner name, patient name, hospital info interpolated.
7. Add a simple retry: `POST /api/admin/notifications/{id}/retry` (or a scheduled job) that re-attempts any `FAILED` row.

**Watch out for:** SES sandbox mode blocks sending to unverified addresses — fine for dev/testing, but you'll need to request production access before real owner emails work.

**Definition of done:** Changing a patient's status sends a real email to your verified test address, and if you temporarily break the SES credentials on purpose, the status update still saves and the notification is logged as FAILED (not silently lost).

---

## Day 13 — Dashboard Statistics

**Goal:** Live numbers, not hardcoded ones.

**Steps:**
1. `GET /api/dashboard/stats`:
   ```csharp
   var stats = new {
       TotalPatients = await _db.Patients.CountAsync(),
       UnderTreatment = await _db.Patients.CountAsync(p => p.ClinicalStatus == "UNDER_TREATMENT"),
       TreatedToday = await _db.Patients.CountAsync(p => p.AdmissionStatus == "DISCHARGED" && p.DischargedAt.Value.Date == DateTime.UtcNow.Date),
       DeceasedMTD = await _db.Patients.CountAsync(p => p.AdmissionStatus == "DECEASED" && p.UpdatedAt.Month == DateTime.UtcNow.Month && p.UpdatedAt.Year == DateTime.UtcNow.Year)
   };
   return Ok(stats);
   ```
2. Connect the dashboard cards in the frontend to this endpoint on load (and optionally refresh on an interval or after any status change).

**Watch out for:** "Deceased Month-to-Date" needs a clear definition of which timestamp counts (when marked deceased, from `StatusHistory`, is more accurate than `UpdatedAt` on the patient row — consider querying `StatusHistories` instead for precision).

**Definition of done:** Admit/treat/discharge a patient in the UI and watch the dashboard numbers update to match.

---

## Day 14 — AWS Deployment

**Goal:** Backend running in AWS against RDS, files in S3.

**Steps:**
1. **RDS:** Create a PostgreSQL RDS instance (free tier eligible size for now). Note the endpoint, and open its security group to allow inbound from your EC2 instance's security group (not `0.0.0.0/0`).
2. Run migrations against RDS: temporarily point your connection string at the RDS endpoint and run `dotnet ef database update` from your machine (or from a one-off deploy step).
3. **S3:** Create a bucket for patient images/reports. Store only the S3 object key/URL on the `Patient`/`Treatment` row, never the file itself in Postgres.
4. **Docker:** Write a `Dockerfile`:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   WORKDIR /src
   COPY . .
   RUN dotnet publish -c Release -o /app

   FROM mcr.microsoft.com/dotnet/aspnet:8.0
   WORKDIR /app
   COPY --from=build /app .
   ENTRYPOINT ["dotnet", "PawCare.Api.dll"]
   ```
   Build and test locally: `docker build -t pawcare-api . && docker run -p 8080:8080 --env-file .env pawcare-api`.
5. **EC2:** Launch an instance, install Docker, pull/run your image (or push to ECR first if you want a cleaner pipeline). Pass secrets via environment variables at container runtime, never baked into the image.
6. Set env vars on the instance for: `ConnectionStrings__Default`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, AWS credentials (or better, use an EC2 IAM role instead of hardcoded keys for SES/S3 access).

**Watch out for:** RDS security group misconfiguration is the #1 blocker here — start by confirming you can `psql` into RDS from the EC2 instance before worrying about the app itself.

**Definition of done:** The API responds on the EC2 public address/DNS, reads/writes to RDS, and can send an email via SES from that environment.

---

## Day 15 — Hardening + Milestone Test

**Goal:** Confirm everything works together, and it's not fragile.

**Steps:**
1. Add global exception handling middleware so unhandled errors return a consistent JSON shape instead of a stack trace:
   ```csharp
   app.UseExceptionHandler(errApp => errApp.Run(async context => {
       context.Response.StatusCode = 500;
       context.Response.ContentType = "application/json";
       await context.Response.WriteAsync("{\"message\":\"Something went wrong.\"}");
   }));
   ```
2. Add input validation (FluentValidation, or data annotations + `[ApiController]`'s automatic 400 responses) on every request DTO — especially `AdmitPatientRequest` and `StatusChangeRequest`.
3. Re-audit every controller: confirm ADMIN-only routes have `[Authorize(Roles="ADMIN")]`, everything else at minimum has `[Authorize]`.
4. Polish Swagger: add XML doc comments or `[ProducesResponseType]` attributes so the docs are actually useful as a reference.
5. Set an RDS automated backup window/retention (a few clicks in the RDS console).
6. Build a Postman collection covering the full milestone and save it to the repo (`docs/PawCare.postman_collection.json`) so anyone can re-run it:
   - Login as Admin → create Staff → login as Staff → admit Patient → assign Ward (already done at admission) → view Patient → add Treatment → change Status (with confirm for DECEASED case) → confirm StatusHistory row exists → confirm Notification row is SENT.
7. Run it end-to-end against the deployed AWS environment, not just localhost.

**Watch out for:** Test the DECEASED path specifically — it's the one place notification content is sensitive and the confirmation gate is most important to verify.

**Definition of done:** The full milestone flow runs clean against production infrastructure, with no unhandled exceptions and no unprotected admin route.

---

## How to use this
- Don't move to the next day until that day's "Definition of done" is genuinely true — Days 5–6 and Day 11 are load-bearing for everything after them.
- If you fall behind, it's better to slow down on foundation days (1–6) than to rush and hit auth/authorization bugs later that are painful to retrofit.
- Keep a running `CHANGELOG.md` or commit messages that map to these days — useful both for tracking progress and for explaining your work later.
