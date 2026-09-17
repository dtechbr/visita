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
            ApplyProfessionalLinks();
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
        var key = LinkKey(record.TeamCode, record.MicroCode);
        if (Db.ProfessionalLinks.TryGetValue(key, out var professional)) record.Professional = professional;
        var idx = Db.Records.FindIndex(r => r.IdentityKey.Equals(record.IdentityKey, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) { Db.Records[idx] = record; Save(); return false; }
        Db.Records.Add(record); Save(); return true;
    }

    public void LinkProfessional(string teamCode, string microCode, string professional)
    {
        var key = LinkKey(teamCode, microCode);
        if (string.IsNullOrWhiteSpace(professional)) Db.ProfessionalLinks.Remove(key);
        else Db.ProfessionalLinks[key] = professional.Trim();
        ApplyProfessionalLinks();
        Save();
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

    public static string LinkKey(string teamCode, string microCode) => $"{teamCode.Trim()}|{microCode.Trim()}";

    private void ApplyProfessionalLinks()
    {
        foreach (var r in Db.Records)
        {
            r.Professional = Db.ProfessionalLinks.TryGetValue(LinkKey(r.TeamCode, r.MicroCode), out var p) ? p : r.Professional;
        }
    }
}
