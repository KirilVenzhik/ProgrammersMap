using Microsoft.AspNetCore.Identity;

namespace ProgChecklist.Data;

public class AppUser : IdentityUser
{
    /// <summary>Registration date (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
