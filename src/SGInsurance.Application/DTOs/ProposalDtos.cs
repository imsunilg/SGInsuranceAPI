using System.Text.Json;

namespace SGInsurance.Application.DTOs;

public class CreateProposalRequest
{
    public Guid QuoteId { get; set; }
    public JsonElement ProposalData { get; set; }
    public JsonElement NomineeData { get; set; }
}

public class UpdateProposalRequest
{
    public JsonElement ProposalData { get; set; }
    public JsonElement NomineeData { get; set; }
}

public class ProposalResponse
{
    public Guid ProposalId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class KycVerifyRequest
{
    public string Pan { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateOnly? Dob { get; set; }
    public string? Address { get; set; }
}

public class KycResultResponse
{
    public Guid KycId { get; set; }
    public string Result { get; set; } = default!;
    public string ProposalStatus { get; set; } = default!;
}

public class RiskVerificationRequest
{
    public string VerificationType { get; set; } = default!;
    public JsonElement VerificationData { get; set; }
}

public class RiskVerificationResponse
{
    public Guid VerificationId { get; set; }
    public string Status { get; set; } = default!;
    public string ProposalStatus { get; set; } = default!;
}
