using System.Text;
using System.Text.Json;

namespace VisitasESUS;

public sealed class DataStore
{
    private readonly string _dbPath;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public LocalDatabase Db { get; private set; } = new();
    public string DatabasePath => _dbPath;
    public int InvalidRecordsRemoved { get; private set; }
    public string? RepairBackupPath { get; private set; }

    public static int LastInvalidRecordsRemoved { get; private set; }
    public static string? LastRepairBackupPath { get; private set; }

    public DataStore()
    {
        _dbPath = Path.Combine(AppContext.BaseDirectory, "visitas_base.json");
        Load();
    }

    public void Load()
    {
        InvalidRecordsRemoved = 0;
        RepairBackupPath = null;
        LastInvalidRecordsRemoved = 0;
        LastRepairBackupPath = null;

        if (!File.Exists(_dbPath))
        {
            Db = new LocalDatabase();
            Save();
            return;
        }

        try
        {
            var text = File.ReadAllText(_dbPath, Encoding.UTF8);
            Db = JsonSerializer.Deserialize<LocalDatabase>(text, _json) ?? new LocalDatabase();
            Db.Records ??= new List<VisitRecord>();
            Db.ProfessionalLinks ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Db.TerritoryConfigs ??= new Dictionary<string, TerritoryConfig>(StringComparer.OrdinalIgnoreCase);
            MigrateLegacyProfessionalLinks();
            ApplyTerritoryConfiguration();
            RemoveImpossibleRecords();
        }
        catch
        {
            var backup = _dbPath + ".corrompido_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            try { File.Copy(_dbPath, backup, true); } catch { }
            Db = new LocalDatabase();
            Save();
        }
    }

    private void MigrateLegacyProfessionalLinks()
    {
        foreach (var item in Db.ProfessionalLinks)
        {
            if (Db.TerritoryConfigs.ContainsKey(item.Key)) continue;
            var parts = item.Key.Split('|');
            if (parts.Length != 2) continue;
            var sample = Db.Records.FirstOrDefault(r =>
                r.TeamCode.Equals(parts[0], StringComparison.OrdinalIgnoreCase) &&
                r.MicroCode.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
            Db.TerritoryConfigs[item.Key] = new TerritoryConfig
            {
                TeamCode = parts[0],
                TeamName = sample?.TeamName ?? "",
                MicroCode = parts[1],
                ProfessionalName = item.Value,
                RegisteredPeople = 0,
                UpdatedAt = DateTime.Now
            };
        }
    }

    private void RemoveImpossibleRecords()
    {
        var invalid = Db.Records
            .Where(r => r.Total < 0 || r.Realizadas < 0 || r.Ausentes < 0 ||
                        r.Realizadas > r.Total || r.Ausentes > r.Total ||
                        (long)r.Realizadas + r.Ausentes > r.Total)
            .ToList();

        if (invalid.Count == 0) return;

        try
        {
            var backupName = $"visitas_base_antes_correcao_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            RepairBackupPath = Path.Combine(AppContext.BaseDirectory, backupName);
            File.Copy(_dbPath, RepairBackupPath, true);
        }
        catch
        {
            RepairBackupPath = null;
        }

        var invalidKeys = invalid.Select(r => r.IdentityKey)
                                 .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Db.Records.RemoveAll(r => invalidKeys.Contains(r.IdentityKey));
        InvalidRecordsRemoved = invalid.Count;
        LastInvalidRecordsRemoved = InvalidRecordsRemoved;
        LastRepairBackupPath = RepairBackupPath;
        Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(AppContext.BaseDirectory);
        var tmp = _dbPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(Db, _json), new UTF8Encoding(false));
        File.Move(tmp, _dbPath, true);
    }

    public bool Upsert(VisitRecord record)
    {
        ApplyTerritoryToRecord(record);
        var idx = Db.Records.FindIndex(r => r.IdentityKey.Equals(record.IdentityKey, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) { Db.Records[idx] = record; Save(); return false; }
        Db.Records.Add(record); Save(); return true;
    }

    public TerritoryConfig? GetTerritoryConfig(string teamCode, string microCode)
    {
        Db.TerritoryConfigs.TryGetValue(LinkKey(teamCode, microCode), out var cfg);
        return cfg;
    }

    public void SaveTerritoryConfig(string teamCode, string teamName, string microCode, string professionalName, int registeredPeople)
    {
        var key = LinkKey(teamCode, microCode);
        Db.TerritoryConfigs[key] = new TerritoryConfig
        {
            TeamCode = teamCode.Trim(),
            TeamName = teamName.Trim(),
            MicroCode = microCode.Trim().PadLeft(2, '0'),
            ProfessionalName = professionalName.Trim(),
            RegisteredPeople = Math.Max(0, registeredPeople),
            UpdatedAt = DateTime.Now
        };

        if (string.IsNullOrWhiteSpace(professionalName)) Db.ProfessionalLinks.Remove(key);
        else Db.ProfessionalLinks[key] = professionalName.Trim();
        ApplyTerritoryConfiguration();
        Save();
    }

    public void LinkProfessional(string teamCode, string microCode, string professional)
    {
        var sample = Db.Records.FirstOrDefault(r => r.TeamCode == teamCode && r.MicroCode == microCode);
        var old = GetTerritoryConfig(teamCode, microCode);
        SaveTerritoryConfig(teamCode, sample?.TeamName ?? old?.TeamName ?? "", microCode,
            professional, old?.RegisteredPeople ?? 0);
    }

    public void Delete(IEnumerable<VisitRecord> records)
    {
        var keys = records.Select(r => r.IdentityKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Db.Records.RemoveAll(r => keys.Contains(r.IdentityKey));
        Save();
    }

    public string Backup()
    {
        Save();
        var name = $"visitas_base_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(AppContext.BaseDirectory, name);
        File.Copy(_dbPath, path, true);
        return path;
    }

    public void Restore(string path)
    {
        File.Copy(path, _dbPath, true);
        Load();
    }

    public static string LinkKey(string teamCode, string microCode) => $"{teamCode.Trim()}|{microCode.Trim().PadLeft(2, '0')}";

    private void ApplyTerritoryConfiguration()
    {
        foreach (var r in Db.Records) ApplyTerritoryToRecord(r);
    }

    private void ApplyTerritoryToRecord(VisitRecord r)
    {
        var key = LinkKey(r.TeamCode, r.MicroCode);
        if (Db.TerritoryConfigs.TryGetValue(key, out var cfg) && !string.IsNullOrWhiteSpace(cfg.ProfessionalName))
            r.Professional = cfg.ProfessionalName;
        else if (Db.ProfessionalLinks.TryGetValue(key, out var legacy))
            r.Professional = legacy;
    }
}
