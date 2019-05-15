using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Model
{
    [Table("tbl_Setting")]
    public class Setting
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerSetting { get; set; }
    }
}
