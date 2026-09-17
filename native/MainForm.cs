using System.Globalization;
using System.Text;

namespace VisitasESUS;

public sealed class MainForm : Form
{
    private readonly DataStore _store = new();
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusText = new();

    private readonly ComboBox _dashMonth = Combo();
    private readonly ComboBox _dashTeam = Combo();
    private readonly ComboBox _dashMicro = Combo();
    private readonly ComboBox _dashProfessional = Combo();
    private readonly Label _cardDone = CardValue();
    private readonly Label _cardAbsent = CardValue();
    private readonly Label _cardTotal = CardValue();
    private readonly Label _cardRate = CardValue();
    private readonly NativeChart _dashChart = new() { Dock = DockStyle.Fill, ChartTitle = "Visitas por microárea" };
    private readonly DataGridView _dashGrid = Grid();

    private readonly ComboBox _evoDimension = Combo();
    private readonly ComboBox _evoEntity = Combo();
    private readonly ComboBox _evoMetric = Combo();
    private readonly NativeChart _evoChart = new() { Dock = DockStyle.Fill, ChartType = NativeChartType.Line, ChartTitle = "Evolução mensal" };

    private readonly CompareSelector _cmpA = new("Cenário A");
    private readonly CompareSelector _cmpB = new("Cenário B");
    private readonly Label _cmpSummary = new() { Dock = DockStyle.Top, Height = 52, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Padding = new Padding(12, 8, 8, 4) };
    private readonly NativeChart _cmpChart = new() { Dock = DockStyle.Fill, ChartTitle = "Comparação" };

    private readonly DataGridView _dataGrid = Grid();

    public MainForm()
    {
        Text = "Painel de Visitas e-SUS";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(980, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);
        AllowDrop = true;

        BuildUi();
        WireEvents();
        RefreshAll();
        _statusText.Text = $"Base local: {_store.DatabasePath}";
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
        bar.Controls.Add(ActionButton("Vincular profissional", (_, _) => ShowProfessionalDialog()));
        bar.Controls.Add(ActionButton("Exportar CSV", (_, _) => ExportCsv()));
        bar.Controls.Add(ActionButton("Exportar PNG", (_, _) => ExportPng()));
        bar.Controls.Add(ActionButton("Backup da base", (_, _) => BackupDatabase()));
        bar.Controls.Add(ActionButton("Restaurar base", (_, _) => RestoreDatabase()));
        return bar;
    }

    private TabPage BuildDashboardTab()
    {
        var tab = new TabPage("Dashboard") { BackColor = BackColor };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(12, 8, 8, 4), BackColor = Color.White };
        filters.Controls.Add(FilterBlock("Mês", _dashMonth));
        filters.Controls.Add(FilterBlock("Equipe", _dashTeam));
        filters.Controls.Add(FilterBlock("Microárea", _dashMicro));
        filters.Controls.Add(FilterBlock("Profissional", _dashProfessional, 190));

