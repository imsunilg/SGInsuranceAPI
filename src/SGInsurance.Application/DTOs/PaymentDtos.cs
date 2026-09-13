namespace SGInsurance.Application.DTOs;

public class CreatePaymentRequest
{
    public Guid ProposalId { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = default!; // UPI/CREDIT_CARD/DEBIT_CARD/NET_BANKING
    public string? SimulateResult { get; set; } // SUCCESS/FAILED/TIMEOUT - client "Simulate" buttons
}

public class RetryPaymentRequest
{
    public string? SimulateResult { get; set; }
}

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public Guid ProposalId { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int AttemptCount { get; set; }
    public Guid? PolicyId { get; set; }
    public string? PolicyNumber { get; set; }
}
