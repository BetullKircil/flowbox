using System.Security.Claims;
using FlowBox.Api.Enums;
using FlowBox.Api.Service;

namespace FlowBox.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Caller? ToCaller(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");

        if (!Guid.TryParse(id, out var userId) || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            return null;
        }

        return new Caller(userId, userRole);
    }
}
