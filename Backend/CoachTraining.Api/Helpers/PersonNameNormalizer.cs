using System.Globalization;
using System.Text;

namespace CoachTraining.Api.Helpers;

/// <summary>Creates consistent display values and comparison keys for person names.</summary>
public static class PersonNameNormalizer
{
    public static string ToComparisonKey(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormKC);
        var key = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (!char.IsWhiteSpace(character) && category is not UnicodeCategory.Format and not UnicodeCategory.Control)
            {
                key.Append(char.ToUpperInvariant(character));
            }
        }

        return key.ToString();
    }

    public static string ToDisplayName(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormKC);
        var visibleCharacters = normalized.Where(character =>
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            return category is not UnicodeCategory.Format and not UnicodeCategory.Control;
        });

        return string.Join(' ', new string(visibleCharacters.ToArray())
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
