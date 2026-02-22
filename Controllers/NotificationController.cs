using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionEvenements.Data;
using GestionEvenements.Models;

namespace GestionEvenements.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var count = await _context.Notifications
                    .CountAsync(n => n.UserId == user.Id && !n.EstLue);

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError("[NotificationController] Error getting unread count: {Message}", ex.Message);
                return StatusCode(500, "Erreur lors du chargement des notifications.");
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetNotifications(int page = 1)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                const int pageSize = 20;
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.DateCreation)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError("[NotificationController] Error getting notifications: {Message}", ex.Message);
                return StatusCode(500, "Erreur lors du chargement des notifications.");
            }
        }

        [HttpPost("mark-as-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null || notification.UserId != user.Id)
                    return NotFound();

                notification.EstLue = true;
                notification.DateLecture = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("[NotificationController] Error marking notification as read: {Message}", ex.Message);
                return StatusCode(500, "Erreur lors de la mise à jour.");
            }
        }

        [HttpPost("clear-all")]
        public async Task<IActionResult> ClearAll()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("[NotificationController] Error clearing notifications: {Message}", ex.Message);
                return StatusCode(500, "Erreur lors de la suppression.");
            }
        }
    }
}
