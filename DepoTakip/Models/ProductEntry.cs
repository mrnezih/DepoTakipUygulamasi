using System;

namespace DepoTakip.Models
{
    public class ProductEntry
    {
        public int Id { get; set; }
        public string ProductName { get; set; }  = string.Empty;
        public string Brand { get; set; }  = string.Empty;
        public string CategoryName { get; set; }  = string.Empty;
        public DateTime EntryDate { get; set; }
        public int Quantity { get; set; }
    }
}