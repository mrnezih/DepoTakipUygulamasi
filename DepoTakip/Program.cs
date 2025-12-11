using System;
using System.IO;
using System.Windows.Forms;
using DepoTakip.DataAccess;

namespace DepoTakip
{
    internal static class Program
    {
       [STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    ApplicationConfiguration.Initialize();

    try
    {
        // DB oluşturma/uygulama migration'ı
        try
        {
            using (var db = new DepoTakip.DataAccess.DatabaseContext())
            {
                // Burada oluşursa, veritabanı oluşturma zamanında neler olduğunu loglayacağız
                db.Database.EnsureCreated();
            }
        }
        catch (Exception exDb)
        {
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "veritabani_hata_log.txt"),
                $"EnsureCreated EXCEPTION: {exDb}\n\nDate: {DateTime.Now}");
            // Sonra uygulama yine açılmasın: hata logu yazıldıktan sonra çökmesini engellemek için devam edebilirsin,
            // ama şimdilik yine throw et ki hatayı görüp düzeltelim:
            throw;
        }

        Application.Run(new Form1());
    }
    catch (Exception ex)
    {
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "uygulama_hata_log.txt"),
            $"{ex}\n\nDate: {DateTime.Now}");
        MessageBox.Show("Uygulama başlatılırken hata oluştu. Masaüstüne yazılan log dosyalarını bana at.");
    }
}

    }
}
