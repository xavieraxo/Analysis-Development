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
    }
}

