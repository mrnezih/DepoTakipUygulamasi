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

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    string sharedDbPath = @"\\DESKTOP-NU1VRGI\DepoVeriTabani\DepoTakip.db";
            optionsBuilder.UseSqlite($"Data Source={sharedDbPath};Cache=Shared;");
}


        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepoTakip.db");
        //    optionsBuilder.UseSqlite($"Data Source={dbPath}");
        //}
    }
}