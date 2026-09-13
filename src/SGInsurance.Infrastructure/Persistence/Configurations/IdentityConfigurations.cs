using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
        b.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.Mobile).HasColumnName("mobile").HasMaxLength(20);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.RoleId);
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.RoleCode).HasColumnName("role_code").IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.RoleCode).IsUnique();
        b.Property(x => x.RoleName).HasColumnName("role_name").IsRequired().HasMaxLength(100);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        b.HasKey(x => new { x.UserId, x.RoleId });
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
        b.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
    }
}
