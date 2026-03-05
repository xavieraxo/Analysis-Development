using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data;

// Heredar de IdentityDbContext para soportar ASP.NET Core Identity
public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,      // TUser
    ApplicationRole,      // TRole
    int,                  // TKey (tipo de ID)
    IdentityUserClaim<int>,
    IdentityUserRole<int>,
    IdentityUserLogin<int>,
    IdentityRoleClaim<int>,
    IdentityUserToken<int>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // MANTENER tabla User original (sistema actual con BCrypt)
    public new DbSet<User> Users { get; set; }
    
    // NUEVA tabla para Identity (coexiste durante migración)
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectLog> ProjectLogs { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<Behavior> Behaviors { get; set; }
    public DbSet<PasswordRecoveryToken> PasswordRecoveryTokens { get; set; }
    public DbSet<DevFlowRun> DevFlowRuns { get; set; }
    public DbSet<DevFlowArtifact> DevFlowArtifacts { get; set; }
    public DbSet<DevFlowGate> DevFlowGates { get; set; }
    public DbSet<BranchPlan> BranchPlans { get; set; }
    public DbSet<BranchPlanItem> BranchPlanItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Llamar al base PRIMERO para configurar Identity
        base.OnModelCreating(modelBuilder);

        // Configurar nombres de tablas de Identity (evitar conflicto con tabla Users original)
        modelBuilder.Entity<ApplicationUser>().ToTable("IdentityUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("IdentityRoles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("IdentityUserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("IdentityUserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("IdentityUserLogins");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("IdentityRoleClaims");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("IdentityUserTokens");

        // Configuración personalizada de ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configuración de User ORIGINAL (mantener tabla "Users")
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users"); // Explícitamente mantener nombre de tabla
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        // Configuración de Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Projects)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        // Configuración de ProjectLog
        modelBuilder.Entity<ProjectLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Logs)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de SystemConfiguration
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationType, e.Key }).IsUnique();
            entity.Property(e => e.ConfigurationType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MatchPattern).HasMaxLength(500);
        });

        modelBuilder.Entity<Behavior>(entity =>
        {
            entity.HasKey(e => e.AgentRole);
            entity.Property(e => e.AgentRole).IsRequired();
            entity.Property(e => e.Alias).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Prompt).IsRequired();
            entity.Property(e => e.InstructionsJson).IsRequired();
        });

        modelBuilder.Entity<PasswordRecoveryToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
            entity.Property(e => e.Expiration).IsRequired();
            entity.Property(e => e.Used).IsRequired();
            entity.HasIndex(e => new { e.Email, e.Code });
        });

        modelBuilder.Entity<DevFlowRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.ProjectId);
        });

        modelBuilder.Entity<DevFlowArtifact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.Stage).IsRequired();
            entity.Property(e => e.AgentRole).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasOne(e => e.DevFlowRun)
                  .WithMany(r => r.Artifacts)
                  .HasForeignKey(e => e.DevFlowRunId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.DevFlowRunId);
            entity.HasIndex(e => new { e.DevFlowRunId, e.Stage });
        });

        modelBuilder.Entity<DevFlowGate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Stage).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.DecisionComment).HasMaxLength(1000);
            entity.HasOne(e => e.DevFlowRun)
                  .WithMany(r => r.Gates)
                  .HasForeignKey(e => e.DevFlowRunId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DecidedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.DecidedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.DevFlowRunId);
            entity.HasIndex(e => new { e.DevFlowRunId, e.Stage });
        });

        modelBuilder.Entity<BranchPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.FormatVersion).IsRequired();
            entity.HasOne(e => e.DevFlowRun)
                  .WithOne(r => r.BranchPlan)
                  .HasForeignKey<BranchPlan>(e => e.DevFlowRunId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.DevFlowRunId).IsUnique();
            entity.HasIndex(e => e.CreatedByUserId);
        });

        modelBuilder.Entity<BranchPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoryId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TaskId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Area).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BranchName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Order).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasOne(e => e.BranchPlan)
                  .WithMany(p => p.Items)
                  .HasForeignKey(e => e.BranchPlanId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.BranchPlanId);
            entity.HasIndex(e => e.BranchName);
            entity.HasIndex(e => new { e.BranchPlanId, e.TaskId }).IsUnique();
        });
    }
}

