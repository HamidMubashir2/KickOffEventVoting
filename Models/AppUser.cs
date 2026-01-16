using System.ComponentModel.DataAnnotations;

namespace KickOffEvent.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }
        public string UserName { get; set; } = "";

        // required for candidate split
        public string Gender { get; set; } = ""; // "Male" or "Female"
        public string PasswordHash { get; set; } = ""; // demo: store hash (BCrypt recommended)
        public bool IsAdmin { get; set; }
    }
}
