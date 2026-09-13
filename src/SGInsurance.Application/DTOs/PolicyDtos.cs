namespace SGInsurance.Application.DTOs;

public class PolicyResponse
{
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string ProductCode { get; set; } = default!;
    public string LobCode { get; set; } = default!;
    public decimal SumInsured { get; set; }
    public decimal TotalPremium { get; set; }
    public decimal GstAmount { get; set; }
    public DateOnly RiskStartDate { get; set; }
    public DateOnly RiskEndDate { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset IssuedAt { get; set; }
}

public class NotificationDto
{
    public Guid NotificationId { get; set; }
    public string TemplateCode { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string Recipient { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; }
}

public class AdminDashboardResponse
{
    public int Customers { get; set; }
    public int Products { get; set; }
    public int Quotes { get; set; }
    public int Proposals { get; set; }
    public int Payments { get; set; }
    public int Policies { get; set; }
    public decimal TotalPremium { get; set; }
}
