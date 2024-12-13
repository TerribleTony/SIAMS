using System.Collections.Generic;

namespace SIAMS.Models
{
    public class UserProfileViewModel
    {
        public User User { get; set; }
        public List<Log> RecentLogs { get; set; }
    }

    public class EditUserViewModel
    {
        public string Email { get; set; }
    }
}
