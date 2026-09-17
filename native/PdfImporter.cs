using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace VisitasESUS;

public static class PdfImporter
{
    public static VisitRecord ParsePdf(string path)
    {
        var sb = new StringBuilder();
        using (var pdf = PdfDocument.Open(path))
        {
            foreach (var page in pdf.GetPages())
            {
                sb.Append(' ').Append(page.Text).Append(' ');
            }
        }
        return ParseText(sb.ToString(), Path.GetFileName(path));
    }

    public static VisitRecord ParseText(string rawText, string sourceFile = "")
    {
        var text = Normalize(rawText);
        var start = MatchDate(text, @"Data\s*Inicial\s*:\s*(\d{2}/\d{2}/\d{4})", "Data Inicial");
        var end = MatchDate(text, @"Data\s*Final\s*:\s*(\d{2}/\d{2}/\d{4})", "Data Final");
        var team = MatchPair(text,
            @"Equipe\s*/\s*[ÁA]rea\s*:\s*([0-9]+)\s*-\s*(.+?)(?=\s+Micro\s*[ÁA]rea\s*:|\s+Unidade\s+de\s+Sa[uú]de\s*:|\s+USF\s+|\s+Desfecho\s+de\s+Visitas|$)",
            "Equipe/Área");
        var micro = MatchPair(text,
            @"Micro\s*[ÁA]rea\s*:\s*([0-9]+)\s*-\s*(.+?)(?=\s+USF\s+|\s+Desfecho\s+de\s+Visitas|\s+Visitas\s+em\s+Domic[ií]lio|$)",
            "Micro Área");

        var unit = MatchOptional(text, @"Unidade\s+de\s+Sa[uú]de\s*:\s*(.+?)(?=\s+Equipe\s*/\s*[ÁA]rea\s*:|$)");
        var realizadas = MatchInt(text, @"VISITAS\s+REALIZADAS\s+(\d+)", "VISITAS REALIZADAS");
        var ausentes = MatchInt(text, @"AUSENTES\s+(\d+)", "AUSENTES");
        var total = MatchIntOptional(text, @"Total\s+Geral\s*\.{0,3}\s*:\s*(\d+)");
        if (total < 0) total = MatchIntOptional(text, @"Total\s+de\s+visitas\s+de\s+Visitas\s+em\s+Domic[ií]lio\s*\(ACS\)\s*\.{0,3}\s*:\s*(\d+)");
        if (total < 0) total = realizadas + ausentes;

        return new VisitRecord
        {
            MonthKey = start.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            StartDate = start,
            EndDate = end,
            Unit = Cleanup(unit),
            TeamCode = Cleanup(team.code),
            TeamName = Cleanup(team.name),
            MicroCode = Cleanup(micro.code).PadLeft(2, '0'),
            MicroName = Cleanup(micro.name),
            Realizadas = realizadas,
            Ausentes = ausentes,
            Total = total,
            SourceFile = sourceFile,
            ImportedAt = DateTime.Now
        };
    }

    private static string Normalize(string text)
    {
        text = text.Replace('\0', ' ').Replace('\r', ' ').Replace('\n', ' ');
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static DateTime MatchDate(string text, string pattern, string field)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success || !DateTime.TryParseExact(m.Groups[1].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
        return d;
    }

    private static (string code, string name) MatchPair(string text, string pattern, string field)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success) throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
        return (m.Groups[1].Value, m.Groups[2].Value);
    }

    private static int MatchInt(string text, string pattern, string field)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var value))
            throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
        return value;
    }

    private static int MatchIntOptional(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return m.Success && int.TryParse(m.Groups[1].Value, out var value) ? value : -1;
    }

    private static string MatchOptional(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return m.Success ? m.Groups[1].Value : "";
    }

    private static string Cleanup(string value) => Regex.Replace(value ?? "", @"\s+", " ").Trim(' ', '-', ':', '.');
}
