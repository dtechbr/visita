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
            const string sample = "Relatório do E-SUS - Desfecho de Visitas - Sintético Filtros: Data Inicial: 01/09/2026 Data Final: 30/09/2026 Unidade de Saúde: 1701-1 - USF VILA DUTRA Equipe/Área: 0001487701 - DUTRA I Micro Área: 01 - MICRO AREA 01 USF VILA DUTRA Desfecho de Visitas Quantidade Visitas em Domicílio (ACS) VISITAS REALIZADAS 8 AUSENTES 32 Total de visitas de Visitas em Domicílio (ACS)..: 40 Total Geral..: 40";
            var r = PdfImporter.ParseText(sample, "selftest.pdf");
            if (r.MonthKey != "2026-09" || r.TeamName != "DUTRA I" || r.MicroCode != "01" || r.Realizadas != 8 || r.Ausentes != 32 || r.Total != 40)
                throw new InvalidOperationException("O parser não retornou os valores esperados.");
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
