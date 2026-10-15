using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;
using PawCare.Api.Models;
using PawCare.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token like this: Bearer {your token}"
    });

    options.AddSecurityRequirement(document =>
    {
        var schemeRef = new OpenApiSecuritySchemeReference("Bearer", document);
        return new OpenApiSecurityRequirement { [schemeRef] = new List<string>() };
    });
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStorageService, S3StorageService>();

// --- JWT Authentication setup ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "pawcare-default-token-signing-key-for-local-development";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "pawcare-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "pawcare-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
// --- end JWT setup ---

var app = builder.Build();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue<bool>("ApplyMigrationsOnStartup", false) || args.Contains("--migrate"))
    {
        app.Logger.LogInformation("Applying pending database migrations...");
        db.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully.");
    }

    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("SeedDefaultData", false))
    {
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
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();