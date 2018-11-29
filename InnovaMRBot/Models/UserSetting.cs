
using System.Collections.Generic;

namespace InnovaMRBot.Models
{
    public class UserSetting
    {
        public UserSetting()
        {
            Users = new List<User>();
        }

        public List<User> Users { get; set; }
    }
}
