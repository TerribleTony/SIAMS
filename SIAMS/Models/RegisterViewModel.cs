using System.ComponentModel.DataAnnotations;

namespace SIAMS.Models
{
    public class RegisterViewModel
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

      
        public string Email { get; set; } = string.Empty;
    }
}
