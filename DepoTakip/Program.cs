namespace DepoTakip;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
{
    Application.Run(new Form1());
}
catch (Exception ex)
{
    File.WriteAllText("hata_log.txt", ex.ToString());
}
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}