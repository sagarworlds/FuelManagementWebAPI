using Dapper;
using System;



namespace DAL.Model
{
    [Table("tbl_Customer")]
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Address { get; set; }

        public int PostCode { get; set; }

        public string Mobile { get; set; }

        public string Email { get; set; }
        public string APIKey { get; set; }
        public string DomainName { get; set; }
        
    }
}
