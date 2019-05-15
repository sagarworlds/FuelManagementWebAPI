using Dapper;
using System;

namespace DAL.Model
{
    [Table("tbl_User")]
    public class AUser
    {         
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }        
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
