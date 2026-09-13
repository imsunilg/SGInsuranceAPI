using SGInsurance.Application.Services;
using SGInsurance.Domain.Entities;
using Xunit;

namespace SGInsurance.UnitTests.Services;

public class ProposalServiceTests
{
    [Theory]
    [InlineData(ProposalStatus.Draft, ProposalStatus.Submitted, true)]
    [InlineData(ProposalStatus.Submitted, ProposalStatus.KycPending, true)]
    [InlineData(ProposalStatus.KycPending, ProposalStatus.VerificationPending, true)]
    [InlineData(ProposalStatus.VerificationPending, ProposalStatus.PaymentPending, true)]
    [InlineData(ProposalStatus.PaymentPending, ProposalStatus.Completed, true)]
    [InlineData(ProposalStatus.PaymentPending, ProposalStatus.PaymentFailed, true)]
    [InlineData(ProposalStatus.PaymentFailed, ProposalStatus.PaymentPending, true)]
    public void Allows_valid_transitions(string from, string to, bool expectedAllowed)
    {
        if (expectedAllowed)
        {
            var ex = Record.Exception(() => ProposalService.EnsureTransitionAllowed(from, to));
            Assert.Null(ex);
        }
    }

    [Theory]
    [InlineData(ProposalStatus.Draft, ProposalStatus.Completed)]
    [InlineData(ProposalStatus.Draft, ProposalStatus.PaymentPending)]
    [InlineData(ProposalStatus.Submitted, ProposalStatus.Completed)]
    [InlineData(ProposalStatus.Completed, ProposalStatus.Draft)]
    [InlineData(ProposalStatus.KycPending, ProposalStatus.PaymentPending)]
    public void Rejects_invalid_transitions(string from, string to)
    {
        Assert.Throws<InvalidOperationException>(() => ProposalService.EnsureTransitionAllowed(from, to));
    }
}
