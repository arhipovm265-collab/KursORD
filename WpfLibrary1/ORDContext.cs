using Microsoft.EntityFrameworkCore;
using WpfLibrary1;

namespace WpfLibrary1.Data
{
    public class ORDContext : DbContext
    {
        public ORDContext()
        {
        }

        public ORDContext(DbContextOptions<ORDContext> options) : base(options)
        {
        }

        public DbSet<Officer> Officers { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<OfficerRank> OfficerRanks { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<CaseRecord> CaseRecords { get; set; } = null!;
        public DbSet<Suspect> Suspects { get; set; } = null!;
        public DbSet<Evidence> Evidences { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<CaseStatusHistory> CaseStatusHistories { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var conn = "Server=.\\SQLEXPRESS;Database=ORD_Schema;Trusted_Connection=True;TrustServerCertificate=True;";
                optionsBuilder.UseSqlServer(conn);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<CaseRecord>()
                .HasIndex(c => c.CaseNumber)
                .IsUnique();

            modelBuilder.Entity<Evidence>().ToTable("Evidence");

            modelBuilder.Entity<CaseRecord>()
                .HasOne(c => c.LeadOfficer)
                .WithMany(o => o.CasesLed)
                .HasForeignKey(c => c.LeadOfficerId)
                .OnDelete(DeleteBehavior.SetNull);

            
            modelBuilder.Entity<CaseRecord>()
                .HasMany(c => c.Suspects)
                .WithMany(s => s.Cases)
                .UsingEntity<Dictionary<string, object>>(
                    "CaseRecordSuspects",
                    j => j.HasOne<Suspect>().WithMany().HasForeignKey("SuspectId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<CaseRecord>().WithMany().HasForeignKey("CaseRecordId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("CaseRecordId", "SuspectId");
                        j.ToTable("CaseRecordSuspects");
                    });

            
            modelBuilder.Entity<CaseStatusHistory>()
                .HasOne(h => h.CaseRecord)
                .WithMany(c => c.StatusHistories)
                .HasForeignKey(h => h.CaseRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Evidence>()
                .HasOne(e => e.CollectedBy)
                .WithMany(o => o.CollectedEvidence)
                .HasForeignKey(e => e.CollectedByOfficerId)
                .OnDelete(DeleteBehavior.SetNull);

            
            modelBuilder.Entity<Evidence>()
                .HasOne(e => e.CaseRecord)
                .WithMany(c => c.EvidenceItems)
                .HasForeignKey(e => e.CaseRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.OfficerId)
                .IsUnique()
                .HasFilter(null);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Officer)
                .WithOne(o => o.User)
                .HasForeignKey<User>(u => u.OfficerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
