using Dapper;



namespace DAL.Models
{
    public class auth_permission 
    {
       
        public int id { get; set; }
        
        public int content_type_id { get; set; }
        
        public string codename { get; set; }
    }
}