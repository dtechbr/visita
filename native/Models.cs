using System.Globalization;

namespace VisitasESUS;

public sealed class VisitRecord
{
    public string MonthKey { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Unit { get; set; } = "";
    public string TeamCode { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string MicroCode { get; set; } = "";
    public string MicroName { get; set; } = "";
    public string Professional { get; set; } = "";
    public int Realizadas { get; set; }
    public int Ausentes { get; set; }
    public int Total { get; set; }
    public string SourceFile { get; set; } = "";
    public DateTime ImportedAt { get; set; } = DateTime.Now;

    public string MonthLabel
    {
        get
        {
            if (!DateTime.TryParseExact(MonthKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return MonthKey;
            return CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(d.Month).ToUpperInvariant() + "/" + d.Year;
        }
    }

    public string TeamLabel => string.IsNullOrWhiteSpace(TeamName) ? TeamCode : TeamName;
    public string MicroLabel => string.IsNullOrWhiteSpace(MicroName) ? MicroCode : $"{MicroCode} - {MicroName}";
    public double SuccessRate => Total <= 0 ? 0 : (double)Realizadas / Total * 100.0;
    public string IdentityKey => $"{MonthKey}|{TeamCode}|{MicroCode}";
}

public sealed class TerritoryConfig
{
    public string TeamCode { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string MicroCode { get; set; } = "";
    public string ProfessionalName { get; set; } = "";
    public int RegisteredPeople { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string Key => DataStore.LinkKey(TeamCode, MicroCode);
    public string TeamLabel => string.IsNullOrWhiteSpace(TeamName) ? TeamCode : TeamName;
}

public sealed class LocalDatabase
{
    public List<VisitRecord> Records { get; set; } = new();
    // Mantido por compatibilidade com versões anteriores.
    public Dictionary<string, string> ProfessionalLinks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, TerritoryConfig> TerritoryConfigs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
