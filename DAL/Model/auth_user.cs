
using Dapper;
using System;

namespace DAL.Models
{

    [Table("tbl_User")]
    public class auth_user
    {
        public int id { get; set; }

        public string password { get; set; }

        public bool is_superuser { get; set; }
        
        public string name { get; set; }

        public string email { get; set; }
        
        public bool is_active { get; set; }
        
        public DateTime last_login { get; set; }
    }
}