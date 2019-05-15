using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAPI.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}