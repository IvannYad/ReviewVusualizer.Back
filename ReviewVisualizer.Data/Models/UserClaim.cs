using System.ComponentModel.DataAnnotations.Schema;

namespace ReviewVisualizer.Data.Models
{
    public class UserClaim
    {
        public int Id { get; set; }
        
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public User User { get; set; }

        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
    }
}
