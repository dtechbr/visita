using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace VisitasESUS;

/// <summary>
/// Camada exclusivamente visual da v6. Não altera regras de negócio, banco,
/// importação, cálculo ou estrutura dos dados da v5.
/// </summary>
internal static class ModernThemeV6
{
    private static readonly Color AppBackground = Color.FromArgb(244, 247, 251);
    private static readonly Color Surface = Color.White;
    private static readonly Color SurfaceAlt = Color.FromArgb(247, 249, 252);
    private static readonly Color Border = Color.FromArgb(226, 231, 238);
    private static readonly Color TextPrimary = Color.FromArgb(28, 36, 48);
    private static readonly Color TextSecondary = Color.FromArgb(103, 113, 128);
    private static readonly Color Accent = Color.FromArgb(30, 106, 184);
    private static readonly Color AccentHover = Color.FromArgb(24, 91, 160);
    private static readonly Color AccentSoft = Color.FromArgb(233, 243, 253);

    private static bool _applied;

    [ModuleInitializer]
    internal static void Initialize()
    {
        Application.Idle += ApplyWhenReady;
    }

    private static void ApplyWhenReady(object? sender, EventArgs e)
    {
        if (_applied) return;
        var form = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f is MainFormV5);
        if (form == null) return;

        _applied = true;
        Application.Idle -= ApplyWhenReady;
        Apply(form);
    }

    private static void Apply(Form form)
    {
        form.SuspendLayout();
        try
        {
            form.Text = "Painel de Visitas e-SUS • v6";
            form.BackColor = AppBackground;
            form.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            form.MinimumSize = new Size(1080, 700);

            var toolbar = form.Controls.OfType<FlowLayoutPanel>()
                .FirstOrDefault(p => p.Dock == DockStyle.Top && p.Height <= 90);
            if (toolbar != null)
                StyleMainToolbar(toolbar);

            foreach (var status in Descendants<StatusStrip>(form))
                StyleStatus(status);

            foreach (var tabs in Descendants<TabControl>(form))
                StyleTabs(tabs);

            foreach (var page in Descendants<TabPage>(form))
            {
                page.BackColor = AppBackground;
                page.ForeColor = TextPrimary;
            }

            foreach (var flow in Descendants<FlowLayoutPanel>(form))
                StyleFlowPanel(flow, toolbar);

            foreach (var combo in Descendants<ComboBox>(form))
                StyleCombo(combo);

            foreach (var button in Descendants<Button>(form))
                StyleButton(button);

            foreach (var grid in Descendants<DataGridView>(form))
                StyleGrid(grid);

            foreach (var split in Descendants<SplitContainer>(form))
            {
                split.BackColor = AppBackground;
                split.SplitterWidth = 6;
                split.Panel1.BackColor = AppBackground;
                split.Panel2.BackColor = AppBackground;
            }

            foreach (var chart in Descendants<NativeChart>(form))
            {
                chart.BackColor = Surface;
                chart.Font = new Font("Segoe UI", 9F);
                chart.Margin = new Padding(0);
                RoundControl(chart, 14);
            }

            foreach (var panel in Descendants<Panel>(form))
                StylePotentialCard(panel);

            foreach (var label in Descendants<Label>(form))
            {
                if (label.Parent is not Panel card || !IsMetricCard(card))
                {
                    if (label.ForeColor == Color.DimGray || label.ForeColor == SystemColors.ControlText)
                        label.ForeColor = TextSecondary;
                }
            }
        }
        finally
        {
            form.ResumeLayout(true);
            form.PerformLayout();
            form.Invalidate(true);
        }
    }

    private static void StyleMainToolbar(FlowLayoutPanel toolbar)
    {
        toolbar.SuspendLayout();
        toolbar.Height = 76;
        toolbar.Padding = new Padding(16, 11, 12, 9);
        toolbar.BackColor = Surface;
        toolbar.WrapContents = false;
        toolbar.AutoScroll = false;

        if (!toolbar.Controls.OfType<BrandBlock>().Any())
        {
            var brand = new BrandBlock
            {
                Width = 265,
                Height = 52,
                Margin = new Padding(0, 0, 12, 0)
            };
            toolbar.Controls.Add(brand);
            toolbar.Controls.SetChildIndex(brand, 0);
        }

        toolbar.Paint -= PaintToolbarSeparator;
        toolbar.Paint += PaintToolbarSeparator;
        toolbar.ResumeLayout(true);
    }

    private static void PaintToolbarSeparator(object? sender, PaintEventArgs e)
    {
        if (sender is not Control c) return;
        using var pen = new Pen(Border);
        e.Graphics.DrawLine(pen, 0, c.Height - 1, c.Width, c.Height - 1);
    }

    private static void StyleStatus(StatusStrip status)
    {
        status.BackColor = Surface;
        status.ForeColor = TextSecondary;
        status.Font = new Font("Segoe UI", 8.5F);
        status.SizingGrip = false;
        status.Padding = new Padding(12, 3, 12, 3);
        foreach (ToolStripItem item in status.Items)
        {
            item.ForeColor = TextSecondary;
            if (item is ToolStripStatusLabel label && !label.Text.StartsWith("v6", StringComparison.OrdinalIgnoreCase))
            {
                var idx = label.Text.IndexOf('•');
                label.Text = idx >= 0 ? "v6 " + label.Text[idx..] : "v6 • " + label.Text;
            }
        }
    }

    private static void StyleTabs(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(150, 42);
        tabs.Padding = new Point(14, 6);
        tabs.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular);
        tabs.BackColor = AppBackground;

        tabs.DrawItem -= DrawModernTab;
        tabs.DrawItem += DrawModernTab;
        tabs.Paint -= PaintTabsBackground;
        tabs.Paint += PaintTabsBackground;
    }

    private static void PaintTabsBackground(object? sender, PaintEventArgs e)
    {
        if (sender is not TabControl tabs) return;
        using var pen = new Pen(Border);
        e.Graphics.DrawLine(pen, 0, tabs.ItemSize.Height + 1, tabs.Width, tabs.ItemSize.Height + 1);
    }

    private static void DrawModernTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0 || e.Index >= tabs.TabCount) return;

        var bounds = e.Bounds;
        var selected = e.Index == tabs.SelectedIndex;
        var bg = selected ? AccentSoft : Surface;
        var fg = selected ? Accent : TextSecondary;

        using var background = new SolidBrush(bg);
        e.Graphics.FillRectangle(background, bounds);

        if (selected)
        {
            using var underline = new SolidBrush(Accent);
            e.Graphics.FillRectangle(underline, bounds.Left + 12, bounds.Bottom - 3, bounds.Width - 24, 3);
        }

        var text = tabs.TabPages[e.Index].Text;
        using var font = new Font("Segoe UI Semibold", 9.5F, selected ? FontStyle.Bold : FontStyle.Regular);
        TextRenderer.DrawText(e.Graphics, text, font, bounds, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void StyleFlowPanel(FlowLayoutPanel flow, FlowLayoutPanel? mainToolbar)
    {
        if (ReferenceEquals(flow, mainToolbar)) return;

        if (flow.Dock == DockStyle.Top)
        {
            flow.BackColor = Surface;
            var top = Math.Max(8, flow.Padding.Top);
            flow.Padding = new Padding(16, top, 12, Math.Max(5, flow.Padding.Bottom));
            flow.Paint -= PaintToolbarSeparator;
            flow.Paint += PaintToolbarSeparator;
        }
        else if (flow.Controls.OfType<Panel>().Any(IsMetricCard))
        {
            flow.BackColor = AppBackground;
            flow.Padding = new Padding(16, 12, 8, 10);
        }
    }

    private static void StyleCombo(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Surface;
        combo.ForeColor = TextPrimary;
        combo.Font = new Font("Segoe UI", 9.5F);
        combo.IntegralHeight = false;
        combo.DropDownHeight = 280;
        combo.Margin = new Padding(0, 2, 0, 0);
    }

    private static void StyleButton(Button button)
    {
        var primary = button.Text.Contains("Importar", StringComparison.OrdinalIgnoreCase) ||
                      button.Text.Contains("Salvar", StringComparison.OrdinalIgnoreCase);

        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Height = 36;
        button.Padding = new Padding(12, 0, 12, 0);
        button.Margin = new Padding(4, 8, 4, 0);
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular);
        button.Cursor = Cursors.Hand;
        button.BackColor = primary ? Accent : SurfaceAlt;
        button.ForeColor = primary ? Color.White : TextPrimary;
        button.FlatAppearance.MouseDownBackColor = primary ? AccentHover : Color.FromArgb(232, 236, 242);
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(238, 242, 247);
        RoundControl(button, 9);
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceAlt,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 9F),
            SelectionBackColor = SurfaceAlt,
            SelectionForeColor = TextPrimary,
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            SelectionBackColor = AccentSoft,
            SelectionForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.2F),
            Padding = new Padding(8, 0, 8, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(251, 252, 254),
            ForeColor = TextPrimary,
            SelectionBackColor = AccentSoft,
            SelectionForeColor = TextPrimary,
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.RowTemplate.Height = 36;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = true;
    }

    private static void StylePotentialCard(Panel panel)
    {
        if (!IsMetricCard(panel)) return;

        panel.BackColor = Surface;
        panel.BorderStyle = BorderStyle.None;
        panel.Margin = new Padding(4, 0, 12, 0);
        panel.Padding = new Padding(17, 9, 12, 8);
        panel.Height = Math.Max(panel.Height, 78);
        RoundControl(panel, 13);

        var title = panel.Controls.OfType<Label>()
            .FirstOrDefault(l => l.Font.Size <= 10 && !string.IsNullOrWhiteSpace(l.Text));
        var value = panel.Controls.OfType<Label>()
            .OrderByDescending(l => l.Font.Size)
            .FirstOrDefault();

        if (title != null)
        {
            title.ForeColor = TextSecondary;
            title.Font = new Font("Segoe UI Semibold", 8.2F, FontStyle.Regular);
        }
        if (value != null)
        {
            value.ForeColor = Accent;
            value.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
        }

        panel.Paint -= PaintCardBorder;
        panel.Paint += PaintCardBorder;
    }

    private static void PaintCardBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectangle(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 13);
        using var pen = new Pen(Border);
        e.Graphics.DrawPath(pen, path);
    }

    private static bool IsMetricCard(Panel panel)
    {
        if (panel.Height < 60 || panel.Height > 105 || panel.Width < 180 || panel.Width > 340) return false;
        var text = string.Join(" ", panel.Controls.OfType<Label>().Select(l => l.Text)).ToUpperInvariant();
        return text.Contains("VISITAS REALIZADAS") || text.Contains("AUSENTES") ||
               text.Contains("TOTAL DO RELATÓRIO") || text.Contains("SOBRE CADASTRADOS");
    }

    private static void RoundControl(Control control, int radius)
    {
        void ApplyRegion()
        {
            if (control.Width <= 1 || control.Height <= 1) return;
            using var path = RoundedRectangle(new Rectangle(0, 0, control.Width, control.Height), radius);
            control.Region?.Dispose();
            control.Region = new Region(path);
        }

        ApplyRegion();
        control.Resize += (_, _) => ApplyRegion();
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(2, radius * 2);
        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        var path = new GraphicsPath();
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static IEnumerable<T> Descendants<T>(Control root) where T : class
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private sealed class BrandBlock : Control
    {
        public BrandBlock()
        {
            DoubleBuffered = true;
            BackColor = Surface;
            ForeColor = TextPrimary;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var circle = new Rectangle(2, 7, 38, 38);
            using var circleBrush = new SolidBrush(Accent);
            e.Graphics.FillEllipse(circleBrush, circle);
            using var iconFont = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, "V", iconFont, circle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            using var titleFont = new Font("Segoe UI Semibold", 13.5F, FontStyle.Bold);
            using var subtitleFont = new Font("Segoe UI", 8.2F, FontStyle.Regular);
            TextRenderer.DrawText(e.Graphics, "Painel de Visitas", titleFont,
                new Rectangle(50, 4, Width - 52, 27), TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, "e-SUS • análise territorial", subtitleFont,
                new Rectangle(51, 29, Width - 53, 20), TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
