using System.Globalization;
using System.Text;

namespace HelpMuslimWebApp.Domain.Models
{
    // Recherche insensible à la casse ET aux accents : "developpeur" trouve "Développeur".
    // Côté base : EF.Functions.ILike(EF.Functions.Unaccent(colonne), SearchText.Pattern(terme))
    public static class SearchText
    {
        // "%terme sans accents%", avec % _ \ échappés pour être recherchés tels quels
        public static string Pattern(string term)
        {
            var escaped = RemoveAccents(term.Trim())
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            return $"%{escaped}%";
        }

        public static string RemoveAccents(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
