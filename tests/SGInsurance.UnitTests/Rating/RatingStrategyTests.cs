using System.Text.Json;
using SGInsurance.Application.Rating;
using Xunit;

namespace SGInsurance.UnitTests.Rating;

public class RatingStrategyTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    private static RatingContext BuildContext(string lob, decimal sumInsured, string productDataJson, string ruleConfigJson, List<SelectedAddon>? addons = null) => new()
    {
        LobCode = lob,
        ProductCode = "TEST",
        SumInsured = sumInsured,
        ProductData = Parse(productDataJson),
        RuleConfig = Parse(ruleConfigJson),
        SelectedAddons = addons ?? new List<SelectedAddon>()
    };

    [Fact]
    public async Task Motor_applies_ncb_discount_and_age_loading()
    {
        var strategy = new MotorRatingStrategy();
        var year = DateTime.UtcNow.Year - 2; // 2 years old vehicle
        var ctx = BuildContext("MOTOR", 500000m,
            $$"""{"year": {{year}}, "ncbPercent": 2}""",
            """{"baseRatePercentOfSumInsured":3.0,"gstPercent":18,"ncbSlabPercent":{"0":0,"1":20,"2":25,"3":35,"4":45,"5":50},"ageLoadingPercentPerYear":1.5,"minPremium":1500}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 500000 * 3% = 15000; ageLoading = 15000 * (1.5*2)/100 = 450; risk = 15450
        // discount = 15450 * 25% = 3862.5
        Assert.Equal(15450m, result.BasePremium);
        Assert.Equal(3862.5m, result.Discount);
        Assert.True(result.TotalPremium > 0);
    }

    [Fact]
    public async Task Health_applies_age_band_and_preexisting_loading()
    {
        var strategy = new HealthRatingStrategy();
        var dob = DateTime.UtcNow.AddYears(-50).ToString("yyyy-MM-dd");
        var ctx = BuildContext("HEALTH", 500000m,
            $$"""{"members":[{"name":"A","dob":"{{dob}}","relationship":"SELF"}],"sumInsured":500000,"preExistingConditions":true,"cityTier":"TIER1"}""",
            """{"baseRatePercentOfSumInsured":2.0,"gstPercent":18,"ageBands":[{"maxAge":30,"loadPercent":0},{"maxAge":45,"loadPercent":10},{"maxAge":60,"loadPercent":25},{"maxAge":200,"loadPercent":50}],"preExistingLoadPercent":15,"minPremium":2000}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 500000*2% = 10000; age 50 -> band <=60 -> 25% load; +15% preexisting = 40% total
        // risk = 10000 * 1.40 = 14000
        Assert.Equal(14000m, result.BasePremium);
    }

    [Fact]
    public async Task Travel_applies_international_multiplier_and_traveller_flat()
    {
        var strategy = new TravelRatingStrategy();
        var start = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var end = DateTime.UtcNow.AddDays(9).ToString("yyyy-MM-dd");
        var ctx = BuildContext("TRAVEL", 0m,
            $$"""{"destinations":["USA"],"tripStartDate":"{{start}}","tripEndDate":"{{end}}","numberOfTravellers":2,"purpose":"LEISURE"}""",
            """{"baseRatePerDay":50,"gstPercent":18,"internationalMultiplier":2.5,"perTravellerFlat":100,"minPremium":500}""");

        var result = await strategy.CalculateAsync(ctx);

        // days=9, travellers=2 => base=50*9*2=900; international *2.5 = 2250; + flat 100*2=200 => 2450
        Assert.Equal(2450m, result.BasePremium);
    }

    [Fact]
    public async Task Home_applies_property_age_loading()
    {
        var strategy = new HomeRatingStrategy();
        var ctx = BuildContext("HOME", 2000000m,
            """{"propertyAddress":"x","constructionType":"RCC","builtUpAreaSqFt":1200,"propertyAge":10,"sumInsuredStructure":1500000,"sumInsuredContents":500000}""",
            """{"baseRatePercentOfSumInsured":0.15,"gstPercent":18,"propertyAgeLoadingPercentPerYear":0.5,"minPremium":1000}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 2000000*0.15% = 3000; ageLoading = 3000*(0.5*10)/100 = 150; total risk = 3150
        Assert.Equal(3150m, result.BasePremium);
    }

    [Fact]
    public async Task PersonalAccident_applies_occupation_loading_and_family_multiplier()
    {
        var strategy = new PersonalAccidentRatingStrategy();
        var ctx = BuildContext("PERSONAL_ACCIDENT", 1000000m,
            """{"occupationClass":"HAZARDOUS","sumInsured":1000000,"familyMembers":[{"name":"Spouse"}]}""",
            """{"baseRatePercentOfSumInsured":0.5,"gstPercent":18,"occupationLoadPercent":{"CLERICAL":0,"FIELD":15,"HAZARDOUS":40},"minPremium":500}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 1000000*0.5% = 5000; *1.40 (hazardous) = 7000; * (1+1 family member)=2 => 14000
        Assert.Equal(14000m, result.BasePremium);
    }

    [Fact]
    public async Task Commercial_applies_employee_count_loading()
    {
        var strategy = new CommercialRatingStrategy();
        var ctx = BuildContext("COMMERCIAL", 10000000m,
            """{"businessName":"Acme","industryType":"MANUFACTURING","propertyValue":10000000,"numberOfEmployees":200}""",
            """{"baseRatePercentOfSumInsured":0.8,"gstPercent":18,"employeeCountLoadingPer100":5,"minPremium":5000}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 10000000*0.8% = 80000; loading = 80000 * (5*2)/100 = 8000; risk = 88000
        Assert.Equal(88000m, result.BasePremium);
    }

    [Fact]
    public async Task Agriculture_applies_season_loading()
    {
        var strategy = new AgricultureRatingStrategy();
        var ctx = BuildContext("AGRICULTURE", 200000m,
            """{"cropOrLivestockType":"WHEAT","areaAcresOrHeadCount":20,"season":"RABI","location":"Punjab","sumInsured":200000}""",
            """{"baseRatePercentOfSumInsured":5.0,"gstPercent":18,"seasonLoadPercent":{"KHARIF":0,"RABI":5,"ZAID":10},"minPremium":300}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 200000*5% = 10000; *1.05 (RABI) = 10500
        Assert.Equal(10500m, result.BasePremium);
    }

    [Fact]
    public async Task Cyber_applies_volume_loading()
    {
        var strategy = new CyberRatingStrategy();
        var ctx = BuildContext("CYBER", 500000m,
            """{"individualOrBusiness":"INDIVIDUAL","annualOnlineTransactionVolume":100000}""",
            """{"baseRatePercentOfSumInsured":1.0,"gstPercent":18,"volumeLoadPercent":10,"minPremium":800}""");

        var result = await strategy.CalculateAsync(ctx);

        // base = 500000*1% = 5000; *1.10 = 5500
        Assert.Equal(5500m, result.BasePremium);
    }
}
