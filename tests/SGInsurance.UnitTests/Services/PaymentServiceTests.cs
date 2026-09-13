using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Services;
using SGInsurance.Domain.Entities;
using SGInsurance.UnitTests.TestSupport;
using Xunit;

namespace SGInsurance.UnitTests.Services;

public class PaymentServiceTests
{
    private static (PaymentService service, Mock<IProposalRepository> proposals, List<PaymentAttempt> attempts) BuildService(Guid proposalId, Guid paymentId, string proposalStatus)
    {
        var proposal = new Proposal { ProposalId = proposalId, Status = proposalStatus, CustomerId = Guid.NewGuid() };

        var proposals = new Mock<IProposalRepository>();
        proposals.Setup(r => r.GetByIdAsync(proposalId)).ReturnsAsync(proposal);
        proposals.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var attemptsStore = new List<PaymentAttempt>();
        var attempts = new Mock<IPaymentAttemptRepository>();
        attempts.Setup(r => r.Query()).Returns(() => attemptsStore.AsTestAsyncQueryable());
        attempts.Setup(r => r.AddAsync(It.IsAny<PaymentAttempt>())).Callback<PaymentAttempt>(a => attemptsStore.Add(a)).Returns(Task.CompletedTask);
        attempts.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var payments = new Mock<IPaymentRepository>();
        payments.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        payments.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(r => r.GetByIdAsync(It.IsAny<object[]>())).ReturnsAsync((Customer?)null);

        var policyService = new Mock<IPolicyService>();
        policyService.Setup(p => p.IssueAsync(proposalId)).ReturnsAsync(new Policy { PolicyId = Guid.NewGuid(), PolicyNumber = "SG-MOTOR-2026-000001" });

        var notifications = new Mock<INotificationService>();
        notifications.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string?>>()))
            .ReturnsAsync("rendered");

        var service = new PaymentService(payments.Object, attempts.Object, proposals.Object, customers.Object, policyService.Object, notifications.Object, NullLogger<PaymentService>.Instance);
        return (service, proposals, attemptsStore);
    }

    [Fact]
    public async Task Third_failed_attempt_marks_proposal_payment_failed()
    {
        var proposalId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var (service, proposals, attemptsStore) = BuildService(proposalId, paymentId, ProposalStatus.PaymentPending);

        var payment = new Payment { PaymentId = paymentId, ProposalId = proposalId, Amount = 1000, Mode = "UPI", Status = "INITIATED" };

        // Seed two prior failed attempts directly (attempts 1 and 2), then drive attempt 3
        // through the service's private ProcessAttemptAsync (invoked via reflection below)
        // which is the single method both CreateAsync and RetryAsync funnel through.
        // Attempt 1 - FAILED
        attemptsStore.Add(new PaymentAttempt { AttemptId = Guid.NewGuid(), PaymentId = paymentId, AttemptNumber = 1, Status = "FAILED" });
        // Attempt 2 - FAILED
        attemptsStore.Add(new PaymentAttempt { AttemptId = Guid.NewGuid(), PaymentId = paymentId, AttemptNumber = 2, Status = "FAILED" });

        // Attempt 3 (the one under test) should push the proposal to PAYMENT_FAILED.
        var proposal = await proposals.Object.GetByIdAsync(proposalId);
        Assert.NotNull(proposal);

        var result = await InvokeProcessAttempt(service, payment, proposal!, "FAILED");

        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(ProposalStatus.PaymentFailed, proposal!.Status);
    }

    [Fact]
    public async Task Success_on_first_attempt_completes_proposal_and_issues_policy()
    {
        var proposalId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var (service, proposals, _) = BuildService(proposalId, paymentId, ProposalStatus.PaymentPending);

        var payment = new Payment { PaymentId = paymentId, ProposalId = proposalId, Amount = 1000, Mode = "UPI", Status = "INITIATED" };
        var proposal = await proposals.Object.GetByIdAsync(proposalId);

        var result = await InvokeProcessAttempt(service, payment, proposal!, "SUCCESS");

        Assert.Equal("SUCCESS", result.Status);
        Assert.Equal(ProposalStatus.Completed, proposal!.Status);
        Assert.NotNull(result.PolicyNumber);
    }

    private static async Task<PaymentResponse> InvokeProcessAttempt(PaymentService service, Payment payment, Proposal proposal, string? simulateResult)
    {
        var method = typeof(PaymentService).GetMethod("ProcessAttemptAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var task = (Task<PaymentResponse>)method.Invoke(service, new object?[] { payment, proposal, simulateResult })!;
        return await task;
    }
}
