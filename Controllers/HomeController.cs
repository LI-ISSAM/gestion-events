using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionEvenements.Data;
using GestionEvenements.Models;

namespace GestionEvenements.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Statistiques
                var stats = new
                {
                    TotalEvents = await _context.Events.CountAsync(e => !e.EstAnnule),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalInscriptions = await _context.Inscriptions.CountAsync(),
                    UpcomingEvents = await _context.Events.CountAsync(e => e.DateEvent > DateTime.Now && !e.EstAnnule)
                };
                ViewBag.Stats = stats;

                // Événements en vedette (les 3 prochains événements)
                var featuredEvents = await _context.Events
                    .Where(e => e.DateEvent > DateTime.Now && !e.EstAnnule)
                    .OrderBy(e => e.DateEvent)
                    .Take(3)
                    .ToListAsync();
                ViewBag.FeaturedEvents = featuredEvents;

                // Avis récents des utilisateurs (les 3 derniers commentaires avec note >= 4)
                var recentReviews = await _context.Comments
                    .Include(c => c.User)
                    .Include(c => c.Event)
                    .Where(c => c.Note >= 4) // Seulement les avis positifs
                    .OrderByDescending(c => c.DateCreation)
                    .Take(3)
                    .ToListAsync();
                ViewBag.RecentReviews = recentReviews;

                _logger.LogInformation("[HomeController] Index loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("[HomeController] Error loading Index: {Message}", ex.Message);
                ViewBag.Stats = new { TotalEvents = 0, TotalUsers = 0, TotalInscriptions = 0, UpcomingEvents = 0 };
                ViewBag.FeaturedEvents = new List<Event>();
                ViewBag.RecentReviews = new List<Comment>();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}