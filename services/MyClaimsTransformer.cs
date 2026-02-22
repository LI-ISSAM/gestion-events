using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestionEvenements.Data;
using System.Text.Json;

/// <summary>
/// Transforme les claims provenant de Kinde pour les adapter à l'application .NET
/// Extrait uniquement les permissions (pas de rôles)
/// </summary>
public class MyClaimsTransformer : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;

    public MyClaimsTransformer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimsIdentity = principal.Identity as ClaimsIdentity;
        if (claimsIdentity == null)
        {
            return principal;
        }

        var kindeUserId = claimsIdentity.FindFirst("sub")?.Value;
        var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(kindeUserId) || string.IsNullOrEmpty(email))
        {
            return principal;
        }

        Console.WriteLine($"\n[DEBUG] Kinde User ID: {kindeUserId}");
        Console.WriteLine($"[DEBUG] Email: {email}");

        var permissionsClaim = claimsIdentity.FindFirst("permissions");
        if (permissionsClaim != null)
        {
            Console.WriteLine($"[DEBUG] Permissions trouvées: {permissionsClaim.Value}");
            
            try
            {
                // Parser les permissions (format JSON array ou texte)
                if (permissionsClaim.Value.StartsWith("["))
                {
                    using JsonDocument doc = JsonDocument.Parse(permissionsClaim.Value);
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var permission = element.GetString();
                        if (!string.IsNullOrEmpty(permission))
                        {
                            claimsIdentity.AddClaim(new Claim("permission", permission));
                            Console.WriteLine($"[DEBUG] ✓ Permission ajoutée: {permission}");
                        }
                    }
                }
                else
                {
                    var permissions = permissionsClaim.Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var permission in permissions)
                    {
                        var cleanPerm = permission.Trim().Trim('"').ToLower();
                        if (!string.IsNullOrEmpty(cleanPerm))
                        {
                            claimsIdentity.AddClaim(new Claim("permission", cleanPerm));
                            Console.WriteLine($"[DEBUG] ✓ Permission ajoutée: {cleanPerm}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Erreur parsing permissions: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[WARNING] ⚠️ Claim 'permissions' NOT FOUND!");
        }

        // Afficher les permissions finales
        Console.WriteLine("\n[DEBUG] PERMISSIONS FINALES:");
        var finalPermissions = claimsIdentity.FindAll("permission");
        foreach (var perm in finalPermissions)
        {
            Console.WriteLine($"[DEBUG] - {perm.Value}");
        }
        Console.WriteLine();

        return principal;
    }
}
