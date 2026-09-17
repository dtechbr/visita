using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace VisitasESUS;

public static class PdfImporter
{
    public static VisitRecord ParsePdf(string path)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(path);

        foreach (var page in pdf.GetPages())
        {
            var readable = ExtractReadablePageText(page);
            if (!string.IsNullOrWhiteSpace(readable))
                sb.AppendLine(readable);
            else
                sb.AppendLine(page.Text ?? string.Empty);
        }

        return ParseText(sb.ToString(), Path.GetFileName(path));
    }

    public static VisitRecord ParseText(string rawText, string sourceFile = "")
    {
        var text = Normalize(rawText);
        var start = MatchDate(text, @"Data\s*Inicial\s*:\s*(\d{2}/\d{2}/\d{4})", "Data Inicial");
        var end = MatchDate(text, @"Data\s*Final\s*:\s*(\d{2}/\d{2}/\d{4})", "Data Final");

        var team = MatchPair(text,
            @"Equipe\s*/\s*[ÁA]rea\s*:\s*([0-9]+)\s*-\s*(.+?)(?=\s*Micro\s*[ÁA]rea\s*:|\s*Unidade\s+de\s+Sa[uú]de\s*:|\s*USF\s+|\s*Desfecho\s+de\s+Visitas|$)",
            "Equipe/Área");

        var micro = MatchPair(text,
            @"Micro\s*[ÁA]rea\s*:\s*([0-9]+)\s*-\s*(.+?)(?=\s*USF\s+|\s*Desfecho\s+de\s+Visitas|\s*Quantidade|\s*Visitas\s+em\s+Domic[ií]lio|$)",
            "Micro Área");

        var unit = MatchOptional(text,
            @"Unidade\s+de\s+Sa[uú]de\s*:\s*(.+?)(?=\s*Equipe\s*/\s*[ÁA]rea\s*:|$)");

        // Alguns PDFs do Jasper/e-SUS não gravam espaço entre o rótulo e o valor
        // na ordem interna do PDF. Por isso aceitamos qualquer quantidade de espaço
        // e ainda temos um fallback que procura no texto totalmente compactado.
        var realizadas = MatchMetric(text, "VISITAS REALIZADAS", "VISITAS REALIZADAS");
        var ausentes = MatchMetric(text, "AUSENTES", "AUSENTES");

        var total = MatchTotal(text);
        if (total < 0)
            total = realizadas + ausentes;

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

    private static string ExtractReadablePageText(Page page)
    {
        try
        {
            var words = NearestNeighbourWordExtractor.Instance
                .GetWords(page.Letters)
                .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                .ToList();

            if (words.Count == 0)
                return page.Text ?? string.Empty;

            const double lineTolerance = 3.0;
            var lines = new List<List<Word>>();

            foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom)
                                      .ThenBy(w => w.BoundingBox.Left))
            {
                List<Word>? target = null;
                double bestDistance = double.MaxValue;

                foreach (var line in lines)
                {
                    var distance = Math.Abs(word.BoundingBox.Bottom - line[0].BoundingBox.Bottom);
                    if (distance <= lineTolerance && distance < bestDistance)
                    {
                        target = line;
                        bestDistance = distance;
                    }
                }

                if (target is null)
                    lines.Add(new List<Word> { word });
                else
                    target.Add(word);
            }

            var sb = new StringBuilder();
            foreach (var line in lines.OrderByDescending(l => l.Average(w => w.BoundingBox.Bottom)))
            {
                sb.AppendLine(string.Join(" ", line.OrderBy(w => w.BoundingBox.Left)
                                                   .Select(w => w.Text.Trim())));
            }
            return sb.ToString();
        }
        catch
        {
            return page.Text ?? string.Empty;
        }
    }

    private static int MatchMetric(string text, string label, string field)
    {
        var labelPattern = string.Join(@"\s*", label.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(Regex.Escape));
        var m = Regex.Match(text,
            labelPattern + @"\s*[:.\-]*\s*(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (m.Success && int.TryParse(m.Groups[1].Value, out var value))
            return value;

        // Fallback para PDFs que devolvem algo como VISITASREALIZADAS138AUSENTES290.
        var compact = Regex.Replace(text, @"\s+", string.Empty);
        var compactLabel = Regex.Replace(label, @"\s+", string.Empty);
        m = Regex.Match(compact,
            Regex.Escape(compactLabel) + @"[:.\-]*(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (m.Success && int.TryParse(m.Groups[1].Value, out value))
            return value;

        throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
    }

    private static int MatchTotal(string text)
    {
        var patterns = new[]
        {
            @"Total\s*Geral\s*\.{0,3}\s*:?\s*(\d+)",
            @"Total\s+de\s+visitas\s+de\s+Visitas\s+em\s+Domic[ií]lio\s*\(ACS\)\s*\.{0,3}\s*:?\s*(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var value = MatchIntOptional(text, pattern);
            if (value >= 0) return value;
        }

        var compact = Regex.Replace(text, @"\s+", string.Empty);
        var compactPatterns = new[]
        {
            @"TotalGeral\.{0,3}:?(\d+)",
            @"TotaldevisitasdeVisitasemDomic[ií]lio\(ACS\)\.{0,3}:?(\d+)"
        };

        foreach (var pattern in compactPatterns)
        {
            var value = MatchIntOptional(compact, pattern);
            if (value >= 0) return value;
        }
        return -1;
    }

    private static string Normalize(string text)
    {
        text = (text ?? string.Empty)
            .Replace('\0', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ');
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static DateTime MatchDate(string text, string pattern, string field)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success || !DateTime.TryParseExact(m.Groups[1].Value, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
        return d;
    }

    private static (string code, string name) MatchPair(string text, string pattern, string field)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            throw new InvalidDataException($"Campo '{field}' não encontrado no PDF.");
        return (m.Groups[1].Value, m.Groups[2].Value);
    }

    private static int MatchIntOptional(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return m.Success && int.TryParse(m.Groups[1].Value, out var value) ? value : -1;
    }

    private static string MatchOptional(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private static string Cleanup(string value) =>
        Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim(' ', '-', ':', '.');
}
