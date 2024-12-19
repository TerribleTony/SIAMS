using System.ComponentModel.DataAnnotations;


namespace SIAMS.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty!;

        [Required]
        public string PasswordHash { get; set; } = string.Empty!;

        [Required]
        public string Salt { get; set; } = string.Empty;

        public string Role { get; set; } = "User";  // Default role

        [Required]
        public string Email { get; set; } = string.Empty;

        public ICollection<Asset> Assets { get; set; } = new List<Asset>();

        public bool IsAdminRequested { get; set; } = false;

        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }

        public bool IsDeleted { get; set; }
        public ICollection<Log> ?Logs { get; set; }
       
    }
}
