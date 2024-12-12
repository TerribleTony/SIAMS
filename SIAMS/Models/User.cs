using System.ComponentModel.DataAnnotations;

namespace SIAMS.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Salt { get; set; } = string.Empty;

        public string Role { get; set; } = "User";  // Default role
    }
}
