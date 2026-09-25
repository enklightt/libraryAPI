using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAPI.Models
{
    [Table("users")]
    public class User
    {
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("email")]
        [MaxLength(254)]
        public string Email { get; set; } = null!;

        [Column("password")]
        public string Password { get; set; } = null!;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; } = 2;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("equipped_title")]
        public string? EquippedTitle { get; set; }

        public Role Role { get; set; } = null!;
        public ICollection<Review> Reviews { get; set; } = [];
        public ICollection<Favorite> Favorites { get; set; } = [];
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public ICollection<WantToRead> WantToReads { get; set; } = [];
        public ICollection<Friend> Friends { get; set; } = [];
    }
}
