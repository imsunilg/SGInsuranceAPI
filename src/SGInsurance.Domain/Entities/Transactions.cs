namespace SGInsurance.Domain.Entities;

public class Customer
{
    public Guid CustomerId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Mobile { get; set; } = default!;
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public static class QuoteStatus
{
    public const string Created = "CREATED";
    public const string Recalculated = "RECALCULATED";
    public const string Converted = "CONVERTED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";
}

public class Quote
{
    public Guid QuoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public string ProductData { get; set; } = "{}";
    public decimal SumInsured { get; set; }
    public decimal BasePremium { get; set; }
    public decimal AddonPremium { get; set; }
    public decimal NcbDiscount { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalPremium { get; set; }
    public string Status { get; set; } = QuoteStatus.Created;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<QuoteAddon> QuoteAddons { get; set; } = new List<QuoteAddon>();
}

public class QuoteAddon
{
    public Guid QuoteId { get; set; }
    public string AddonCode { get; set; } = default!;
    public string ProductCode { get; set; } = default!;
    public decimal Price { get; set; }
}

public static class ProposalStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string KycPending = "KYC_PENDING";
    public const string VerificationPending = "VERIFICATION_PENDING";
    public const string PaymentPending = "PAYMENT_PENDING";
    public const string PaymentFailed = "PAYMENT_FAILED";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
}

public class Proposal
{
    public Guid ProposalId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = ProposalStatus.Draft;
    public string ProposalData { get; set; } = "{}";
    public string NomineeData { get; set; } = "{}";
    public DateTimeOffset? PaymentDueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class KycVerification
{
    public Guid KycId { get; set; }
    public Guid ProposalId { get; set; }
    public string Pan { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
    public string Result { get; set; } = default!; // VERIFIED, FAILED, PENDING
    public DateTimeOffset VerifiedAt { get; set; }
}

public class RiskVerification
{
    public Guid VerificationId { get; set; }
    public Guid ProposalId { get; set; }
    public string VerificationType { get; set; } = default!;
    public string VerificationData { get; set; } = "{}";
    public string Status { get; set; } = "PENDING"; // PENDING, PASSED, FAILED
    public DateTimeOffset VerifiedAt { get; set; }
}

public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid ProposalId { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = default!;
    public string Status { get; set; } = "INITIATED"; // INITIATED, SUCCESS, FAILED, TIMEOUT
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PaymentAttempt> Attempts { get; set; } = new List<PaymentAttempt>();
}

public class PaymentAttempt
{
    public Guid AttemptId { get; set; }
    public Guid PaymentId { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = default!; // SUCCESS, FAILED, TIMEOUT
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Policy
{
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = default!;
    public Guid ProposalId { get; set; }
    public Guid CustomerId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public string CoverageData { get; set; } = "{}";
    public decimal SumInsured { get; set; }
    public decimal TotalPremium { get; set; }
    public decimal GstAmount { get; set; }
    public DateOnly RiskStartDate { get; set; }
    public DateOnly RiskEndDate { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, CANCELLED
    public DateTimeOffset IssuedAt { get; set; }
}

public class PolicyDocument
{
    public Guid DocId { get; set; }
    public Guid PolicyId { get; set; }
    public string FilePath { get; set; } = default!;
    public int Version { get; set; } = 1;
    public DateTimeOffset GeneratedAt { get; set; }
}

public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid? CustomerId { get; set; }
    public string TemplateCode { get; set; } = default!;
    public string Channel { get; set; } = "EMAIL";
    public string Recipient { get; set; } = default!;
    public string Payload { get; set; } = "{}";
    public string Status { get; set; } = "SENT"; // SENT, FAILED
    public DateTimeOffset SentAt { get; set; }
}

public class AuditLog
{
    public Guid AuditId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string? EntityId { get; set; }
    public string Details { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
