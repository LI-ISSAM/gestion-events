using System.Linq;
using GestionEvenements.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionEvenements.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace GestionEvenements.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = new DashboardViewModel();

                // Statistiques globales
                dashboardData.TotalEvents = await _context.Events.CountAsync();
                dashboardData.TotalUsers = await _userManager.Users.CountAsync();
                dashboardData.TotalInscriptions = await _context.Inscriptions.CountAsync();
                dashboardData.PendingInscriptions = await _context.Inscriptions.CountAsync(i => i.Statut == "En attente");

                dashboardData.UpcomingEvents = await _context.Events
                    .Where(e => e.DateEvent > DateTime.Now && !e.EstAnnule)
                    .OrderBy(e => e.DateEvent)
                    .Take(5)
                    .Include(e => e.Statistiques)
                    .ToListAsync();

                dashboardData.RecentEvents = await _context.Events
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .Include(e => e.Statistiques)
                    .ToListAsync();

                dashboardData.RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                dashboardData.PendingRegistrations = await _context.Inscriptions
                    .Where(i => i.Statut == "En attente")
                    .Include(i => i.User)
                    .Include(i => i.Event)
                    .OrderBy(i => i.DateInscription)
                    .Take(10)
                    .ToListAsync();

                dashboardData.RecentComments = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Event)
                    .OrderByDescending(c => c.DateCreation)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("[DashboardController] Dashboard loaded successfully");
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError("[DashboardController] Error loading dashboard: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = new StatisticsViewModel();

                // Registrations par statut
                stats.RegistrationsByStatus = await _context.Inscriptions
                    .GroupBy(i => i.Statut)
                    .Select(g => new RegistrationStatusDto { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Événements par mois
                stats.EventsByMonth = await _context.Events
                    .GroupBy(e => e.CreatedAt.Month)
                    .Select(g => new EventByMonthDto { Month = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                // Top catégories
                stats.TopCategories = await _context.Events
                    .Where(e => e.CategorieId.HasValue && e.Categorie != null)
                    .GroupBy(e => e.Categorie.Nom)
                    .Select(g => new TopCategoryDto { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                stats.RatingDistribution = await _context.Comments
                    .GroupBy(c => c.Note)
                    .Select(g => new RatingDistributionDto { Rating = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Rating)
                    .ToListAsync();

                stats.MostActiveUsers = await _userManager.Users
                    .Include(u => u.Inscriptions)
                    .OrderByDescending(u => u.Inscriptions.Count)
                    .Take(10)
                    .Select(u => new MostActiveUserDto 
                    { 
                        User = u.FirstName + " " + u.LastName, 
                        Registrations = u.Inscriptions.Count 
                    })
                    .ToListAsync();

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError("[DashboardController] Error loading statistics: {Message}", ex.Message);
                return StatusCode(500, "Erreur lors du chargement des statistiques.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUsers(string? searchTerm = "", int page = 1)
        {
            try
            {
                const int pageSize = 10;
                
                var query = _userManager.Users.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u => 
                        (u.FirstName + " " + u.LastName).Contains(searchTerm) || 
                        u.Email.Contains(searchTerm));
                }

                var totalUsers = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Users = users;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;

                _logger.LogInformation("[DashboardController] ManageUsers loaded with {Count} users", users.Count);
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError("[DashboardController] Error in ManageUsers: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors du chargement des utilisateurs.";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [IgnoreAntiforgeryToken] 
        public async Task<IActionResult> SuspendUser([FromBody] SuspendUserRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UserId))
                    return Json(new { success = false, message = "ID utilisateur invalide." });

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                    return Json(new { success = false, message = "Utilisateur non trouvé." });

                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Suspend indefinitely
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[DashboardController] User suspended: {UserId}", request.UserId);
                    return Json(new { success = true, message = "Utilisateur suspendu avec succès." });
                }

                return Json(new { success = false, message = "Erreur lors de la suspension." });
            }
            catch (Exception ex)
            {
                _logger.LogError("[DashboardController] Error suspending user: {Message}", ex.Message);
                return Json(new { success = false, message = "Erreur serveur." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [IgnoreAntiforgeryToken] // Pour les requêtes AJAX JSON
        public async Task<IActionResult> UnsuspendUser([FromBody] SuspendUserRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UserId))
                    return Json(new { success = false, message = "ID utilisateur invalide." });

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                    return Json(new { success = false, message = "Utilisateur non trouvé." });

                user.LockoutEnd = null; 
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[DashboardController] User unsuspended: {UserId}", request.UserId);
                    return Json(new { success = true, message = "Utilisateur réactivé avec succès." });
                }

                return Json(new { success = false, message = "Erreur lors de la réactivation." });
            }
            catch (Exception ex)
            {
                _logger.LogError("[DashboardController] Error unsuspending user: {Message}", ex.Message);
                return Json(new { success = false, message = "Erreur serveur." });
            }
        }
    }

    public class SuspendUserRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}