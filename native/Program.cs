namespace VisitasESUS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
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
}
