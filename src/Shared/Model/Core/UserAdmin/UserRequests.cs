using System.ComponentModel.DataAnnotations;

namespace DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

/// <summary>Body of <c>POST api/users</c>.</summary>
public class CreateUserRequest
{
    [Required]
    public UserProfileItem User { get; set; }

    [Required]
    public string Role { get; set; }

    /// <summary>Public id of the organization to place the user in; null = the caller's primary organization.</summary>
    public string OrganizationId { get; set; }
}

/// <summary>Body of <c>PUT api/users/{id}</c>.</summary>
public class UpdateUserRequest
{
    [Required]
    public UserProfileItem User { get; set; }

    [Required]
    public string Role { get; set; }
}

/// <summary>Body of <c>PUT api/users/{id}/password</c> (an administrator setting a user's password).</summary>
public class SetUserPasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }
}
