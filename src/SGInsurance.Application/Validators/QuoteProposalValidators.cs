using System.Text.Json;
using FluentValidation;
using SGInsurance.Application.DTOs;

namespace SGInsurance.Application.Validators;

/// <summary>
/// Required top-level keys per LOB productData shape, checked generically so one
/// validator covers all 8 LOBs instead of one class per LOB.
/// </summary>
public static class LobProductDataRules
{
    public static readonly Dictionary<string, string[]> RequiredKeys = new()
    {
        ["MOTOR"] = new[] { "regNumber", "make", "model", "year", "fuelType" },
        ["HEALTH"] = new[] { "members", "sumInsured" },
        ["TRAVEL"] = new[] { "destinations", "tripStartDate", "tripEndDate", "numberOfTravellers" },
        ["HOME"] = new[] { "propertyAddress", "constructionType", "sumInsuredStructure" },
        ["PERSONAL_ACCIDENT"] = new[] { "occupationClass", "sumInsured" },
        ["COMMERCIAL"] = new[] { "businessName", "industryType" },
        ["AGRICULTURE"] = new[] { "cropOrLivestockType", "areaAcresOrHeadCount", "season" },
        ["CYBER"] = new[] { "individualOrBusiness" }
    };

    public static List<string> Validate(string lobCode, JsonElement productData)
    {
        var errors = new List<string>();
        if (!RequiredKeys.TryGetValue(lobCode, out var keys)) return errors;
        if (productData.ValueKind != JsonValueKind.Object)
        {
            errors.Add("productData must be a JSON object.");
            return errors;
        }
        foreach (var key in keys)
        {
            if (!productData.TryGetProperty(key, out _))
                errors.Add($"productData.{key} is required for LOB '{lobCode}'.");
        }

        if (lobCode == "MOTOR")
        {
            if (productData.TryGetProperty("regNumber", out var reg) && reg.ValueKind == JsonValueKind.String &&
                !System.Text.RegularExpressions.Regex.IsMatch(reg.GetString() ?? "", "^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$"))
                errors.Add("productData.regNumber must look like a valid vehicle registration number (e.g. KA01AB1234).");

            if (productData.TryGetProperty("year", out var yearEl) && yearEl.ValueKind == JsonValueKind.Number)
            {
                var year = yearEl.GetInt32();
                var maxYear = DateTime.UtcNow.Year + 1;
                if (year < 1990 || year > maxYear)
                    errors.Add($"productData.year must be between 1990 and {maxYear}.");
            }
        }

        return errors;
    }
}

public class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ProductCode).NotEmpty();
        RuleFor(x => x.LobCode).NotEmpty();
        RuleFor(x => x).Custom((req, ctx) =>
        {
            foreach (var error in LobProductDataRules.Validate(req.LobCode, req.ProductData))
                ctx.AddFailure(nameof(req.ProductData), error);
        });
    }
}

public class KycVerifyRequestValidator : AbstractValidator<KycVerifyRequest>
{
    public KycVerifyRequestValidator()
    {
        RuleFor(x => x.Pan).NotEmpty().Matches("^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
            .WithMessage("PAN must match format AAAAA9999A.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    private static readonly string[] ValidModes = { "UPI", "CREDIT_CARD", "DEBIT_CARD", "NET_BANKING" };

    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.ProposalId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Mode).NotEmpty().Must(m => ValidModes.Contains(m))
            .WithMessage($"Mode must be one of: {string.Join(", ", ValidModes)}");
    }
}
