namespace HRMS.API.Models.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RoleName { get; set; } = "Employee";
    public Guid OrganizationId { get; set; }   
}

public class UserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegisterRequest
{
    // Organization data (maps to auth.organizations table)
    public string CompanyName { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string CompanySize { get; set; } = string.Empty; // Used to determine subscription type
    
    // User data (maps to auth.users table)
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}


public class UpdateProfileRequest
{
    public string? EmployeeNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Dictionary<string, object> Address { get; set; } = new();
    public DateTime? DateOfBirth { get; set; }
    public DateTime? HireDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public decimal? BasicSalary { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public string? WorkLocation { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public Dictionary<string, object> EmergencyContact { get; set; } = new();
    public Guid? roleId { get; set; }

    public IFormFile? ProfilePicture { get; set; }
}


public class ResetPasswordRequest
{
    public string password { get; set; } = string.Empty;
    public string token { get; set; } = string.Empty;
}

public class PasswordResetToken
{
    public Guid tokenid { get; set; }
    public Guid userid { get; set; }
    public string token { get; set; } = string.Empty;
    public DateTime expiresat { get; set; }
    public DateTime createdat { get; set; }
    public bool used { get; set; }
}



public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}