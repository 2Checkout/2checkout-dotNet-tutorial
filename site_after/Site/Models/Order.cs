using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace Site.Models
{
    public class Order
    {
        public int ID { get; set; }
        public string OrderNumber { get; set; }
        public string DatePlaced { get; set; }
        public string CustomerName { get; set; }
        public string Total { get; set; }
        public string Refunded { get; set; }
    }

    public class OrderDBContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
    }
}