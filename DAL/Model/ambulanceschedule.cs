using Dapper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DAL.Models
{
    public class ambulanceschedule
    {
        public int id { get; set; }
        public string Day { get; set; }
        public string Time { get; set; }
        public bool Availability { get; set; }
        public int Count { get; set; }
        

    }
}