using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Infrastructure.Persistence.Configurations;

public class LobMasterConfiguration : IEntityTypeConfiguration<LobMaster>
{
    public void Configure(EntityTypeBuilder<LobMaster> b)
    {
        b.ToTable("lob_master");
        b.HasKey(x => x.LobCode);
        b.Property(x => x.LobCode).HasColumnName("lob_code").HasMaxLength(50);
        b.Property(x => x.LobName).HasColumnName("lob_name").IsRequired().HasMaxLength(150);
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}

public class ProductMasterConfiguration : IEntityTypeConfiguration<ProductMaster>
{
    public void Configure(EntityTypeBuilder<ProductMaster> b)
    {
        b.ToTable("product_master");
        b.HasKey(x => x.ProductCode);
        b.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(50);
        b.Property(x => x.LobCode).HasColumnName("lob_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.ProductName).HasColumnName("product_name").IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
        b.Property(x => x.RatingStrategyKey).HasColumnName("rating_strategy_key").IsRequired().HasMaxLength(50);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasOne(x => x.Lob).WithMany(l => l.Products).HasForeignKey(x => x.LobCode);
    }
}

public class ProductAddonConfiguration : IEntityTypeConfiguration<ProductAddon>
{
    public void Configure(EntityTypeBuilder<ProductAddon> b)
    {
        b.ToTable("product_addons");
        b.HasKey(x => new { x.AddonCode, x.ProductCode });
        b.Property(x => x.AddonCode).HasColumnName("addon_code").HasMaxLength(50);
        b.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(50);
        b.Property(x => x.AddonName).HasColumnName("addon_name").IsRequired().HasMaxLength(150);
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.BasePrice).HasColumnName("base_price").HasColumnType("numeric(12,2)");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.HasOne(x => x.Product).WithMany(p => p.Addons).HasForeignKey(x => x.ProductCode);
    }
}

public class PremiumRuleConfiguration : IEntityTypeConfiguration<PremiumRule>
{
    public void Configure(EntityTypeBuilder<PremiumRule> b)
    {
        b.ToTable("premium_rules");
        b.HasKey(x => x.RuleId);
        b.Property(x => x.RuleId).HasColumnName("rule_id");
        b.Property(x => x.ProductCode).HasColumnName("product_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.RuleType).HasColumnName("rule_type").IsRequired().HasMaxLength(50);
        b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
        b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
        b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        b.HasOne(x => x.Product).WithMany(p => p.PremiumRules).HasForeignKey(x => x.ProductCode);
    }
}

public class VehicleMakeConfiguration : IEntityTypeConfiguration<VehicleMake>
{
    public void Configure(EntityTypeBuilder<VehicleMake> b)
    {
        b.ToTable("vehicle_makes");
        b.HasKey(x => x.MakeCode);
        b.Property(x => x.MakeCode).HasColumnName("make_code").HasMaxLength(50);
        b.Property(x => x.MakeName).HasColumnName("make_name").IsRequired().HasMaxLength(100);
    }
}

public class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> b)
    {
        b.ToTable("vehicle_models");
        b.HasKey(x => x.ModelCode);
        b.Property(x => x.ModelCode).HasColumnName("model_code").HasMaxLength(50);
        b.Property(x => x.MakeCode).HasColumnName("make_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.ModelName).HasColumnName("model_name").IsRequired().HasMaxLength(100);
    }
}

public class FuelTypeConfiguration : IEntityTypeConfiguration<FuelType>
{
    public void Configure(EntityTypeBuilder<FuelType> b)
    {
        b.ToTable("fuel_types");
        b.HasKey(x => x.FuelCode);
        b.Property(x => x.FuelCode).HasColumnName("fuel_code").HasMaxLength(30);
        b.Property(x => x.FuelName).HasColumnName("fuel_name").IsRequired().HasMaxLength(50);
    }
}

public class PaymentModeConfiguration : IEntityTypeConfiguration<PaymentMode>
{
    public void Configure(EntityTypeBuilder<PaymentMode> b)
    {
        b.ToTable("payment_modes");
        b.HasKey(x => x.ModeCode);
        b.Property(x => x.ModeCode).HasColumnName("mode_code").HasMaxLength(30);
        b.Property(x => x.ModeName).HasColumnName("mode_name").IsRequired().HasMaxLength(80);
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> b)
    {
        b.ToTable("notification_templates");
        b.HasKey(x => x.TemplateCode);
        b.Property(x => x.TemplateCode).HasColumnName("template_code").HasMaxLength(50);
        b.Property(x => x.Channel).HasColumnName("channel").IsRequired().HasMaxLength(30);
        b.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
        b.Property(x => x.BodyTemplate).HasColumnName("body_template").IsRequired();
        b.Property(x => x.Placeholders).HasColumnName("placeholders").HasColumnType("jsonb");
        b.Property(x => x.IsActive).HasColumnName("is_active");
    }
}
