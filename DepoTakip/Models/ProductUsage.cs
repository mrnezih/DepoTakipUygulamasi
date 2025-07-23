using System;

namespace DepoTakip.Models
{
    public class ProductUsage
    {
        public int Id { get; set; }
        public string ProductName { get; set; }  = string.Empty;
        public string Brand { get; set; }  = string.Empty;
        public string CategoryName { get; set; }  = string.Empty;
        public string UsedBy { get; set; }  = string.Empty;
        public string UserLevel { get; set; }  = string.Empty;
        public DateTime UsageDate { get; set; }
        public int Quantity { get; set; }
    }
}