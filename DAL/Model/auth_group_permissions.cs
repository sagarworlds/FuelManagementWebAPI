
using Dapper;
namespace DAL.Models
{
    
    public class auth_group_permissions
    {
        
        public int id { get; set; }
        
        public int group_id { get; set; }
        
        public int permission_id { get; set; }
    }
}