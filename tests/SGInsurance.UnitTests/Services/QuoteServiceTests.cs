using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Application.Rating;
using SGInsurance.Application.Services;
using SGInsurance.Domain.Entities;
using SGInsurance.UnitTests.TestSupport;
using Xunit;

namespace SGInsurance.UnitTests.Services;

public class QuoteServiceTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task CreateAsync_persists_quote_with_premium_from_calculation_service()
    {
        var quotesStore = new List<Quote>();
        var quotes = new Mock<IQuoteRepository>();
        quotes.Setup(r => r.AddAsync(It.IsAny<Quote>())).Callback<Quote>(q => quotesStore.Add(q)).Returns(Task.CompletedTask);
        quotes.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        quotes.Setup(r => r.Query()).Returns(() => quotesStore.AsTestAsyncQueryable());

        var product = new ProductMaster { ProductCode = "PRIVATE_CAR", LobCode = "MOTOR", ProductName = "Private Car Insurance", RatingStrategyKey = "MOTOR" };
        var products = new Mock<IProductRepository>();
        products.Setup(r => r.GetByIdAsync(It.IsAny<object[]>())).ReturnsAsync(product);

        var addons = new Mock<IProductAddonRepository>();
        addons.Setup(r => r.Query()).Returns(new List<ProductAddon>().AsTestAsyncQueryable());

        var premiumResult = new PremiumResult { SumInsured = 500000, BasePremium = 15000, AddonPremium = 0, Discount = 0, GstAmount = 2700, TotalPremium = 17700 };
        var premium = new Mock<IPremiumCalculationService>();
        premium.Setup(p => p.CalculateAsync("PRIVATE_CAR", "MOTOR", It.IsAny<decimal>(), It.IsAny<JsonElement>(), It.IsAny<List<SelectedAddon>>()))
            .ReturnsAsync(premiumResult);

        var notifications = new Mock<INotificationService>();
        notifications.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string?>>())).ReturnsAsync("x");

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(r => r.GetByIdAsync(It.IsAny<object[]>())).ReturnsAsync((Customer?)null);

        var service = new QuoteService(quotes.Object, products.Object, addons.Object, premium.Object, notifications.Object, customers.Object, NullLogger<QuoteService>.Instance);

        var request = new CreateQuoteRequest
        {
            CustomerId = Guid.NewGuid(),
            ProductCode = "PRIVATE_CAR",
            LobCode = "MOTOR",
            ProductData = Parse("""{"sumInsured":500000,"year":2022}"""),
            AddOns = new List<string>()
        };

        var response = await service.CreateAsync(request);

        Assert.Equal(17700, response.TotalPremium);
        Assert.Single(quotesStore);
        Assert.Equal(Domain.Entities.QuoteStatus.Created, quotesStore[0].Status);
    }
}
