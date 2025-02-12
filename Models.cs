using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;

namespace MFAServer;

public class AddUserDto
{
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!; 
}

public record LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class OtpValidationDto
{
    //public string SecretKey { get; set; } = null!;
    public string Email { get; set; } = null!; 
    public string Code { get; set; } = null!;
}

public class OtpSetupResponse
{
    public string SecretKey { get; set; } = null!;
    public string QrCodeUri { get; set; } = null!;
    public string QrCodeBase64 { get; set; } = null!;
}

public class User : IdentityUser<long>
{
    public string FullName { get; set; } = null!;
    public string SecretKey { get; set; } = "";  // this should be encrypted
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    [NotMapped]
    public List<string> Roles { get; set; } = [];
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

}

public class Role : IdentityRole<long>
{
    public long ProjectId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    //
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

    public virtual ICollection<RoleClaim> RoleClaims { get; set; } = [];

    [NotMapped]
    public List<string> Claims { get; set; } = [];

}

public class RoleClaim : IdentityRoleClaim<long>
{
    public virtual Role? Role { get; set; }
}

public class UserRole : IdentityUserRole<long>
{
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

