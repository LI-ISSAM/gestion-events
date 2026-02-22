using Microsoft.EntityFrameworkCore;
using GestionEvenements.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GestionEvenements.Models;

namespace GestionEvenements.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[AccountController] User {Email} logged in successfully.", model.Email);
                    return LocalRedirect(returnUrl ?? "/");
                }
                
                ModelState.AddModelError(string.Empty, "Email ou mot de passe invalide.");
                _logger.LogWarning("[AccountController] Failed login attempt for {Email}", model.Email);
            }
            
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("[AccountController] New user {Email} registered successfully.", model.Email);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var profileData = new UserProfileViewModel
            {
                User = user,
                TotalEventsCreated = await _context.Events.CountAsync(e => e.CreatedById == user.Id),
                TotalEventsAttended = await _context.Inscriptions
                    .CountAsync(i => i.UserId == user.Id && i.Statut == "Confirmée"),
                TotalComments = await _context.Comments.CountAsync(c => c.UserId == user.Id),
                AverageRating = await _context.Comments
                    .Where(c => c.UserId == user.Id)
                    .AnyAsync() 
                    ? await _context.Comments
                        .Where(c => c.UserId == user.Id)
                        .AverageAsync(c => (double)c.Note)
                    : 0,
                RecentEvents = await _context.Events
                    .Where(e => e.CreatedById == user.Id)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RegisteredEvents = await _context.Inscriptions
                    .Where(i => i.UserId == user.Id && i.Statut == "Confirmée")
                    .Include(i => i.Event)
                    .OrderByDescending(i => i.DateInscription)
                    .Take(5)
                    .Select(i => i.Event)
                    .ToListAsync(),
                Notifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id && !n.EstLue)
                    .OrderByDescending(n => n.DateCreation)
                    .Take(5)
                    .ToListAsync()
            };

            return View(profileData);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserProfileEditViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                Ville = user.Ville,
                Pays = user.Pays,
                DateNaissance = user.DateNaissance?.ToString("yyyy-MM-dd")
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View("EditProfile", model);
            }

            try
            {
                if (user.Email != model.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", "Cet email est déjà utilisé par un autre compte.");
                        return View("EditProfile", model);
                    }
                    user.Email = model.Email;
                    user.UserName = model.Email; 
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Bio = model.Bio;
                user.Ville = model.Ville;
                user.Pays = model.Pays;

                if (!string.IsNullOrEmpty(model.DateNaissance))
                {
                    if (DateTime.TryParse(model.DateNaissance, out var date))
                    {
                        user.DateNaissance = date;
                    }
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("[AccountController] Profile updated for user {UserId}", user.Id);
                    TempData["SuccessMessage"] = "Profil mis à jour avec succès!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[AccountController] Error updating profile: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de la mise à jour du profil.";
            }

            return View("EditProfile", model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("[AccountController] Password changed successfully for user {UserId}", user.Id);
                TempData["SuccessMessage"] = "Mot de passe changé avec succès!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.DerniereConnexion = DateTime.Now;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}