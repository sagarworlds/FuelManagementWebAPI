using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAL.Models
{
    public class ambulancebooking 
    {
        public int id { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public DateTime DateBooked { get; set; }
        public string Purpose { get; set; }
        public string Day { get; set; }
        public string Time { get; set; }

    }
}