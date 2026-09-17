using System.Globalization;
using System.Text;

namespace VisitasESUS;

public sealed class MainFormV5 : Form
{
    private readonly DataStore _store = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusText = new();

    // Dashboard
    private readonly ComboBox _dashMonth = Combo();
    private readonly ComboBox _dashTeam = Combo();
    private readonly ComboBox _dashMicro = Combo();
    private readonly ComboBox _dashProfessional = Combo();
    private readonly Label _cardDone = CardValue();
    private readonly Label _cardAbsent = CardValue();
    private readonly Label _cardTotal = CardValue();
    private readonly Label _cardRate = CardValue();
    private readonly NativeChart _dashChart = new() { Dock = DockStyle.Fill, ChartTitle = "Visitas por ACS" };
    private readonly DataGridView _dashGrid = DashboardGrid();

    // Evolução
    private readonly ComboBox _evoDimension = Combo();
    private readonly ComboBox _evoEntity = Combo();
    private readonly ComboBox _evoMetric = Combo();
    private readonly NativeChart _evoChart = new() { Dock = DockStyle.Fill, ChartType = NativeChartType.Line, ChartTitle = "Evolução mensal" };

    // Comparação
    private readonly ComboBox _cmpTeam = Combo();
    private readonly ComboBox _cmpMonthA = Combo();
    private readonly ComboBox _cmpMonthB = Combo();
    private readonly ComboBox _cmpMetric = Combo();
    private readonly NativeChart _cmpChart = new() { Dock = DockStyle.Fill, ChartTitle = "Comparação entre ACS da equipe" };
    private readonly DataGridView _cmpGrid = ComparisonGrid();
    private readonly Label _cmpSummary = new() { Dock = DockStyle.Top, Height = 38, Padding = new Padding(12, 8, 8, 4), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

    // Configuração
    private readonly DataGridView _cfgGrid = ConfigurationGrid();
    private readonly Label _cfgInfo = new()
    {
        AutoSize = true,
        Padding = new Padding(12, 8, 0, 0),
        Text = "Cadastre o nome do ACS e o total de pessoas cadastradas em cada território. Equipe e microárea são identificadas pelos PDFs importados."
    };

    // Dados
    private readonly DataGridView _dataGrid = DashboardGrid();

    public MainFormV5()
    {
        Text = "Painel de Visitas e-SUS v5";
        Width = 1320;
        Height = 850;
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);
        AllowDrop = true;

        BuildUi();
        WireEvents();
        RefreshAll();
        _statusText.Text = $"v5 • Base local: {_store.DatabasePath}";
    }

    private void BuildUi()
    {
        Controls.Add(_tabs);
        Controls.Add(BuildToolbar());
        _status.Items.Add(_statusText);
        Controls.Add(_status);

        _tabs.TabPages.Add(BuildDashboardTab());
        _tabs.TabPages.Add(BuildEvolutionTab());
        _tabs.TabPages.Add(BuildComparisonTab());
        _tabs.TabPages.Add(BuildConfigurationTab());
        _tabs.TabPages.Add(BuildDataTab());
    }

