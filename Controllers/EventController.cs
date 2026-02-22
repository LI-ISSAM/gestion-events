using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GestionEvenements.Data;
using GestionEvenements.Models;
using GestionEvenements.Services;

namespace GestionEvenements.Controllers
{
    public class EventController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EventController> _logger;
        private readonly IEmailService _emailService;

        public EventController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<EventController> logger, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Event
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, string searchType, DateTime? searchDate)
        {
            try
            {
                var eventsQuery = _context.Events
                    .Include(e => e.CreatedByUser)
                    .Include(e => e.Categorie)
                    .Include(e => e.Statistiques)
                    .Include(e => e.Inscriptions)
                    .Where(e => !e.EstAnnule)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower().Trim();

                    switch (searchType)
                    {
                        case "nom":
                            eventsQuery = eventsQuery.Where(e => e.Titre.ToLower().Contains(searchTerm));
                            break;
                        case "lieu":
                            eventsQuery = eventsQuery.Where(e => e.Lieu.ToLower().Contains(searchTerm));
                            break;
                        default:
                            // Recherche globale (nom ou lieu)
                            eventsQuery = eventsQuery.Where(e =>
                                e.Titre.ToLower().Contains(searchTerm) ||
                                e.Lieu.ToLower().Contains(searchTerm));
                            break;
                    }
                }

                // Filtrage par date
                if (searchDate.HasValue)
                {
                    eventsQuery = eventsQuery.Where(e => e.DateEvent.Date == searchDate.Value.Date);
                }

                var events = await eventsQuery
                    .OrderByDescending(e => e.DateEvent)
                    .ToListAsync();

                // Passer les valeurs de recherche à la vue pour les conserver
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SearchType = searchType;
                ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");

                _logger.LogInformation("[EventController] Retrieved {Count} events with search filters", events.Count);
                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error in Index: {Message}", ex.Message);
                return View(new List<Event>());
            }
        }

        // GET: Event/Details/5
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var @event = await _context.Events
                    .Include(e => e.CreatedByUser)
                    .Include(e => e.Categorie)
                    .Include(e => e.Inscriptions)
                    .ThenInclude(i => i.User)
                    .Include(e => e.Statistiques)
                    .Include(e => e.Commentaires)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event == null)
                    return NotFound();

                if (@event.Statistiques == null)
                {
                    @event.Statistiques = new StatisticViewModel { EventId = @event.Id };
                    _context.Add(@event.Statistiques);
                }

                @event.Statistiques.TotalViews++;
                @event.Statistiques.LastUpdated = DateTime.Now;
                @event.EstTermine = @event.DateEvent < DateTime.Now;

                ViewBag.EstTermine = @event.EstTermine;
                ViewBag.TotalViews = @event.Statistiques.TotalViews;
                ViewBag.TotalInscriptions = @event.Inscriptions.Count;
                ViewBag.ConfirmedInscriptions = @event.Inscriptions.Count(i => i.Statut == "Confirmée");

                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.IsUserRegistered = currentUser != null && @event.Inscriptions.Any(i => i.UserId == currentUser.Id);
                ViewBag.IsEventCreator = currentUser != null && @event.CreatedById == currentUser.Id;
                ViewBag.IsAdmin = User.IsInRole("Admin");
                if (currentUser != null)
                {
                    var userInscription = @event.Inscriptions.FirstOrDefault(i => i.UserId == currentUser.Id);
                    ViewBag.UserRegistrationStatus = userInscription?.Statut;
                    ViewBag.UserRegistrationDate = userInscription?.DateInscription;
                }

                await _context.SaveChangesAsync();
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error in Details: {Message}", ex.Message);
                return NotFound();
            }
        }



        private async Task<string?> SaveEventImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "events");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/images/events/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error saving image: {Message}", ex.Message);
                return null;
            }
        }

        // GET: Event/Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error loading Create view: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire.";
                return RedirectToAction("Index");
            }
        }

        // POST: Event/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titre,Description,Lieu,DateEvent,Prix,CategorieId")] Event @event, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    @event.CreatedById = currentUser.Id;
                    @event.CreatedAt = DateTime.Now;
                    @event.EstTermine = false;
                    @event.EstAnnule = false;

                    // Save uploaded image if provided
                    if (imageFile != null)
                    {
                        @event.ImageUrl = await SaveEventImageAsync(imageFile);
                    }

                    _context.Add(@event);
                    await _context.SaveChangesAsync();

                    var stats = new StatisticViewModel
                    {
                        EventId = @event.Id,
                        TotalViews = 0,
                        TotalRegistrations = 0,
                        ConfirmedRegistrations = 0,
                        LastUpdated = DateTime.Now
                    };
                    _context.Add(stats);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[EventController] Event created: {EventId} by {UserId}", @event.Id, currentUser.Id);
                    TempData["SuccessMessage"] = "Événement créé avec succès.";
                    return RedirectToAction("Details", new { id = @event.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError("[EventController] Error creating event: {Message}", ex.Message);
                    TempData["ErrorMessage"] = "Erreur lors de la création de l'événement.";
                }
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(@event);
        }

        // GET: Event/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (@event.CreatedById != currentUser.Id && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("[EventController] Unauthorized edit attempt for event {EventId}", id);
                    return Forbid();
                }

                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error loading Edit view: {Message}", ex.Message);
                return NotFound();
            }
        }

        // POST: Event/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,Lieu,DateEvent,Prix,CategorieId")] Event @event, IFormFile? imageFile)
        {
            if (id != @event.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.FindAsync(id);
                    if (existingEvent == null)
                        return NotFound();

                    existingEvent.Titre = @event.Titre;
                    existingEvent.Description = @event.Description;
                    existingEvent.Lieu = @event.Lieu;
                    existingEvent.DateEvent = @event.DateEvent;
                    existingEvent.Prix = @event.Prix;
                    existingEvent.CategorieId = @event.CategorieId;
                    existingEvent.UpdatedAt = DateTime.Now;

                    // Save new image if uploaded, keep existing if not
                    if (imageFile != null)
                    {
                        existingEvent.ImageUrl = await SaveEventImageAsync(imageFile);
                    }

                    _context.Update(existingEvent);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[EventController] Event updated: {EventId}", id);
                    TempData["SuccessMessage"] = "Événement mis à jour avec succès.";
                    return RedirectToAction("Details", new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError("[EventController] Error updating event: {Message}", ex.Message);
                    TempData["ErrorMessage"] = "Erreur lors de la mise à jour.";
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(@event);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            try
            {
                var @event = await _context.Events
                    .Include(e => e.CreatedByUser)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event == null)
                    return NotFound();

                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error loading Delete view: {Message}", ex.Message);
                return NotFound();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var @event = await _context.Events
                    .Include(e => e.Statistiques)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event == null)
                    return NotFound();

                if (@event.Statistiques != null)
                    _context.Remove(@event.Statistiques);

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[EventController] Event deleted: {EventId}", id);
                TempData["SuccessMessage"] = "Événement supprimé avec succès.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error deleting event: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de la suppression.";
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: Event/Register
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int eventId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var @event = await _context.Events
                    .Include(e => e.Statistiques)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                    return NotFound();

                if (@event.EstAnnule)
                {
                    TempData["ErrorMessage"] = "Cet événement a été annulé.";
                    return RedirectToAction("Details", new { id = eventId });
                }

                var existingRegistration = await _context.Inscriptions
                    .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == currentUser.Id);

                if (existingRegistration != null)
                {
                    TempData["ErrorMessage"] = "Vous êtes déjà inscrit à cet événement.";
                    return RedirectToAction("Details", new { id = eventId });
                }

                var inscription = new Inscription
                {
                    EventId = eventId,
                    UserId = currentUser.Id,
                    DateInscription = DateTime.Now,
                    Statut = "En attente"
                };

                _context.Inscriptions.Add(inscription);

                if (@event.Statistiques != null)
                    @event.Statistiques.TotalRegistrations++;

                await _context.SaveChangesAsync();

                _logger.LogInformation("[EventController] User {UserId} registered for event {EventId}", currentUser.Id, eventId);
                TempData["SuccessMessage"] = "Inscription confirmée.";
                return RedirectToAction("Details", new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error registering: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de l'inscription.";
                return RedirectToAction("Details", new { id = eventId });
            }
        }

        // POST: Event/Unregister
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int eventId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var inscription = await _context.Inscriptions
                    .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == currentUser.Id);

                if (inscription != null)
                {
                    _context.Inscriptions.Remove(inscription);

                    var @event = await _context.Events
                        .Include(e => e.Statistiques)
                        .FirstOrDefaultAsync(e => e.Id == eventId);

                    if (@event?.Statistiques != null && @event.Statistiques.TotalRegistrations > 0)
                        @event.Statistiques.TotalRegistrations--;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("[EventController] User {UserId} unregistered from event {EventId}", currentUser.Id, eventId);
                    TempData["SuccessMessage"] = "Désinscription confirmée.";
                }

                return RedirectToAction("Details", new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error unregistering: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de la désinscription.";
                return RedirectToAction("Details", new { id = eventId });
            }
        }

        // GET: Event/Inscriptions/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Inscriptions(int? eventId)
        {
            if (eventId == null)
                return NotFound();

            try
            {
                var @event = await _context.Events
                    .Include(e => e.Inscriptions)
                    .ThenInclude(i => i.User)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                var isCreator = @event.CreatedById == currentUser.Id;
                var isAdmin = User.IsInRole("Admin");

                if (!isCreator && !isAdmin)
                {
                    _logger.LogWarning("[EventController] Unauthorized access to Inscriptions for event {EventId}", eventId);
                    return Forbid();
                }

                ViewBag.EventId = eventId;
                return View(@event.Inscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error in Inscriptions: {Message}", ex.Message);
                return NotFound();
            }
        }

        // POST: Event/ApproveRegistration
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRegistration(int inscriptionId)
        {
            try
            {
                var inscription = await _context.Inscriptions
                    .Include(i => i.Event)
                    .ThenInclude(e => e.Statistiques)
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == inscriptionId);

                if (inscription == null)
                    return NotFound();

                inscription.Statut = "Confirmée";

                if (inscription.Event?.Statistiques != null)
                    inscription.Event.Statistiques.ConfirmedRegistrations++;

                _context.Update(inscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[EventController] Registration approved: {InscriptionId}", inscriptionId);
                // Create an in-app notification for the user
                try
                {
                    if (inscription.User != null && inscription.Event != null)
                    {
                        var notif = new Notification
                        {
                            UserId = inscription.UserId,
                            Titre = "Inscription confirmée",
                            Message = $"Votre inscription à \"{inscription.Event.Titre}\" a été confirmée.",
                            Type = "success",
                            Lien = Url.Action("Details", "Event", new { id = inscription.EventId })
                        };
                        _context.Notifications.Add(notif);
                        await _context.SaveChangesAsync();

                        // Try sending confirmation email as well
                        try
                        {
                            await _emailService.SendConfirmationEmailAsync(
                                inscription.User.Email,
                                $"{inscription.User.FirstName} {inscription.User.LastName}",
                                inscription.Event.Titre,
                                inscription.Event.DateEvent.ToString("dd/MM/yyyy HH:mm"),
                                inscription.Event.Lieu
                            );
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning("[EventController] Email sending failed on approve but notification saved: {Message}", emailEx.Message);
                        }
                    }
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning("[EventController] Failed to create notification for approved registration: {Message}", notifEx.Message);
                }

                TempData["SuccessMessage"] = "Inscription approuvée.";
                return RedirectToAction("Inscriptions", new { eventId = inscription.EventId });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error approving registration: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de l'approbation.";
                return RedirectToAction("Index");
            }
        }

        // POST: Event/UpdateInscriptionStatus
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInscriptionStatus([FromBody] UpdateInscriptionStatusRequest request)
        {
            try
            {
                if (request == null || request.InscriptionId <= 0 || string.IsNullOrEmpty(request.NewStatus))
                {
                    return Json(new { success = false, message = "Données invalides." });
                }

                var inscription = await _context.Inscriptions
                    .Include(i => i.Event)
                    .ThenInclude(e => e.Statistiques)
                    .Include(i => i.User)
                    .FirstOrDefaultAsync(i => i.Id == request.InscriptionId);

                if (inscription == null)
                {
                    return Json(new { success = false, message = "Inscription non trouvée." });
                }

                var oldStatus = inscription.Statut;
                inscription.Statut = request.NewStatus;

                // Update statistics
                if (oldStatus == "Confirmée" && request.NewStatus != "Confirmée" && inscription.Event?.Statistiques != null)
                    inscription.Event.Statistiques.ConfirmedRegistrations--;

                if (oldStatus != "Confirmée" && request.NewStatus == "Confirmée" && inscription.Event?.Statistiques != null)
                    inscription.Event.Statistiques.ConfirmedRegistrations++;

                _context.Update(inscription);
                await _context.SaveChangesAsync();

                // Send email based on new status
                if (inscription.User != null && inscription.Event != null)
                {
                    try
                    {
                        if (request.NewStatus == "Confirmée")
                        {
                            await _emailService.SendConfirmationEmailAsync(
                                inscription.User.Email,
                                $"{inscription.User.FirstName} {inscription.User.LastName}",
                                inscription.Event.Titre,
                                inscription.Event.DateEvent.ToString("dd/MM/yyyy HH:mm"),
                                inscription.Event.Lieu
                            );
                        }
                        else if (request.NewStatus == "Rejetée")
                        {
                            await _emailService.SendRejectionEmailAsync(
                                inscription.User.Email,
                                $"{inscription.User.FirstName} {inscription.User.LastName}",
                                inscription.Event.Titre,
                                "Votre inscription n'a pas pu être acceptée."
                            );
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning("[EventController] Email sending failed but status was updated: {Message}", emailEx.Message);
                        // Continue even if email fails - status update is more important
                    }
                    // Create an in-app notification for the user about the status change
                    try
                    {
                        var notifType = request.NewStatus == "Confirmée" ? "success" : request.NewStatus == "Rejetée" ? "error" : "info";
                        var notif = new Notification
                        {
                            UserId = inscription.UserId,
                            Titre = $"Statut d'inscription : {request.NewStatus}",
                            Message = $"Le statut de votre inscription à \"{inscription.Event.Titre}\" a été mis à jour : {request.NewStatus}.",
                            Type = notifType,
                            Lien = Url.Action("Details", "Event", new { id = inscription.EventId })
                        };
                        _context.Notifications.Add(notif);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning("[EventController] Failed to create notification after status update: {Message}", notifEx.Message);
                    }
                }

                _logger.LogInformation("[EventController] Inscription {InscriptionId} status updated to {NewStatus}", request.InscriptionId, request.NewStatus);
                return Json(new { success = true, message = "Statut mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error updating inscription status: {Message}", ex.Message);
                return Json(new { success = false, message = "Erreur lors de la mise à jour." });
            }
        }

        // POST: Event/Cancel
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, [FromForm] string? raison)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                    return NotFound();

                @event.EstAnnule = true;
                @event.RaisonAnnulation = raison ?? "Non spécifiée";
                @event.DateAnnulation = DateTime.Now;

                _context.Update(@event);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[EventController] Event cancelled: {EventId}", id);
                TempData["SuccessMessage"] = "Événement annulé.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error cancelling event: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de l'annulation.";
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: Event/PostComment - Add new comment to event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment(int eventId, [FromForm] string Texte, [FromForm] int note)
        {
            try
            {
                Console.WriteLine($"[v0] PostComment called - EventId: {eventId}, Texte: {Texte}, Note: {note}");

                if (string.IsNullOrWhiteSpace(Texte) || note < 1 || note > 5)
                {
                    Console.WriteLine("[v0] Validation failed");
                    TempData["ErrorMessage"] = "Veuillez remplir tous les champs correctement.";
                    return RedirectToAction("Details", new { id = eventId });
                }

                var @event = await _context.Events.FindAsync(eventId);
                if (@event == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                Console.WriteLine($"[v0] Current user: {currentUser?.Id}");

                var existingComment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.UserId == currentUser.Id && c.EventId == eventId);

                if (existingComment != null)
                {
                    Console.WriteLine("[v0] Updating existing comment");
                    existingComment.Texte = Texte;
                    existingComment.Note = note;
                    existingComment.DateModification = DateTime.Now;
                    _context.Update(existingComment);
                }
                else
                {
                    Console.WriteLine("[v0] Creating new comment");
                    var isParticipant = await _context.Inscriptions
                        .AnyAsync(i => i.UserId == currentUser.Id && i.EventId == eventId);

                    var comment = new Comment
                    {
                        EventId = eventId,
                        UserId = currentUser.Id,
                        Texte = Texte,
                        Note = note,
                        DateCreation = DateTime.Now,
                        UtilisateurAParticipe = isParticipant
                    };

                    _context.Comments.Add(comment);
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("[v0] Comment saved successfully");

                _logger.LogInformation("[EventController] Comment posted by {UserId} on event {EventId}", currentUser.Id, eventId);
                TempData["SuccessMessage"] = "Votre avis a été enregistré avec succès.";
                return RedirectToAction("Details", new { id = eventId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error: {ex.Message}");
                _logger.LogError("[EventController] Error posting comment: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de l'enregistrement du commentaire.";
                return RedirectToAction("Details", new { id = eventId });
            }
        }





        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int eventId)
        {
            try
            {
                var comment = await _context.Comments.FindAsync(commentId);
                if (comment == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                if (comment.UserId != currentUser.Id && !isAdmin)
                {
                    _logger.LogWarning("[EventController] Unauthorized comment deletion attempt by {UserId}", currentUser.Id);
                    return Forbid();
                }

                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[EventController] Comment deleted: {CommentId}", commentId);
                TempData["SuccessMessage"] = "Commentaire supprimé avec succès.";
                return RedirectToAction("Details", new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError("[EventController] Error deleting comment: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Erreur lors de la suppression du commentaire.";
                return RedirectToAction("Details", new { id = eventId });
            }
        }

    }
}