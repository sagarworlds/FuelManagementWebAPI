
using Dapper;


namespace DAL.Models
{
    public class auth_user_user_permissions 
    {
       
        public int id { get; set; }
        
        public int user_id { get; set; }
        
        public int permission_id { get; set; }

    }
}