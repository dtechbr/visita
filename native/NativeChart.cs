using System.Drawing.Drawing2D;

namespace VisitasESUS;

public enum NativeChartType { Bar, Line }

public sealed class ChartSeries
{
    public string Name { get; set; } = "";
    public List<double> Values { get; set; } = new();
}

public sealed class NativeChart : Control
{
    public string ChartTitle { get; set; } = "";
    public NativeChartType ChartType { get; set; } = NativeChartType.Bar;
    public List<string> Labels { get; set; } = new();
    public List<ChartSeries> Series { get; set; } = new();
    public string ValueSuffix { get; set; } = "";

    public NativeChart()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        Padding = new Padding(12);
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using var border = new Pen(Color.FromArgb(224, 228, 235));
        g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        if (!string.IsNullOrWhiteSpace(ChartTitle))
        {
            using var tf = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            g.DrawString(ChartTitle, tf, Brushes.Black, 18, 14);
        }

        if (Labels.Count == 0 || Series.Count == 0 || Series.All(s => s.Values.Count == 0))
        {
            using var f = new Font(Font.FontFamily, 10F);
            var msg = "Sem dados para os filtros selecionados.";
            var sz = g.MeasureString(msg, f);
            g.DrawString(msg, f, Brushes.Gray, (Width - sz.Width) / 2, (Height - sz.Height) / 2);
            return;
        }

        var left = 58f;
        var right = 24f;
        var top = 56f;
        var bottom = 66f;
        var plot = new RectangleF(left, top, Math.Max(10, Width - left - right), Math.Max(10, Height - top - bottom));
        var max = Series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max();
        if (max <= 0) max = 1;
        max *= 1.12;

        using var gridPen = new Pen(Color.FromArgb(235, 238, 243));
        using var axisPen = new Pen(Color.FromArgb(150, 156, 166));
        for (int i = 0; i <= 5; i++)
        {
            var y = plot.Bottom - plot.Height * i / 5f;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            var v = max * i / 5f;
            var txt = FormatValue(v);
            var sz = g.MeasureString(txt, Font);
            g.DrawString(txt, Font, Brushes.DimGray, plot.Left - sz.Width - 7, y - sz.Height / 2);
        }
        g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
        g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        if (ChartType == NativeChartType.Line) DrawLineChart(g, plot, max);
        else DrawBarChart(g, plot, max);
        DrawLegend(g, plot);
    }

    private void DrawBarChart(Graphics g, RectangleF plot, double max)
    {
        var groups = Math.Max(1, Labels.Count);
        var groupW = plot.Width / groups;
        var seriesCount = Math.Max(1, Series.Count);
        var barW = Math.Min(42f, Math.Max(6f, groupW * 0.68f / seriesCount));
        for (int i = 0; i < Labels.Count; i++)
        {
            var groupX = plot.Left + i * groupW;
            var totalBarsW = barW * seriesCount;
            var startX = groupX + (groupW - totalBarsW) / 2f;
            for (int s = 0; s < Series.Count; s++)
            {
                var value = i < Series[s].Values.Count ? Series[s].Values[i] : 0;
                var h = (float)(value / max * plot.Height);
                var rect = new RectangleF(startX + s * barW + 1, plot.Bottom - h, Math.Max(3, barW - 3), h);
                using var brush = new SolidBrush(GetSeriesColor(s));
                g.FillRectangle(brush, rect);
                if (h > 18)
                {
                    var t = FormatValue(value);
                    var sz = g.MeasureString(t, Font);
                    g.DrawString(t, Font, Brushes.White, rect.Left + (rect.Width - sz.Width) / 2, rect.Top + 3);
                }
            }
            DrawLabel(g, Labels[i], groupX + groupW / 2f, plot.Bottom + 7, groupW - 4);
        }
    }

    private void DrawLineChart(Graphics g, RectangleF plot, double max)
    {
        var count = Math.Max(Labels.Count, 1);
        var step = count <= 1 ? 0 : plot.Width / (count - 1);
        for (int s = 0; s < Series.Count; s++)
        {
            var values = Series[s].Values;
            if (values.Count == 0) continue;
            var pts = new List<PointF>();
            for (int i = 0; i < Labels.Count; i++)
            {
                var value = i < values.Count ? values[i] : 0;
                var x = count <= 1 ? plot.Left + plot.Width / 2f : plot.Left + i * step;
                var y = plot.Bottom - (float)(value / max * plot.Height);
                pts.Add(new PointF(x, y));
            }
            using var pen = new Pen(GetSeriesColor(s), 2.5f);
            if (pts.Count > 1) g.DrawLines(pen, pts.ToArray());
            foreach (var p in pts)
            {
                using var b = new SolidBrush(GetSeriesColor(s));
                g.FillEllipse(b, p.X - 4, p.Y - 4, 8, 8);
            }
        }
        for (int i = 0; i < Labels.Count; i++)
        {
            var x = count <= 1 ? plot.Left + plot.Width / 2f : plot.Left + i * step;
            DrawLabel(g, Labels[i], x, plot.Bottom + 7, Math.Max(60, step - 4));
        }
    }

    private void DrawLabel(Graphics g, string label, float centerX, float y, float maxWidth)
    {
        var text = label;
        if (text.Length > 16) text = text[..14] + "…";
        var sz = g.MeasureString(text, Font);
        g.DrawString(text, Font, Brushes.DimGray, centerX - Math.Min(sz.Width, maxWidth) / 2f, y);
    }

    private void DrawLegend(Graphics g, RectangleF plot)
    {
        if (Series.Count <= 1) return;
        var x = plot.Left;
        var y = 34f;
        for (int i = 0; i < Series.Count; i++)
        {
            using var b = new SolidBrush(GetSeriesColor(i));
            g.FillRectangle(b, x, y + 3, 12, 12);
            g.DrawString(Series[i].Name, Font, Brushes.DimGray, x + 17, y);
            x += g.MeasureString(Series[i].Name, Font).Width + 42;
        }
    }

    private string FormatValue(double value)
    {
        var text = Math.Abs(value - Math.Round(value)) < 0.001 ? Math.Round(value).ToString("N0") : value.ToString("N1");
        return text + ValueSuffix;
    }

    private static Color GetSeriesColor(int index)
    {
        Color[] colors =
        {
            Color.FromArgb(28, 99, 168),
            Color.FromArgb(220, 121, 42),
            Color.FromArgb(49, 142, 91),
            Color.FromArgb(145, 80, 169),
            Color.FromArgb(187, 65, 74)
        };
        return colors[index % colors.Length];
    }
}
