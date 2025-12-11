using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using DepoTakip.Models; 

namespace DepoTakip.DataAccess
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductEntry> ProductEntries { get; set; }
        public DbSet<ProductUsage> ProductUsages { get; set; }

//protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//{
 //   string sharedDbPath = @"\\DESKTOP-NU1VRGI\DepoVeriTabani\DepoTakip.db";
 //           optionsBuilder.UseSqlite($"Data Source={sharedDbPath};Cache=Shared;");
//}


      //  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
     //  {
//           string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepoTakip.db");
      //      optionsBuilder.UseSqlite($"Data Source={dbPath}");
      //  }


   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DepoTakip"
                );

                // Log klasör yolunu yaz (hata için)
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "db_debug_log.txt"),
                    $"[{DateTime.Now}] Ensuring folder: {folder}{Environment.NewLine}");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "db_debug_log.txt"),
                        $"[{DateTime.Now}] Folder created.{Environment.NewLine}");
                }

                string dbPath = Path.Combine(folder, "DepoTakip.db");

                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "db_debug_log.txt"),
                    $"[{DateTime.Now}] DB Path: {dbPath}{Environment.NewLine}");

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
            catch (Exception ex)
            {
                // Oluşan hatayı masaüstüne hemen kaydet
                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "veritabani_hata_log.txt"),
                    $"OnConfiguring EXCEPTION: {ex}\n\nDate: {DateTime.Now}");
                throw; // tekrar fırlat; üstteki try-catch de yakalasın
            }
        }

    }
}