using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Model
{
    public class FuelDetail
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MeterReading { get; set; }
        public double TotalPrice { get; set; }
        public double AddedFuel { get; set; }
        public string Note { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        
    
    }
}