

using Dapper;
namespace DAL.Models
{
    public class auth_user_groups 
    {       
        public int id { get; set; }
        
        public int user_id { get; set; }
        
        public int group_id { get; set; }
        
    }
}