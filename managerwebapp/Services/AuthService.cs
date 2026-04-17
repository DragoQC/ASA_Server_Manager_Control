using System.Security.Claims;
using managerwebapp.Data;
using Microsoft.AspNetCore.Identity;

namespace managerwebapp.Services;

public sealed class AuthService(UserManager<ApplicationUser> userManager)
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task EnsureDefaultAdminUserAsync(CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await _userManager.FindByNameAsync("admin");
        if (user is not null)
        {
            return;
        }

        ApplicationUser adminUser = new()
        {
            UserName = "admin",
            Email = "admin@local",
            EmailConfirmed = true
        };

        IdentityResult result = await _userManager.CreateAsync(adminUser, "admin");
        if (!result.Succeeded)
        {
            string errors = string.Join(' ', result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Default admin user creation failed. {errors}");
        }
    }

    public async Task<bool> MustChangePasswordAsync(ClaimsPrincipal principal)
    {
        ApplicationUser? user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }

        return await MustChangePasswordAsync(user);
    }

    public async Task<bool> MustChangePasswordAsync(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        ApplicationUser? user = await _userManager.FindByNameAsync(username);
        if (user is null)
        {
            return false;
        }

        return await MustChangePasswordAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ClaimsPrincipal principal, string newPassword)
    {
        ApplicationUser? user = await _userManager.GetUserAsync(principal);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "Current user was not found."
            });
        }

        string passwordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);
        user.PasswordHash = passwordHash;
        user.SecurityStamp = Guid.NewGuid().ToString("N");

        return await _userManager.UpdateAsync(user);
    }

    private async Task<bool> MustChangePasswordAsync(ApplicationUser user)
    {
        bool isDefaultAdmin = string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase);
        if (!isDefaultAdmin)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, "admin");
    }
}
