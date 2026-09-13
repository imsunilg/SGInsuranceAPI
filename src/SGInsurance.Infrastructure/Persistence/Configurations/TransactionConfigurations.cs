using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(x => x.CustomerId);
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
        b.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Mobile).HasColumnName("mobile").IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.Mobile).IsUnique();
        b.Property(x => x.Dob).HasColumnName("dob");
        b.Property(x => x.Address).HasColumnName("address");
        b.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
        b.Property(x => x.State).HasColumnName("state").HasMaxLength(100);
        b.Property(x => x.Pincode).HasColumnName("pincode").HasMaxLength(10);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> b)
    {
        b.ToTable("quotes");
        b.HasKey(x => x.QuoteId);
        b.Property(x => x.QuoteId).HasColumnName("quote_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ProductCode).HasColumnName("product_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.LobCode).HasColumnName("lob_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.ProductData).HasColumnName("product_data").HasColumnType("jsonb");
        b.Property(x => x.SumInsured).HasColumnName("sum_insured").HasColumnType("numeric(14,2)");
        b.Property(x => x.BasePremium).HasColumnName("base_premium").HasColumnType("numeric(14,2)");
        b.Property(x => x.AddonPremium).HasColumnName("addon_premium").HasColumnType("numeric(14,2)");
        b.Property(x => x.NcbDiscount).HasColumnName("ncb_discount").HasColumnType("numeric(14,2)");
        b.Property(x => x.GstAmount).HasColumnName("gst_amount").HasColumnType("numeric(14,2)");
        b.Property(x => x.TotalPremium).HasColumnName("total_premium").HasColumnType("numeric(14,2)");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(30);
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasMany(x => x.QuoteAddons).WithOne().HasForeignKey(x => x.QuoteId);
    }
}

public class QuoteAddonConfiguration : IEntityTypeConfiguration<QuoteAddon>
{
    public void Configure(EntityTypeBuilder<QuoteAddon> b)
    {
        b.ToTable("quote_addons");
        b.HasKey(x => new { x.QuoteId, x.AddonCode });
        b.Property(x => x.QuoteId).HasColumnName("quote_id");
        b.Property(x => x.AddonCode).HasColumnName("addon_code").HasMaxLength(50);
        b.Property(x => x.ProductCode).HasColumnName("product_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(12,2)");
    }
}

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> b)
    {
        b.ToTable("proposals");
        b.HasKey(x => x.ProposalId);
        b.Property(x => x.ProposalId).HasColumnName("proposal_id");
        b.Property(x => x.QuoteId).HasColumnName("quote_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(30);
        b.Property(x => x.ProposalData).HasColumnName("proposal_data").HasColumnType("jsonb");
        b.Property(x => x.NomineeData).HasColumnName("nominee_data").HasColumnType("jsonb");
        b.Property(x => x.PaymentDueAt).HasColumnName("payment_due_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> b)
    {
        b.ToTable("kyc_verifications");
        b.HasKey(x => x.KycId);
        b.Property(x => x.KycId).HasColumnName("kyc_id");
        b.Property(x => x.ProposalId).HasColumnName("proposal_id");
        b.Property(x => x.Pan).HasColumnName("pan").IsRequired().HasMaxLength(20);
        b.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        b.Property(x => x.Dob).HasColumnName("dob");
        b.Property(x => x.Address).HasColumnName("address");
        b.Property(x => x.Result).HasColumnName("result").IsRequired().HasMaxLength(20);
        b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
    }
}

public class RiskVerificationConfiguration : IEntityTypeConfiguration<RiskVerification>
{
    public void Configure(EntityTypeBuilder<RiskVerification> b)
    {
        b.ToTable("risk_verifications");
        b.HasKey(x => x.VerificationId);
        b.Property(x => x.VerificationId).HasColumnName("verification_id");
        b.Property(x => x.ProposalId).HasColumnName("proposal_id");
        b.Property(x => x.VerificationType).HasColumnName("verification_type").IsRequired().HasMaxLength(30);
        b.Property(x => x.VerificationData).HasColumnName("verification_data").HasColumnType("jsonb");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(x => x.PaymentId);
        b.Property(x => x.PaymentId).HasColumnName("payment_id");
        b.Property(x => x.ProposalId).HasColumnName("proposal_id");
        b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(14,2)");
        b.Property(x => x.Mode).HasColumnName("mode").IsRequired().HasMaxLength(30);
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.PaymentId);
    }
}

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> b)
    {
        b.ToTable("payment_attempts");
        b.HasKey(x => x.AttemptId);
        b.Property(x => x.AttemptId).HasColumnName("attempt_id");
        b.Property(x => x.PaymentId).HasColumnName("payment_id");
        b.Property(x => x.AttemptNumber).HasColumnName("attempt_number");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(255);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> b)
    {
        b.ToTable("policies");
        b.HasKey(x => x.PolicyId);
        b.Property(x => x.PolicyId).HasColumnName("policy_id");
        b.Property(x => x.PolicyNumber).HasColumnName("policy_number").IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.PolicyNumber).IsUnique();
        b.Property(x => x.ProposalId).HasColumnName("proposal_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ProductCode).HasColumnName("product_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.LobCode).HasColumnName("lob_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.CoverageData).HasColumnName("coverage_data").HasColumnType("jsonb");
        b.Property(x => x.SumInsured).HasColumnName("sum_insured").HasColumnType("numeric(14,2)");
        b.Property(x => x.TotalPremium).HasColumnName("total_premium").HasColumnType("numeric(14,2)");
        b.Property(x => x.GstAmount).HasColumnName("gst_amount").HasColumnType("numeric(14,2)");
        b.Property(x => x.RiskStartDate).HasColumnName("risk_start_date");
        b.Property(x => x.RiskEndDate).HasColumnName("risk_end_date");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        b.Property(x => x.IssuedAt).HasColumnName("issued_at");
    }
}

public class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
{
    public void Configure(EntityTypeBuilder<PolicyDocument> b)
    {
        b.ToTable("policy_documents");
        b.HasKey(x => x.DocId);
        b.Property(x => x.DocId).HasColumnName("doc_id");
        b.Property(x => x.PolicyId).HasColumnName("policy_id");
        b.Property(x => x.FilePath).HasColumnName("file_path").IsRequired().HasMaxLength(500);
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.GeneratedAt).HasColumnName("generated_at");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(x => x.NotificationId);
        b.Property(x => x.NotificationId).HasColumnName("notification_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.TemplateCode).HasColumnName("template_code").IsRequired().HasMaxLength(50);
        b.Property(x => x.Channel).HasColumnName("channel").IsRequired().HasMaxLength(30);
        b.Property(x => x.Recipient).HasColumnName("recipient").IsRequired().HasMaxLength(255);
        b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        b.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        b.Property(x => x.SentAt).HasColumnName("sent_at");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.AuditId);
        b.Property(x => x.AuditId).HasColumnName("audit_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityType).HasColumnName("entity_type").IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        b.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}
