using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class IsbnAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string isbn || string.IsNullOrWhiteSpace(isbn))
            return true;

        var normalized = isbn.Replace("-", string.Empty).Replace(" ", string.Empty);
        return normalized.Length switch
        {
            10 => IsValidIsbn10(normalized),
            13 => IsValidIsbn13(normalized),
            _ => false
        };
    }

    public override string FormatErrorMessage(string name) =>
        $"{name} має бути коректним ISBN-10 або ISBN-13";

    private static bool IsValidIsbn10(string isbn)
    {
        if (!isbn.Take(9).All(char.IsDigit) || !(char.IsDigit(isbn[9]) || isbn[9] is 'X' or 'x'))
            return false;

        var checksum = isbn.Take(9).Select((digit, index) => (digit - '0') * (10 - index)).Sum();
        checksum += isbn[9] is 'X' or 'x' ? 10 : isbn[9] - '0';
        return checksum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsDigit))
            return false;

        var checksum = isbn.Take(12)
            .Select((digit, index) => (digit - '0') * (index % 2 == 0 ? 1 : 3))
            .Sum();
        var checkDigit = (10 - checksum % 10) % 10;
        return checkDigit == isbn[12] - '0';
    }
}