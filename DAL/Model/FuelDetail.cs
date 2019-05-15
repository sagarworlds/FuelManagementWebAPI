using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Model
{
    [Table("tbl_FuelDetail")]
    public class FuelDetail
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MeterReading { get; set; }
        public int TotalPrice { get; set; }
        public int AddedFuel { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }


    }
}
