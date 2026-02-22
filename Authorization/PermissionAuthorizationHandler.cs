using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GestionEvenements.Authorization
{


    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Récupérer tous les claims de permission
            var permissions = context.User
                .FindAll("permission")
                .Select(static c => c.Value)
                .ToList();

            Console.WriteLine($"[DEBUG] Permissions de l'utilisateur: {string.Join(", ", permissions)}");
            Console.WriteLine($"[DEBUG] Permission requise: {requirement.Permission}");

            // Vérifier si l'utilisateur a la permission requise
            if (permissions.Contains(requirement.Permission))
            {
                Console.WriteLine($"[DEBUG] ✓ Permission '{requirement.Permission}' accordée");
                context.Succeed(requirement);
            }
            else
            {
                Console.WriteLine($"[DEBUG] ✗ Permission '{requirement.Permission}' refusée");
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>

    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
