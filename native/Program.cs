namespace VisitasESUS;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase)))
        {
            RunSelfTest();
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            try
            {
                var log = Path.Combine(AppContext.BaseDirectory, "erro_inicializacao.txt");
                File.WriteAllText(log, ex.ToString());
                MessageBox.Show("O aplicativo encontrou um erro ao iniciar. Foi criado o arquivo 'erro_inicializacao.txt' na pasta do programa.\n\n" + ex.Message,
                    "Painel de Visitas e-SUS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }

    private static void RunSelfTest()
    {
        try
        {
            var samples = new[]
            {
                new
                {
                    Text = "Relatório do E-SUS Filtros: Data Inicial: 01/09/2026 Data Final: 30/09/2026 Unidade de Saúde: 1701-1 - USF VILA DUTRA Equipe/Área: 0001487701 - DUTRA I Micro Área: 01 - MICRO AREA 01 USF VILA DUTRA VISITAS REALIZADAS 8 AUSENTES 32 Total Geral..: 40",
                    Month = "2026-09", Team = "DUTRA I", Micro = "01", Real = 8, Aus = 32, Total = 40
                },
                new
                {
                    // Simula a ordem interna encontrada nos PDFs Jasper: rótulo e número sem espaço.
                    Text = "Relatório do E-SUS Filtros: Data Inicial: 01/04/2026 Data Final: 30/04/2026 Unidade de Saúde: 1701-1 - USF VILA DUTRA Equipe/Área: 0001487728 - DUTRA II Micro Área: 01 - MICRO AREA 01 USF VILA DUTRA VISITASREALIZADAS138AUSENTES290 TotalGeral..:428",
                    Month = "2026-04", Team = "DUTRA II", Micro = "01", Real = 138, Aus = 290, Total = 428
                },
                new
                {
                    // Relatórios podem conter VISITAS RECUSADAS entre realizadas e ausentes.
                    Text = "Relatório do E-SUS Filtros: Data Inicial: 01/01/2026 Data Final: 31/01/2026 Unidade de Saúde: 1701-1 - USF VILA DUTRA Equipe/Área: 0001487728 - DUTRA II Micro Área: 03 - MICRO AREA 03 USF VILA DUTRA VISITASREALIZADAS195VISITASRECUSADAS6AUSENTES525 TotalGeral..:726",
                    Month = "2026-01", Team = "DUTRA II", Micro = "03", Real = 195, Aus = 525, Total = 726
                }
            };

            foreach (var s in samples)
            {
                var r = PdfImporter.ParseText(s.Text, "selftest.pdf");
                if (r.MonthKey != s.Month || r.TeamName != s.Team || r.MicroCode != s.Micro ||
                    r.Realizadas != s.Real || r.Ausentes != s.Aus || r.Total != s.Total)
                {
                    throw new InvalidOperationException(
                        $"Parser divergente. Esperado {s.Month}/{s.Team}/{s.Micro}: {s.Real}/{s.Aus}/{s.Total}. " +
                        $"Obtido {r.MonthKey}/{r.TeamName}/{r.MicroCode}: {r.Realizadas}/{r.Ausentes}/{r.Total}.");
                }
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "selftest_ok.txt"), "OK");
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "selftest_erro.txt"), ex.ToString());
            Environment.ExitCode = 10;
        }
    }
}
