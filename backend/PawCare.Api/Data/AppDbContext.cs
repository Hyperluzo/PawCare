using Microsoft.EntityFrameworkCore;
using PawCare.Api.Models;

namespace PawCare.Api.Data
{
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
}