    private Control BuildToolbar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(12, 9, 8, 7),
            BackColor = Color.White,
            WrapContents = false
        };
        bar.Controls.Add(ActionButton("Importar PDFs", (_, _) => ImportWithDialog(), true));
        bar.Controls.Add(ActionButton("Exportar CSV", (_, _) => ExportCsv()));
        bar.Controls.Add(ActionButton("Exportar PNG", (_, _) => ExportPng()));
        bar.Controls.Add(ActionButton("Backup da base", (_, _) => BackupDatabase()));
        bar.Controls.Add(ActionButton("Restaurar base", (_, _) => RestoreDatabase()));
        return bar;
    }

    private TabPage BuildDashboardTab()
    {
        var tab = new TabPage("Dashboard") { BackColor = BackColor };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(12, 8, 8, 4), BackColor = Color.White };
        filters.Controls.Add(FilterBlock("Mês", _dashMonth, 145));
        filters.Controls.Add(FilterBlock("Equipe", _dashTeam, 180));
        filters.Controls.Add(FilterBlock("ACS / Microárea", _dashMicro, 210));
        filters.Controls.Add(FilterBlock("Profissional", _dashProfessional, 210));

        var cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(12, 8, 4, 6), BackColor = BackColor };
        cards.Controls.Add(Card("VISITAS REALIZADAS", _cardDone));
        cards.Controls.Add(Card("AUSENTES", _cardAbsent));
        cards.Controls.Add(Card("TOTAL DO RELATÓRIO", _cardTotal));
        cards.Controls.Add(Card("% SOBRE CADASTRADOS", _cardRate));

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 390, BackColor = BackColor };
        split.Panel1.Padding = new Padding(12, 6, 12, 6);
        split.Panel1.Controls.Add(_dashChart);
        split.Panel2.Padding = new Padding(12, 4, 12, 10);
        split.Panel2.Controls.Add(_dashGrid);

        tab.Controls.Add(split);
        tab.Controls.Add(cards);
        tab.Controls.Add(filters);
        return tab;
    }

    private TabPage BuildEvolutionTab()
    {
        var tab = new TabPage("Evolução") { BackColor = BackColor };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 66, Padding = new Padding(12, 10, 8, 6), BackColor = Color.White };
        filters.Controls.Add(FilterBlock("Analisar por", _evoDimension, 150));
        filters.Controls.Add(FilterBlock("Equipe / ACS", _evoEntity, 290));
        filters.Controls.Add(FilterBlock("Indicador", _evoMetric, 200));
        tab.Controls.Add(_evoChart);
        tab.Controls.Add(filters);
        return tab;
    }

    private TabPage BuildComparisonTab()
    {
        var tab = new TabPage("Comparação") { BackColor = BackColor };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 66, Padding = new Padding(12, 10, 8, 6), BackColor = Color.White };
        filters.Controls.Add(FilterBlock("Equipe", _cmpTeam, 200));
        filters.Controls.Add(FilterBlock("Mês principal", _cmpMonthA, 155));
        filters.Controls.Add(FilterBlock("Comparar com", _cmpMonthB, 165));
        filters.Controls.Add(FilterBlock("Indicador", _cmpMetric, 190));

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 430, BackColor = BackColor };
        split.Panel1.Padding = new Padding(12, 6, 12, 6);
        split.Panel1.Controls.Add(_cmpChart);
        split.Panel2.Padding = new Padding(12, 4, 12, 10);
        split.Panel2.Controls.Add(_cmpGrid);

        tab.Controls.Add(split);
        tab.Controls.Add(_cmpSummary);
        tab.Controls.Add(filters);
        return tab;
    }

    private TabPage BuildConfigurationTab()
    {
        var tab = new TabPage("Configuração") { BackColor = BackColor };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(10, 8, 6, 4), BackColor = Color.White };
        bar.Controls.Add(ActionButton("Salvar alterações", (_, _) => SaveConfiguration(), true));
        bar.Controls.Add(ActionButton("Recarregar", (_, _) => RefreshConfigurationGrid()));
        bar.Controls.Add(_cfgInfo);
        tab.Controls.Add(_cfgGrid);
        tab.Controls.Add(bar);
        return tab;
    }

    private TabPage BuildDataTab()
    {
        var tab = new TabPage("Dados importados") { BackColor = BackColor };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(10, 7, 6, 4), BackColor = Color.White };
        bar.Controls.Add(ActionButton("Excluir selecionados", (_, _) => DeleteSelected()));
        bar.Controls.Add(new Label { AutoSize = true, Padding = new Padding(12, 8, 0, 0), Text = "Os PDFs são identificados por mês + equipe + microárea." });
        tab.Controls.Add(_dataGrid);
        tab.Controls.Add(bar);
        return tab;
    }

    private void WireEvents()
    {
        foreach (var c in new[] { _dashMonth, _dashTeam, _dashMicro, _dashProfessional })
            c.SelectedIndexChanged += (_, _) => RefreshDashboard();
        _dashTeam.SelectedIndexChanged += (_, _) => RefreshDashboardMicroChoices();

        _evoDimension.SelectedIndexChanged += (_, _) => { RefreshEvolutionEntityChoices(); RefreshEvolution(); };
        _evoEntity.SelectedIndexChanged += (_, _) => RefreshEvolution();
        _evoMetric.SelectedIndexChanged += (_, _) => RefreshEvolution();

        foreach (var c in new[] { _cmpTeam, _cmpMonthA, _cmpMonthB, _cmpMetric })
            c.SelectedIndexChanged += (_, _) => RefreshComparison();

        DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
        DragDrop += (_, e) =>
        {
            var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
            if (files != null) ImportFiles(files.Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray());
        };
    }

    private void RefreshAll()
    {
        RefreshDashboardChoices();
        RefreshEvolutionChoices();
        RefreshComparisonChoices();
        RefreshConfigurationGrid();
        RefreshDashboard();
        RefreshEvolution();
        RefreshComparison();
        RefreshDataGrid();
    }

    // ---------------- Dashboard ----------------

    private void RefreshDashboardChoices()
    {
        var records = _store.Db.Records;
        var currentMonth = ChoiceValue(_dashMonth);
        var months = records.Select(r => r.MonthKey).Distinct().OrderByDescending(x => x)
            .Select(m => new Choice(m, MonthText(m))).ToList();
        SetChoices(_dashMonth, months, true, currentMonth);
        if (_dashMonth.SelectedIndex == 0 && months.Count > 0) _dashMonth.SelectedIndex = 1;

        var teams = records.GroupBy(r => r.TeamCode)
            .Select(g => new Choice(g.Key, g.First().TeamLabel)).OrderBy(x => x.Text);
        SetChoices(_dashTeam, teams, true, ChoiceValue(_dashTeam));

        var professionals = _store.Db.TerritoryConfigs.Values
            .Where(c => !string.IsNullOrWhiteSpace(c.ProfessionalName))
            .Select(c => c.ProfessionalName).Concat(records.Where(r => !string.IsNullOrWhiteSpace(r.Professional)).Select(r => r.Professional))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Select(x => new Choice(x, x));
        SetChoices(_dashProfessional, professionals, true, ChoiceValue(_dashProfessional));
        RefreshDashboardMicroChoices();
    }

    private void RefreshDashboardMicroChoices()
    {
        var teamCode = ChoiceValue(_dashTeam);
        var old = ChoiceValue(_dashMicro);
        var q = _store.Db.Records.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(teamCode)) q = q.Where(r => r.TeamCode == teamCode);
        var micros = q.GroupBy(r => new { r.TeamCode, r.MicroCode })
            .Select(g => new Choice(g.Key.MicroCode, TerritoryDisplay(g.Key.TeamCode, g.Key.MicroCode, g.First().Professional)))
            .OrderBy(x => x.Value).ToList();
        SetChoices(_dashMicro, micros, true, old);
    }

    private List<VisitRecord> GetDashboardFiltered()
    {
        var q = _store.Db.Records.AsEnumerable();
        var month = ChoiceValue(_dashMonth);
        var team = ChoiceValue(_dashTeam);
        var micro = ChoiceValue(_dashMicro);
        var prof = ChoiceValue(_dashProfessional);
        if (!string.IsNullOrWhiteSpace(month)) q = q.Where(r => r.MonthKey == month);
        if (!string.IsNullOrWhiteSpace(team)) q = q.Where(r => r.TeamCode == team);
        if (!string.IsNullOrWhiteSpace(micro)) q = q.Where(r => r.MicroCode == micro);
        if (!string.IsNullOrWhiteSpace(prof)) q = q.Where(r => TerritoryProfessional(r.TeamCode, r.MicroCode, r.Professional).Equals(prof, StringComparison.OrdinalIgnoreCase));
        return q.OrderBy(r => r.TeamLabel).ThenBy(r => r.MicroCode).ToList();
    }

    private void RefreshDashboard()
    {
        var rows = GetDashboardFiltered();
        var done = rows.Sum(r => r.Realizadas);
        var absent = rows.Sum(r => r.Ausentes);
        var total = rows.Sum(r => r.Total);
        var populationRate = PopulationRate(rows);

        _cardDone.Text = done.ToString("N0");
        _cardAbsent.Text = absent.ToString("N0");
        _cardTotal.Text = total.ToString("N0");
        _cardRate.Text = populationRate.HasValue ? populationRate.Value.ToString("N1") + "%" : "Configurar";

        var groups = rows.GroupBy(r => new { r.TeamCode, r.TeamLabel, r.MicroCode })
            .Select(g => new
            {
                g.Key.TeamCode,
                g.Key.TeamLabel,
                g.Key.MicroCode,
                Done = g.Sum(x => x.Realizadas),
                Absent = g.Sum(x => x.Ausentes),
                Professional = TerritoryProfessional(g.Key.TeamCode, g.Key.MicroCode, g.First().Professional)
            })
            .OrderBy(x => x.TeamLabel).ThenBy(x => x.MicroCode).ToList();

        _dashChart.Labels = groups.Select(x => string.IsNullOrWhiteSpace(x.Professional) ? $"MA {x.MicroCode}" : x.Professional).ToList();
        _dashChart.Series = new List<ChartSeries>
        {
            new() { Name = "Realizadas", Values = groups.Select(x => (double)x.Done).ToList() },
            new() { Name = "Ausentes", Values = groups.Select(x => (double)x.Absent).ToList() }
        };
        _dashChart.ValueSuffix = "";
        _dashChart.Invalidate();
        FillDashboardGrid(_dashGrid, rows);
    }

    // ---------------- Evolução ----------------

    private void RefreshEvolutionChoices()
    {
        SetChoices(_evoDimension, new[]
        {
            new Choice("team", "Equipe"),
            new Choice("micro", "ACS / Microárea"),
            new Choice("professional", "Profissional")
        }, false, ChoiceValue(_evoDimension));
        SetChoices(_evoMetric, new[]
        {
            new Choice("done", "Visitas realizadas"),
            new Choice("absent", "Ausentes"),
            new Choice("total", "Total do relatório"),
            new Choice("rate", "% sobre cadastrados")
        }, false, ChoiceValue(_evoMetric));
        if (_evoDimension.SelectedIndex < 0) _evoDimension.SelectedIndex = 0;
        if (_evoMetric.SelectedIndex < 0) _evoMetric.SelectedIndex = 0;
        RefreshEvolutionEntityChoices();
    }

    private void RefreshEvolutionEntityChoices()
    {
        var mode = ChoiceValue(_evoDimension);
        var old = ChoiceValue(_evoEntity);
        IEnumerable<Choice> items;

        if (mode == "micro")
        {
            items = _store.Db.Records
                .GroupBy(r => new { r.TeamCode, r.MicroCode })
                .Select(g => new Choice($"{g.Key.TeamCode}|{g.Key.MicroCode}",
                    $"{g.First().TeamLabel} - {TerritoryDisplay(g.Key.TeamCode, g.Key.MicroCode, g.First().Professional)}"))
                .OrderBy(x => x.Text);
        }
        else if (mode == "professional")
        {
            items = _store.Db.TerritoryConfigs.Values
                .Where(c => !string.IsNullOrWhiteSpace(c.ProfessionalName))
                .Select(c => c.ProfessionalName)
                .Concat(_store.Db.Records.Where(r => !string.IsNullOrWhiteSpace(r.Professional)).Select(r => r.Professional))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).Select(x => new Choice(x, x));
        }
        else
        {
            items = _store.Db.Records.GroupBy(r => r.TeamCode)
                .Select(g => new Choice(g.Key, g.First().TeamLabel)).OrderBy(x => x.Text);
        }

        SetChoices(_evoEntity, items, false, old);
        if (_evoEntity.SelectedIndex < 0 && _evoEntity.Items.Count > 0) _evoEntity.SelectedIndex = 0;
    }

    private void RefreshEvolution()
    {
        var entity = ChoiceValue(_evoEntity);
        if (string.IsNullOrWhiteSpace(entity))
        {
            _evoChart.Labels = new(); _evoChart.Series = new(); _evoChart.Invalidate(); return;
        }

        var mode = ChoiceValue(_evoDimension);
        var metric = ChoiceValue(_evoMetric);
        var q = _store.Db.Records.AsEnumerable();

        if (mode == "micro")
        {
            var p = entity.Split('|');
            q = q.Where(r => p.Length == 2 && r.TeamCode == p[0] && r.MicroCode == p[1]);
        }
        else if (mode == "professional")
        {
            q = q.Where(r => TerritoryProfessional(r.TeamCode, r.MicroCode, r.Professional).Equals(entity, StringComparison.OrdinalIgnoreCase));
        }
        else q = q.Where(r => r.TeamCode == entity);

        var groups = q.GroupBy(r => r.MonthKey).OrderBy(g => g.Key).ToList();
        _evoChart.Labels = groups.Select(g => MonthText(g.Key)).ToList();
        _evoChart.Series = new List<ChartSeries>
        {
            new() { Name = _evoMetric.Text, Values = groups.Select(g => MetricValue(g, metric)).ToList() }
        };
        _evoChart.ValueSuffix = metric == "rate" ? "%" : "";
        _evoChart.ChartTitle = $"Evolução mensal — {_evoEntity.Text}";
        _evoChart.Invalidate();
    }

    private double MetricValue(IEnumerable<VisitRecord> records, string metric)
    {
        var list = records.ToList();
        return metric switch
        {
            "absent" => list.Sum(r => r.Ausentes),
            "total" => list.Sum(r => r.Total),
            "rate" => PopulationRate(list) ?? 0,
            _ => list.Sum(r => r.Realizadas)
        };
    }

    // ---------------- Comparação ----------------

    private void RefreshComparisonChoices()
    {
        var records = _store.Db.Records;
        var teams = records.GroupBy(r => r.TeamCode).Select(g => new Choice(g.Key, g.First().TeamLabel))
            .Concat(_store.Db.TerritoryConfigs.Values.GroupBy(c => c.TeamCode).Select(g => new Choice(g.Key, g.First().TeamLabel)))
            .GroupBy(x => x.Value).Select(g => g.First()).OrderBy(x => x.Text);
        SetChoices(_cmpTeam, teams, false, ChoiceValue(_cmpTeam));
        if (_cmpTeam.SelectedIndex < 0 && _cmpTeam.Items.Count > 0) _cmpTeam.SelectedIndex = 0;

        var months = records.Select(r => r.MonthKey).Distinct().OrderByDescending(x => x).Select(m => new Choice(m, MonthText(m))).ToList();
        SetChoices(_cmpMonthA, months, false, ChoiceValue(_cmpMonthA));
        if (_cmpMonthA.SelectedIndex < 0 && _cmpMonthA.Items.Count > 0) _cmpMonthA.SelectedIndex = 0;
        SetChoices(_cmpMonthB, months, true, ChoiceValue(_cmpMonthB), "Sem segundo mês");

        SetChoices(_cmpMetric, new[]
        {
            new Choice("done", "Visitas realizadas"),
            new Choice("absent", "Ausentes"),
            new Choice("rate", "% sobre cadastrados")
        }, false, ChoiceValue(_cmpMetric));
        if (_cmpMetric.SelectedIndex < 0) _cmpMetric.SelectedIndex = 0;
    }

    private void RefreshComparison()
    {
        var team = ChoiceValue(_cmpTeam);
        var monthA = ChoiceValue(_cmpMonthA);
        var monthB = ChoiceValue(_cmpMonthB);
        var metric = ChoiceValue(_cmpMetric);

        if (string.IsNullOrWhiteSpace(team) || string.IsNullOrWhiteSpace(monthA))
        {
            _cmpChart.Labels = new(); _cmpChart.Series = new(); _cmpChart.Invalidate();
            _cmpGrid.Rows.Clear(); return;
        }

        var microCodes = _store.Db.Records.Where(r => r.TeamCode == team).Select(r => r.MicroCode)
            .Concat(_store.Db.TerritoryConfigs.Values.Where(c => c.TeamCode == team).Select(c => c.MicroCode))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var labels = new List<string>();
        var valuesA = new List<double>();
        var valuesB = new List<double>();
        _cmpGrid.Rows.Clear();

        foreach (var micro in microCodes)
        {
            var sample = _store.Db.Records.FirstOrDefault(r => r.TeamCode == team && r.MicroCode == micro);
            var label = TerritoryDisplay(team, micro, sample?.Professional ?? "");
            var a = ComparisonMetric(team, micro, monthA, metric);
            var b = string.IsNullOrWhiteSpace(monthB) ? (double?)null : ComparisonMetric(team, micro, monthB, metric);
            labels.Add(label);
            valuesA.Add(a);
            if (b.HasValue) valuesB.Add(b.Value);
            _cmpGrid.Rows.Add(label, micro, FormatMetric(a, metric), b.HasValue ? FormatMetric(b.Value, metric) : "—");
        }

        _cmpChart.Labels = labels;
        _cmpChart.Series = new List<ChartSeries>
        {
            new() { Name = MonthText(monthA), Values = valuesA }
        };
        if (!string.IsNullOrWhiteSpace(monthB))
            _cmpChart.Series.Add(new ChartSeries { Name = MonthText(monthB), Values = valuesB });
        _cmpChart.ValueSuffix = metric == "rate" ? "%" : "";
        _cmpChart.ChartTitle = $"{_cmpTeam.Text} — {_cmpMetric.Text}";
        _cmpChart.Invalidate();

        var second = string.IsNullOrWhiteSpace(monthB) ? "" : $" x {MonthText(monthB)}";
        _cmpSummary.Text = $"{_cmpTeam.Text} • {MonthText(monthA)}{second} • cada barra representa um ACS/território";
        _cmpGrid.Columns[2].HeaderText = MonthText(monthA);
        _cmpGrid.Columns[3].HeaderText = string.IsNullOrWhiteSpace(monthB) ? "Segundo mês" : MonthText(monthB);
    }

    private double ComparisonMetric(string teamCode, string microCode, string month, string metric)
    {
        var rows = _store.Db.Records.Where(r => r.TeamCode == teamCode && r.MicroCode == microCode && r.MonthKey == month).ToList();
        if (metric == "absent") return rows.Sum(r => r.Ausentes);
        if (metric == "rate")
        {
            var cfg = _store.GetTerritoryConfig(teamCode, microCode);
            if (cfg == null || cfg.RegisteredPeople <= 0) return 0;
            return (double)rows.Sum(r => r.Realizadas) / cfg.RegisteredPeople * 100.0;
        }
        return rows.Sum(r => r.Realizadas);
    }

    // ---------------- Configuração ----------------

    private void RefreshConfigurationGrid()
    {
        _cfgGrid.Rows.Clear();
        var territories = _store.Db.Records.Select(r => new { r.TeamCode, r.TeamName, r.TeamLabel, r.MicroCode })
            .Concat(_store.Db.TerritoryConfigs.Values.Select(c => new { c.TeamCode, TeamName = c.TeamName, TeamLabel = c.TeamLabel, c.MicroCode }))
            .GroupBy(x => DataStore.LinkKey(x.TeamCode, x.MicroCode))
            .Select(g => g.First())
            .OrderBy(x => x.TeamLabel).ThenBy(x => x.MicroCode);

        foreach (var t in territories)
        {
            var cfg = _store.GetTerritoryConfig(t.TeamCode, t.MicroCode);
            _cfgGrid.Rows.Add(t.TeamCode, t.TeamLabel, t.MicroCode, cfg?.ProfessionalName ?? "", cfg?.RegisteredPeople ?? 0);
        }
    }

    private void SaveConfiguration()
    {
        _cfgGrid.EndEdit();
        int saved = 0;
        foreach (DataGridViewRow row in _cfgGrid.Rows)
        {
            if (row.IsNewRow) continue;
            var teamCode = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
            var teamName = Convert.ToString(row.Cells[1].Value)?.Trim() ?? "";
            var micro = Convert.ToString(row.Cells[2].Value)?.Trim() ?? "";
            var professional = Convert.ToString(row.Cells[3].Value)?.Trim() ?? "";
            var populationText = Convert.ToString(row.Cells[4].Value)?.Trim() ?? "0";
            if (string.IsNullOrWhiteSpace(teamCode) || string.IsNullOrWhiteSpace(micro)) continue;
            if (!int.TryParse(populationText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var population))
            {
                MessageBox.Show(this, $"Valor inválido de pessoas cadastradas em {teamName} / microárea {micro}.", "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _store.SaveTerritoryConfig(teamCode, teamName, micro, professional, population);
            saved++;
        }
        RefreshAll();
        MessageBox.Show(this, $"Configuração salva para {saved} território(s).", "Configuração", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ---------------- Utilidades de território/taxa ----------------

    private string TerritoryProfessional(string teamCode, string microCode, string fallback = "")
    {
        var cfg = _store.GetTerritoryConfig(teamCode, microCode);
        if (cfg != null && !string.IsNullOrWhiteSpace(cfg.ProfessionalName)) return cfg.ProfessionalName;
        return fallback ?? "";
    }

    private string TerritoryDisplay(string teamCode, string microCode, string fallbackProfessional = "")
    {
        var p = TerritoryProfessional(teamCode, microCode, fallbackProfessional);
        return string.IsNullOrWhiteSpace(p) ? $"MA {microCode}" : p;
    }

    private double? PopulationRate(IEnumerable<VisitRecord> records)
    {
        var list = records.ToList();
        if (list.Count == 0) return 0;

        var denominator = 0;
        var groups = list.GroupBy(r => new { r.MonthKey, r.TeamCode, r.MicroCode });
        foreach (var g in groups)
        {
            var cfg = _store.GetTerritoryConfig(g.Key.TeamCode, g.Key.MicroCode);
            if (cfg == null || cfg.RegisteredPeople <= 0) return null;
            denominator += cfg.RegisteredPeople;
        }
        if (denominator <= 0) return null;
        return (double)list.Sum(r => r.Realizadas) / denominator * 100.0;
    }

    private int RegisteredPeople(VisitRecord r) => _store.GetTerritoryConfig(r.TeamCode, r.MicroCode)?.RegisteredPeople ?? 0;

    // ---------------- Dados/importação/exportação ----------------

    private void RefreshDataGrid() => FillDashboardGrid(_dataGrid,
        _store.Db.Records.OrderByDescending(r => r.MonthKey).ThenBy(r => r.TeamLabel).ThenBy(r => r.MicroCode));

    private void FillDashboardGrid(DataGridView grid, IEnumerable<VisitRecord> records)
    {
        grid.Rows.Clear();
        foreach (var r in records)
        {
            var pop = RegisteredPeople(r);
            var rate = pop > 0 ? (double)r.Realizadas / pop * 100.0 : (double?)null;
            grid.Rows.Add(r.MonthLabel, r.TeamLabel, TerritoryDisplay(r.TeamCode, r.MicroCode, r.Professional), r.MicroCode,
                r.Realizadas, r.Ausentes, r.Total, pop > 0 ? pop : "", rate.HasValue ? rate.Value.ToString("N1") + "%" : "—", r.SourceFile, r.IdentityKey);
        }
    }

    private void ImportWithDialog()
    {
        using var dlg = new OpenFileDialog { Filter = "Relatórios PDF (*.pdf)|*.pdf", Multiselect = true, Title = "Selecione os relatórios do e-SUS" };
        if (dlg.ShowDialog(this) == DialogResult.OK) ImportFiles(dlg.FileNames);
    }

    private void ImportFiles(string[] files)
    {
        if (files.Length == 0) return;
        Cursor = Cursors.WaitCursor;
        int added = 0, updated = 0;
        var errors = new List<string>();
        try
        {
            foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var record = PdfImporter.ParsePdf(file);
                    if (_store.Upsert(record)) added++; else updated++;
                }
                catch (Exception ex) { errors.Add(Path.GetFileName(file) + ": " + ex.Message); }
            }
        }
        finally { Cursor = Cursors.Default; }
        RefreshAll();
        var msg = $"Importação concluída.\n\nNovos: {added}\nAtualizados: {updated}\nCom erro: {errors.Count}";
        if (errors.Count > 0) msg += "\n\n" + string.Join("\n", errors.Take(8));
        MessageBox.Show(this, msg, "Importação de PDFs", MessageBoxButtons.OK, errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void ExportCsv()
    {
        var rows = GetDashboardFiltered();
        using var dlg = new SaveFileDialog { Filter = "Arquivo CSV (*.csv)|*.csv", FileName = $"visitas_esus_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        using var sw = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true));
        sw.WriteLine("Mes;Equipe;MicroArea;ACS;Realizadas;Ausentes;TotalRelatorio;PessoasCadastradas;TaxaSobreCadastrados;Arquivo");
        foreach (var r in rows)
        {
            var pop = RegisteredPeople(r);
            var rate = pop > 0 ? (double)r.Realizadas / pop * 100.0 : (double?)null;
            sw.WriteLine(string.Join(';', Csv(r.MonthLabel), Csv(r.TeamLabel), Csv(r.MicroCode), Csv(TerritoryDisplay(r.TeamCode, r.MicroCode, r.Professional)),
                r.Realizadas, r.Ausentes, r.Total, pop, rate.HasValue ? rate.Value.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")) : "", Csv(r.SourceFile)));
        }
        MessageBox.Show(this, "CSV exportado com sucesso.", "Exportação", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportPng()
    {
        using var dlg = new SaveFileDialog { Filter = "Imagem PNG (*.png)|*.png", FileName = $"painel_visitas_{DateTime.Now:yyyyMMdd_HHmm}.png" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var page = _tabs.SelectedTab;
        if (page == null) return;
        using var bmp = new Bitmap(page.Width, page.Height);
        page.DrawToBitmap(bmp, new Rectangle(Point.Empty, page.Size));
        bmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
        MessageBox.Show(this, "Imagem exportada com sucesso.", "Exportação", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BackupDatabase()
    {
        var path = _store.Backup();
        MessageBox.Show(this, "Backup criado na mesma pasta do aplicativo:\n\n" + path, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RestoreDatabase()
    {
        using var dlg = new OpenFileDialog { Filter = "Base/backup JSON (*.json)|*.json", Title = "Selecione um backup" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, "A base atual será substituída pelo arquivo selecionado. Continuar?", "Restaurar base", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _store.Restore(dlg.FileName);
        RefreshAll();
    }

    private void DeleteSelected()
    {
        var keys = _dataGrid.SelectedRows.Cast<DataGridViewRow>()
            .Select(r => Convert.ToString(r.Cells[10].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return;
        if (MessageBox.Show(this, $"Excluir {keys.Count} registro(s) selecionado(s)?", "Excluir", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _store.Delete(_store.Db.Records.Where(r => keys.Contains(r.IdentityKey)).ToList());
        RefreshAll();
    }

    // ---------------- Componentes ----------------

    private static DataGridView DashboardGrid()
    {
        var g = BaseGrid();
        g.Columns.Add("Mes", "Mês");
        g.Columns.Add("Equipe", "Equipe");
        g.Columns.Add("ACS", "ACS");
        g.Columns.Add("Micro", "Microárea");
        g.Columns.Add("Realizadas", "Realizadas");
        g.Columns.Add("Ausentes", "Ausentes");
        g.Columns.Add("Total", "Total");
        g.Columns.Add("Cadastrados", "Cadastrados");
        g.Columns.Add("Taxa", "% sobre cadastrados");
        g.Columns.Add("Arquivo", "Arquivo");
        g.Columns.Add("Key", "Key");
        g.Columns[10].Visible = false;
        g.Columns[0].Width = 110; g.Columns[1].Width = 150; g.Columns[2].Width = 180; g.Columns[3].Width = 80;
        g.Columns[9].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        return g;
    }

    private static DataGridView ComparisonGrid()
    {
        var g = BaseGrid();
        g.Columns.Add("ACS", "ACS");
        g.Columns.Add("Micro", "Microárea");
        g.Columns.Add("A", "Mês principal");
        g.Columns.Add("B", "Segundo mês");
        g.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        g.Columns[1].Width = 90; g.Columns[2].Width = 150; g.Columns[3].Width = 150;
        return g;
    }

    private static DataGridView ConfigurationGrid()
    {
        var g = BaseGrid();
        g.AllowUserToAddRows = false;
        g.ReadOnly = false;
        g.Columns.Add("TeamCode", "Código da equipe");
        g.Columns.Add("Equipe", "Equipe / Área");
        g.Columns.Add("Micro", "Microárea");
        g.Columns.Add("ACS", "Nome do ACS / profissional");
        g.Columns.Add("Cadastrados", "Pessoas cadastradas no território");
        g.Columns[0].Visible = false;
        g.Columns[1].ReadOnly = true;
        g.Columns[2].ReadOnly = true;
        g.Columns[1].Width = 220; g.Columns[2].Width = 100; g.Columns[3].Width = 300; g.Columns[4].Width = 220;
        return g;
    }

    private static DataGridView BaseGrid() => new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        RowHeadersVisible = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None
    };

    private static Button ActionButton(string text, EventHandler click, bool primary = false)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 34, Padding = new Padding(9, 0, 9, 0), FlatStyle = FlatStyle.Flat, Margin = new Padding(4, 0, 4, 0) };
        b.FlatAppearance.BorderColor = primary ? Color.FromArgb(28, 99, 168) : Color.FromArgb(190, 196, 205);
        b.BackColor = primary ? Color.FromArgb(28, 99, 168) : Color.White;
        b.ForeColor = primary ? Color.White : Color.FromArgb(35, 39, 45);
        b.Click += click;
        return b;
    }

    private static Control FilterBlock(string title, ComboBox combo, int width = 160)
    {
        var p = new Panel { Width = width, Height = 46, Margin = new Padding(4, 0, 8, 0) };
        var l = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8.3F), ForeColor = Color.DimGray };
        combo.Dock = DockStyle.Bottom; combo.Width = width;
        p.Controls.Add(combo); p.Controls.Add(l);
        return p;
    }

    private static Panel Card(string title, Label value)
    {
        var p = new Panel { Width = 250, Height = 72, BackColor = Color.White, Margin = new Padding(4, 0, 10, 0), Padding = new Padding(12, 8, 8, 6) };
        var t = new Label { Text = title, Dock = DockStyle.Top, Height = 20, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.3F, FontStyle.Bold) };
        p.Controls.Add(value); p.Controls.Add(t);
        return p;
    }

    private static Label CardValue() => new() { Text = "0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = Color.FromArgb(28, 99, 168) };
    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };

    private static void SetChoices(ComboBox combo, IEnumerable<Choice> choices, bool includeAll, string selectedValue, string allText = "Todos")
    {
        var list = choices.ToList();
        combo.BeginUpdate();
        combo.Items.Clear();
        if (includeAll) combo.Items.Add(new Choice("", allText));
        foreach (var c in list) combo.Items.Add(c);
        combo.EndUpdate();

        var idx = -1;
        for (int i = 0; i < combo.Items.Count; i++)
            if (combo.Items[i] is Choice c && c.Value == selectedValue) { idx = i; break; }
        if (idx >= 0) combo.SelectedIndex = idx;
        else if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private static string ChoiceValue(ComboBox combo) => combo.SelectedItem is Choice c ? c.Value : "";
    private sealed record Choice(string Value, string Text) { public override string ToString() => Text; }

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private static string MonthText(string key)
    {
        if (!DateTime.TryParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return key;
        return d.ToString("MM/yyyy");
    }

    private static string FormatMetric(double value, string metric) => metric == "rate" ? value.ToString("N1") + "%" : value.ToString("N0");
}
