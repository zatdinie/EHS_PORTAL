using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace EHS_PORTAL.Areas.CLIP.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            UserPlants = new HashSet<UserPlant>();
            UserCompetencies = new HashSet<UserCompetency>();
        }

        public string EmpID { get; set; }
        public int? Atom_CEP { get; set; }
        public int? DOE_CPD { get; set; }
        public int? Dosh_CEP { get; set; }

        public virtual ICollection<UserPlant> UserPlants { get; set; }
        public virtual ICollection<UserCompetency> UserCompetencies { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<CompetencyModule> CompetencyModules { get; set; }
        public DbSet<UserCompetency> UserCompetencies { get; set; }
        public DbSet<Plant> Plants { get; set; }
        public DbSet<UserPlant> UserPlants { get; set; }
        public DbSet<AreaPlant> AreaPlants { get; set; }
        public DbSet<CertificateOfFitness> CertificateOfFitness { get; set; }
        public DbSet<Monitoring> Monitorings { get; set; }
        public DbSet<PlantMonitoring> PlantMonitorings { get; set; }
        public DbSet<MonitoringDocument> MonitoringDocuments { get; set; }
        public DbSet<ActivityTraining> ActivityTrainings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity Tables → CLIP schema
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers", "CLIP");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles", "CLIP");
            modelBuilder.Entity<IdentityUserRole>().ToTable("AspNetUserRoles", "CLIP");
            modelBuilder.Entity<IdentityUserLogin>().ToTable("AspNetUserLogins", "CLIP");
            modelBuilder.Entity<IdentityUserClaim>().ToTable("AspNetUserClaims", "CLIP");

            // Your Tables → CLIP schema
            MapToClip<UserCompetency>(modelBuilder, "UserCompetencies");
            MapToClip<CompetencyModule>(modelBuilder, "CompetencyModules");
            MapToClip<UserPlant>(modelBuilder, "UserPlants");
            MapToClip<Plant>(modelBuilder, "Plants");
            MapToClip<AreaPlant>(modelBuilder, "AreaPlant");
            MapToClip<CertificateOfFitness>(modelBuilder, "CertificateOfFitness");
            MapToClip<Monitoring>(modelBuilder, "Monitoring");
            MapToClip<PlantMonitoring>(modelBuilder, "PlantMonitoring");
            MapToClip<MonitoringDocument>(modelBuilder, "MonitoringDocument");
            MapToClip<ActivityTraining>(modelBuilder, "ActivityTrainings");
            MapToClip<ActivityLog>(modelBuilder, "ActivityLogs");

            // Relationships
            modelBuilder.Entity<UserCompetency>()
                .HasRequired(uc => uc.User)
                .WithMany(u => u.UserCompetencies)
                .HasForeignKey(uc => uc.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserCompetency>()
                .HasRequired(uc => uc.CompetencyModule)
                .WithMany(cm => cm.UserCompetencies)
                .HasForeignKey(uc => uc.CompetencyModuleId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserPlant>()
                .HasRequired(up => up.User)
                .WithMany(u => u.UserPlants)
                .HasForeignKey(up => up.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserPlant>()
                .HasRequired(up => up.Plant)
                .WithMany(p => p.UserPlants)
                .HasForeignKey(up => up.PlantId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CompetencyModule>()
                .Property(c => c.ModuleName)
                .IsRequired()
                .HasMaxLength(256);

            modelBuilder.Entity<CompetencyModule>()
                .HasIndex(c => c.ModuleName)
                .IsUnique();

            modelBuilder.Entity<CertificateOfFitness>()
                .HasRequired(cf => cf.Plant)
                .WithMany()
                .HasForeignKey(cf => cf.PlantId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlantMonitoring>()
                .HasRequired(pm => pm.Plant)
                .WithMany()
                .HasForeignKey(pm => pm.PlantID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PlantMonitoring>()
                .HasRequired(pm => pm.Monitoring)
                .WithMany(m => m.PlantMonitorings)
                .HasForeignKey(pm => pm.MonitoringID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MonitoringDocument>()
                .HasRequired(md => md.PlantMonitoring)
                .WithMany()
                .HasForeignKey(md => md.PlantMonitoringId)
                .WillCascadeOnDelete(false);
        }

        // Helper to reduce repetition
        private void MapToClip<TEntity>(DbModelBuilder modelBuilder, string tableName) where TEntity : class
        {
            modelBuilder.Entity<TEntity>().ToTable(tableName, "CLIP");
        }
    }
}