        var cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(12, 8, 4, 6), BackColor = BackColor };
        cards.Controls.Add(Card("VISITAS REALIZADAS", _cardDone));
        cards.Controls.Add(Card("AUSENTES", _cardAbsent));
        cards.Controls.Add(Card("TOTAL", _cardTotal));
        cards.Controls.Add(Card("TAXA DE REALIZAÇÃO", _cardRate));

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 380, BackColor = BackColor };
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
        filters.Controls.Add(FilterBlock("Equipe / Microárea / Profissional", _evoEntity, 270));
        filters.Controls.Add(FilterBlock("Indicador", _evoMetric, 180));
        tab.Controls.Add(_evoChart);
        tab.Controls.Add(filters);
        return tab;
    }

    private TabPage BuildComparisonTab()
    {
        var tab = new TabPage("Comparação") { BackColor = BackColor };
        var selectors = new TableLayoutPanel { Dock = DockStyle.Top, Height = 190, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
        selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selectors.Controls.Add(_cmpA.BuildPanel(), 0, 0);
        selectors.Controls.Add(_cmpB.BuildPanel(), 1, 0);
        tab.Controls.Add(_cmpChart);
        tab.Controls.Add(_cmpSummary);
        tab.Controls.Add(selectors);
        return tab;
    }

    private TabPage BuildDataTab()
    {
        var tab = new TabPage("Dados importados") { BackColor = BackColor };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(10, 7, 6, 4), BackColor = Color.White };
        bar.Controls.Add(ActionButton("Excluir selecionados", (_, _) => DeleteSelected()));
        bar.Controls.Add(new Label { AutoSize = true, Padding = new Padding(12, 8, 0, 0), Text = "Duplo clique na linha para localizar o PDF de origem na coluna Arquivo." });
        tab.Controls.Add(_dataGrid);
        tab.Controls.Add(bar);
        return tab;
    }

    private void WireEvents()
    {
        foreach (var c in new[] { _dashMonth, _dashTeam, _dashMicro, _dashProfessional }) c.SelectedIndexChanged += (_, _) => RefreshDashboard();
        _dashTeam.SelectedIndexChanged += (_, _) => RefreshDashboardMicroChoices();
        _evoDimension.SelectedIndexChanged += (_, _) => { RefreshEvolutionEntityChoices(); RefreshEvolution(); };
        _evoEntity.SelectedIndexChanged += (_, _) => RefreshEvolution();
        _evoMetric.SelectedIndexChanged += (_, _) => RefreshEvolution();
        _cmpA.Changed += (_, _) => RefreshComparison();
        _cmpB.Changed += (_, _) => RefreshComparison();
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
        RefreshCompareChoices(_cmpA);
        RefreshCompareChoices(_cmpB);
        RefreshDashboard();
        RefreshEvolution();
        RefreshComparison();
        RefreshDataGrid();
    }

    private void RefreshDashboardChoices()
    {
        var records = _store.Db.Records;
        var currentMonth = ChoiceValue(_dashMonth);
        var months = records.Select(r => r.MonthKey).Distinct().OrderByDescending(x => x)
            .Select(m => new Choice(m, MonthText(m))).ToList();
        SetChoices(_dashMonth, months, true, currentMonth);
        if (_dashMonth.SelectedIndex == 0 && months.Count > 0) _dashMonth.SelectedIndex = 1;
        SetChoices(_dashTeam, records.Select(r => r.TeamLabel).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)), true, ChoiceValue(_dashTeam));
        SetChoices(_dashProfessional, records.Where(r => !string.IsNullOrWhiteSpace(r.Professional)).Select(r => r.Professional).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)), true, ChoiceValue(_dashProfessional));
        RefreshDashboardMicroChoices();
    }

    private void RefreshDashboardMicroChoices()
    {
        var team = ChoiceValue(_dashTeam);
        var old = ChoiceValue(_dashMicro);
        var q = _store.Db.Records.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(team)) q = q.Where(r => r.TeamLabel == team);
        var micros = q.Select(r => r.MicroCode).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)).ToList();
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
        if (!string.IsNullOrWhiteSpace(team)) q = q.Where(r => r.TeamLabel == team);
        if (!string.IsNullOrWhiteSpace(micro)) q = q.Where(r => r.MicroCode == micro);
        if (!string.IsNullOrWhiteSpace(prof)) q = q.Where(r => r.Professional == prof);
        return q.OrderBy(r => r.TeamLabel).ThenBy(r => r.MicroCode).ToList();
    }

    private void RefreshDashboard()
    {
        var rows = GetDashboardFiltered();
        var done = rows.Sum(r => r.Realizadas);
        var absent = rows.Sum(r => r.Ausentes);
        var total = rows.Sum(r => r.Total);
        _cardDone.Text = done.ToString("N0");
        _cardAbsent.Text = absent.ToString("N0");
        _cardTotal.Text = total.ToString("N0");
        _cardRate.Text = total == 0 ? "0%" : ((double)done / total * 100).ToString("N1") + "%";

        var groups = rows.GroupBy(r => new { r.TeamLabel, r.MicroCode })
            .Select(g => new { g.Key.TeamLabel, g.Key.MicroCode, Done = g.Sum(x => x.Realizadas), Absent = g.Sum(x => x.Ausentes) })
            .OrderBy(x => x.TeamLabel).ThenBy(x => x.MicroCode).ToList();
        var oneTeam = groups.Select(x => x.TeamLabel).Distinct().Count() <= 1;
        _dashChart.Labels = groups.Select(x => oneTeam ? $"MA {x.MicroCode}" : $"{x.TeamLabel} / {x.MicroCode}").ToList();
        _dashChart.Series = new List<ChartSeries>
        {
            new() { Name = "Realizadas", Values = groups.Select(x => (double)x.Done).ToList() },
            new() { Name = "Ausentes", Values = groups.Select(x => (double)x.Absent).ToList() }
        };
        _dashChart.ValueSuffix = "";
        _dashChart.Invalidate();
        FillGrid(_dashGrid, rows);
    }

    private void RefreshEvolutionChoices()
    {
        SetChoices(_evoDimension, new[] { new Choice("team", "Equipe"), new Choice("micro", "Microárea"), new Choice("professional", "Profissional") }, false, ChoiceValue(_evoDimension));
        SetChoices(_evoMetric, new[] { new Choice("done", "Visitas realizadas"), new Choice("absent", "Ausentes"), new Choice("total", "Total"), new Choice("rate", "Taxa de realização") }, false, ChoiceValue(_evoMetric));
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
            items = _store.Db.Records.Select(r => new Choice($"{r.TeamCode}|{r.MicroCode}", $"{r.TeamLabel} - Microárea {r.MicroCode}")).GroupBy(x => x.Value).Select(g => g.First()).OrderBy(x => x.Text);
        else if (mode == "professional")
            items = _store.Db.Records.Where(r => !string.IsNullOrWhiteSpace(r.Professional)).Select(r => r.Professional).Distinct().OrderBy(x => x).Select(x => new Choice(x, x));
        else
            items = _store.Db.Records.Select(r => r.TeamLabel).Distinct().OrderBy(x => x).Select(x => new Choice(x, x));
        SetChoices(_evoEntity, items, false, old);
        if (_evoEntity.SelectedIndex < 0 && _evoEntity.Items.Count > 0) _evoEntity.SelectedIndex = 0;
    }

    private void RefreshEvolution()
    {
        var entity = ChoiceValue(_evoEntity);
        if (string.IsNullOrWhiteSpace(entity)) { _evoChart.Labels = new(); _evoChart.Series = new(); _evoChart.Invalidate(); return; }
        var mode = ChoiceValue(_evoDimension);
        var metric = ChoiceValue(_evoMetric);
        var q = _store.Db.Records.AsEnumerable();
        if (mode == "micro")
        {
            var p = entity.Split('|');
            q = q.Where(r => p.Length == 2 && r.TeamCode == p[0] && r.MicroCode == p[1]);
        }
        else if (mode == "professional") q = q.Where(r => r.Professional == entity);
        else q = q.Where(r => r.TeamLabel == entity);

        var groups = q.GroupBy(r => r.MonthKey).OrderBy(g => g.Key).ToList();
        _evoChart.Labels = groups.Select(g => MonthText(g.Key)).ToList();
        _evoChart.Series = new List<ChartSeries>
        {
            new()
            {
                Name = _evoMetric.Text,
                Values = groups.Select(g => MetricValue(g, metric)).ToList()
            }
        };
        _evoChart.ValueSuffix = metric == "rate" ? "%" : "";
        _evoChart.ChartTitle = $"Evolução mensal — {_evoEntity.Text}";
        _evoChart.Invalidate();
    }

    private static double MetricValue(IEnumerable<VisitRecord> records, string metric)
    {
        var list = records.ToList();
        var done = list.Sum(r => r.Realizadas);
        var total = list.Sum(r => r.Total);
        return metric switch
        {
            "absent" => list.Sum(r => r.Ausentes),
            "total" => total,
            "rate" => total == 0 ? 0 : (double)done / total * 100,
            _ => done
        };
    }

    private void RefreshCompareChoices(CompareSelector s)
    {
        var records = _store.Db.Records;
        SetChoices(s.Month, records.Select(r => r.MonthKey).Distinct().OrderByDescending(x => x).Select(m => new Choice(m, MonthText(m))), true, ChoiceValue(s.Month));
        SetChoices(s.Team, records.Select(r => r.TeamLabel).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)), true, ChoiceValue(s.Team));
        SetChoices(s.Micro, records.Select(r => r.MicroCode).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)), true, ChoiceValue(s.Micro));
        SetChoices(s.Professional, records.Where(r => !string.IsNullOrWhiteSpace(r.Professional)).Select(r => r.Professional).Distinct().OrderBy(x => x).Select(x => new Choice(x, x)), true, ChoiceValue(s.Professional));
    }

    private void RefreshComparison()
    {
        var a = AggregateFor(_cmpA);
        var b = AggregateFor(_cmpB);
        _cmpChart.Labels = new() { "Realizadas", "Ausentes", "Total" };
        _cmpChart.Series = new()
        {
            new ChartSeries { Name = "Cenário A", Values = new() { a.done, a.absent, a.total } },
            new ChartSeries { Name = "Cenário B", Values = new() { b.done, b.absent, b.total } }
        };
        _cmpChart.ValueSuffix = "";
        _cmpChart.Invalidate();
        var ra = a.total == 0 ? 0 : (double)a.done / a.total * 100;
        var rb = b.total == 0 ? 0 : (double)b.done / b.total * 100;
        _cmpSummary.Text = $"Cenário A: {a.done:N0} realizadas | {a.absent:N0} ausentes | {a.total:N0} total | {ra:N1}%     •     Cenário B: {b.done:N0} realizadas | {b.absent:N0} ausentes | {b.total:N0} total | {rb:N1}%";
    }

    private (int done, int absent, int total) AggregateFor(CompareSelector s)
    {
        var q = _store.Db.Records.AsEnumerable();
        var month = ChoiceValue(s.Month); var team = ChoiceValue(s.Team); var micro = ChoiceValue(s.Micro); var prof = ChoiceValue(s.Professional);
        if (!string.IsNullOrWhiteSpace(month)) q = q.Where(r => r.MonthKey == month);
        if (!string.IsNullOrWhiteSpace(team)) q = q.Where(r => r.TeamLabel == team);
        if (!string.IsNullOrWhiteSpace(micro)) q = q.Where(r => r.MicroCode == micro);
        if (!string.IsNullOrWhiteSpace(prof)) q = q.Where(r => r.Professional == prof);
        var list = q.ToList();
        return (list.Sum(r => r.Realizadas), list.Sum(r => r.Ausentes), list.Sum(r => r.Total));
    }

    private void RefreshDataGrid() => FillGrid(_dataGrid, _store.Db.Records.OrderByDescending(r => r.MonthKey).ThenBy(r => r.TeamLabel).ThenBy(r => r.MicroCode));

    private static void FillGrid(DataGridView grid, IEnumerable<VisitRecord> records)
    {
        grid.Rows.Clear();
        foreach (var r in records)
            grid.Rows.Add(r.MonthLabel, r.TeamLabel, r.MicroCode, r.Professional, r.Realizadas, r.Ausentes, r.Total, r.SuccessRate.ToString("N1") + "%", r.SourceFile, r.IdentityKey);
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

    private void ShowProfessionalDialog()
    {
        var records = _store.Db.Records;
        if (records.Count == 0) { MessageBox.Show(this, "Importe ao menos um relatório antes de vincular profissionais."); return; }
        using var dlg = new ProfessionalLinkDialog(records, _store.Db.ProfessionalLinks);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _store.LinkProfessional(dlg.TeamCode, dlg.MicroCode, dlg.ProfessionalName);
            RefreshAll();
        }
    }

    private void ExportCsv()
    {
        var rows = GetDashboardFiltered();
        using var dlg = new SaveFileDialog { Filter = "Arquivo CSV (*.csv)|*.csv", FileName = $"visitas_esus_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        using var sw = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true));
        sw.WriteLine("Mes;Equipe;MicroArea;Profissional;Realizadas;Ausentes;Total;TaxaRealizacao;Arquivo");
        foreach (var r in rows)
            sw.WriteLine(string.Join(';', Csv(r.MonthLabel), Csv(r.TeamLabel), Csv(r.MicroCode), Csv(r.Professional), r.Realizadas, r.Ausentes, r.Total, r.SuccessRate.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")), Csv(r.SourceFile)));
        MessageBox.Show(this, "CSV exportado com sucesso.", "Exportação", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportPng()
    {
        using var dlg = new SaveFileDialog { Filter = "Imagem PNG (*.png)|*.png", FileName = $"dashboard_visitas_{DateTime.Now:yyyyMMdd_HHmm}.png" };
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
        var keys = _dataGrid.SelectedRows.Cast<DataGridViewRow>().Select(r => Convert.ToString(r.Cells[9].Value)).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return;
        if (MessageBox.Show(this, $"Excluir {keys.Count} registro(s) selecionado(s)?", "Excluir", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _store.Delete(_store.Db.Records.Where(r => keys.Contains(r.IdentityKey)).ToList());
        RefreshAll();
    }

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private static string MonthText(string key)
    {
        if (!DateTime.TryParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return key;
        return d.ToString("MM/yyyy");
    }

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
        var p = new Panel { Width = 235, Height = 72, BackColor = Color.White, Margin = new Padding(4, 0, 10, 0), Padding = new Padding(12, 8, 8, 6) };
        var t = new Label { Text = title, Dock = DockStyle.Top, Height = 20, ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.3F, FontStyle.Bold) };
        p.Controls.Add(value); p.Controls.Add(t);
        return p;
    }

    private static Label CardValue() => new() { Text = "0", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI", 21F, FontStyle.Bold), ForeColor = Color.FromArgb(28, 99, 168) };
    private static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };

    private static DataGridView Grid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            MultiSelect = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };
        g.Columns.Add("month", "Mês"); g.Columns.Add("team", "Equipe"); g.Columns.Add("micro", "Microárea"); g.Columns.Add("professional", "Profissional");
        g.Columns.Add("done", "Realizadas"); g.Columns.Add("absent", "Ausentes"); g.Columns.Add("total", "Total"); g.Columns.Add("rate", "Taxa"); g.Columns.Add("file", "Arquivo");
        var hidden = g.Columns.Add("key", "Chave"); g.Columns[hidden].Visible = false;
        return g;
    }

    private static void SetChoices(ComboBox combo, IEnumerable<Choice> choices, bool includeAll, string? desired)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        if (includeAll) combo.Items.Add(new Choice("", "Todos"));
        foreach (var c in choices) combo.Items.Add(c);
        combo.DisplayMember = nameof(Choice.Text);
        combo.ValueMember = nameof(Choice.Value);
        var idx = -1;
        for (int i = 0; i < combo.Items.Count; i++) if (combo.Items[i] is Choice c && c.Value == desired) { idx = i; break; }
        combo.SelectedIndex = idx >= 0 ? idx : (combo.Items.Count > 0 ? 0 : -1);
        combo.EndUpdate();
    }

    private static string ChoiceValue(ComboBox combo) => combo.SelectedItem is Choice c ? c.Value : "";

    private sealed record Choice(string Value, string Text)
    {
        public override string ToString() => Text;
    }

    private sealed class CompareSelector
    {
        public string Title { get; }
        public ComboBox Month { get; } = Combo();
        public ComboBox Team { get; } = Combo();
        public ComboBox Micro { get; } = Combo();
        public ComboBox Professional { get; } = Combo();
        public event EventHandler? Changed;
        public CompareSelector(string title)
        {
            Title = title;
            foreach (var c in new[] { Month, Team, Micro, Professional }) c.SelectedIndexChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        }
        public Control BuildPanel()
        {
            var box = new GroupBox { Text = Title, Dock = DockStyle.Fill, Padding = new Padding(10) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(4), WrapContents = true };
            flow.Controls.Add(FilterBlock("Mês", Month, 135)); flow.Controls.Add(FilterBlock("Equipe", Team, 180)); flow.Controls.Add(FilterBlock("Microárea", Micro, 125)); flow.Controls.Add(FilterBlock("Profissional", Professional, 180));
            box.Controls.Add(flow); return box;
        }
    }

    private sealed class ProfessionalLinkDialog : Form
    {
        private readonly List<VisitRecord> _records;
        private readonly Dictionary<string, string> _links;
        private readonly ComboBox _team = Combo();
        private readonly ComboBox _micro = Combo();
        private readonly TextBox _name = new() { Width = 280 };
        public string TeamCode => _team.SelectedItem is LinkChoice c ? c.Code : "";
        public string MicroCode => _micro.SelectedItem is LinkChoice c ? c.Code : "";
        public string ProfessionalName => _name.Text.Trim();

        public ProfessionalLinkDialog(IEnumerable<VisitRecord> records, Dictionary<string, string> links)
        {
            _records = records.ToList(); _links = links;
            Text = "Vincular profissional à microárea"; Width = 480; Height = 270; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(18), WrapContents = false };
            p.Controls.Add(new Label { Text = "Equipe", AutoSize = true }); p.Controls.Add(_team);
            p.Controls.Add(new Label { Text = "Microárea", AutoSize = true, Margin = new Padding(3, 8, 3, 0) }); p.Controls.Add(_micro);
            p.Controls.Add(new Label { Text = "Nome do profissional/ACS", AutoSize = true, Margin = new Padding(3, 8, 3, 0) }); p.Controls.Add(_name);
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
            var ok = new Button { Text = "Salvar", DialogResult = DialogResult.OK, AutoSize = true }; var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(ok); buttons.Controls.Add(cancel); p.Controls.Add(buttons); Controls.Add(p); AcceptButton = ok; CancelButton = cancel;
            foreach (var t in _records.GroupBy(r => r.TeamCode).Select(g => new LinkChoice(g.Key, g.First().TeamLabel)).OrderBy(x => x.Label)) _team.Items.Add(t);
            _team.DisplayMember = nameof(LinkChoice.Label); _micro.DisplayMember = nameof(LinkChoice.Label);
            _team.SelectedIndexChanged += (_, _) => RefreshMicros(); _micro.SelectedIndexChanged += (_, _) => RefreshName();
            if (_team.Items.Count > 0) _team.SelectedIndex = 0;
        }
        private void RefreshMicros()
        {
            _micro.Items.Clear();
            if (_team.SelectedItem is not LinkChoice t) return;
            foreach (var m in _records.Where(r => r.TeamCode == t.Code).GroupBy(r => r.MicroCode).Select(g => new LinkChoice(g.Key, $"{g.Key} - {g.First().MicroName}")).OrderBy(x => x.Code)) _micro.Items.Add(m);
            if (_micro.Items.Count > 0) _micro.SelectedIndex = 0;
        }
        private void RefreshName()
        {
            if (_team.SelectedItem is not LinkChoice t || _micro.SelectedItem is not LinkChoice m) return;
            _name.Text = _links.TryGetValue(DataStore.LinkKey(t.Code, m.Code), out var p) ? p : "";
        }
        private sealed record LinkChoice(string Code, string Label) { public override string ToString() => Label; }
    }
}
