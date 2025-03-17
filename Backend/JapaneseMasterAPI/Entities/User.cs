using Microsoft.AspNetCore.Identity;

namespace JapaneseMasterAPI.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string? RefreshToken { get; set; } = string.Empty;

        public DateTime? RTokenExpiryDate { get; set; }
    }
}
