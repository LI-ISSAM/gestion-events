using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using GestionEvenements.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionEvenements.Data
{
    public class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var context = services.GetRequiredService<AppDbContext>();

            var roles = new[] { "Admin", "User", "Moderator" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@gestionevenements.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            var desiredAdminPassword = "Password123!";

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "System",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now,
                    Bio = "Administrateur système pour GestionEvenements",
                    Ville = "Casablanca",
                    Pays = "Maroc",
                    DerniereConnexion = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, desiredAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                var needsUpdate = false;
                if (adminUser.UserName != adminEmail)
                {
                    adminUser.UserName = adminEmail;
                    needsUpdate = true;
                }

                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await userManager.UpdateAsync(adminUser);
                }

                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                if (!await userManager.CheckPasswordAsync(adminUser, desiredAdminPassword))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    await userManager.ResetPasswordAsync(adminUser, token, desiredAdminPassword);
                }
            }

            var sampleUsers = new List<(string email, string firstName, string lastName)>
            {
                ("user1@example.com", "Ahmed", "Bennani"),
                ("user2@example.com", "Fatima", "Alaoui"),
                ("user3@example.com", "Mohammed", "Hassan")
            };

            foreach (var (email, firstName, lastName) in sampleUsers)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.Now,
                        DerniereConnexion = DateTime.Now,
                        Ville = "Casablanca"
                    };

                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }

            if (!context.Categories.Any())
            {
                var categories = new List<EventCategory>
                {
                    new EventCategory
                    {
                        Nom = "Conférence",
                        Description = "Événements de conférence et séminaires",
                        Couleur = "#3b82f6",
                        Icone = "fas fa-microphone"
                    },
                    new EventCategory
                    {
                        Nom = "Concert",
                        Description = "Événements musicaux et performances",
                        Couleur = "#8b5cf6",
                        Icone = "fas fa-music"
                    },
                    new EventCategory
                    {
                        Nom = "Sport",
                        Description = "Événements sportifs et compétitions",
                        Couleur = "#ef4444",
                        Icone = "fas fa-dumbbell"
                    },
                    new EventCategory
                    {
                        Nom = "Atelier",
                        Description = "Ateliers pratiques et formations",
                        Couleur = "#10b981",
                        Icone = "fas fa-wrench"
                    },
                    new EventCategory
                    {
                        Nom = "Networking",
                        Description = "Événements de réseautage et connexions",
                        Couleur = "#f59e0b",
                        Icone = "fas fa-handshake"
                    },
                    new EventCategory
                    {
                        Nom = "Social",
                        Description = "Événements sociaux et rencontres",
                        Couleur = "#ec4899",
                        Icone = "fas fa-users"
                    }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Events.Any())
            {
                var adminUserForEvents = await userManager.FindByEmailAsync(adminEmail);
                var categories = context.Categories.ToList();

                var events = new List<Event>
                {
                    new Event
                    {
                        Titre = "Conférence IA & Machine Learning 2025",
                        Description = "Une conférence complète sur les dernières tendances en Intelligence Artificielle et Machine Learning. Découvrez les applications pratiques et les cas d'usage réels.",
                        Lieu = "Centre de Conférences Casablanca",
                        DateEvent = DateTime.Now.AddDays(15).AddHours(9),
                        Prix = 299m,
                        CategorieId = categories.FirstOrDefault(static c => c.Nom == "Conférence")?.Id ?? 1,
                        CreatedById = adminUserForEvents?.Id,
                        CreatedAt = DateTime.Now,
                        ImageUrl = "/images/events/conference.jpg",
                        EstTermine = false,
                        EstAnnule = false
                    },
                    new Event
                    {
                        Titre = "Concert Jazz - Jazz en Terrasse",
                        Description = "Une soirée magique avec les meilleurs musiciens de jazz de la région. Ambiance décontractée, bonne musique et délicieux cocktails.",
                        Lieu = "Terrasse Vue Mer, Casablanca",
                        DateEvent = DateTime.Now.AddDays(8).AddHours(19),
                        Prix = 150m,
                        CategorieId = categories.FirstOrDefault(static c => c.Nom == "Concert")?.Id ?? 2,
                        CreatedById = adminUserForEvents?.Id,
                        CreatedAt = DateTime.Now,
                        ImageUrl = "/images/events/concert.jpg",
                        EstTermine = false,
                        EstAnnule = false
                    },
                    new Event
                    {
                        Titre = "Marathon Sportif Casablanca 2025",
                        Description = "Rejoignez notre marathon annuel ! 42km à travers les plus beaux quartiers de Casablanca. Pour tous les niveaux.",
                        Lieu = "Stade Municipale, Casablanca",
                        DateEvent = DateTime.Now.AddDays(30).AddHours(6),
                        Prix = 75m,
                        CategorieId = categories.FirstOrDefault(static c => c.Nom == "Sport")?.Id ?? 3,
                        CreatedById = adminUserForEvents?.Id,
                        CreatedAt = DateTime.Now,
                        ImageUrl = "/images/events/marathon.jpg",
                        EstTermine = false,
                        EstAnnule = false
                    },
                    new Event
                    {
                        Titre = "Atelier: Web Development avec ASP.NET Core",
                        Description = "Apprenez à développer des applications web modernes avec ASP.NET Core 8. Inclus: Entity Framework, Identity, et déploiement.",
                        Lieu = "Tech Hub Casablanca",
                        DateEvent = DateTime.Now.AddDays(20).AddHours(10),
                        Prix = 199m,
                        CategorieId = categories.FirstOrDefault(static c => c.Nom == "Atelier")?.Id ?? 4,
                        CreatedById = adminUserForEvents?.Id,
                        CreatedAt = DateTime.Now,
                        ImageUrl = "/images/events/workshop.jpg",
                        EstTermine = false,
                        EstAnnule = false
                    }
                };

                context.Events.AddRange(events);
                await context.SaveChangesAsync();

                foreach (var @event in events)
                {
                    var stats = new StatisticViewModel
                    {
                        EventId = @event.Id,
                        TotalViews = 0,
                        TotalRegistrations = 0,
                        ConfirmedRegistrations = 0,
                        TotalComments = 0,
                        AverageRating = 0,
                        LastUpdated = DateTime.Now
                    };

                    context.EventStatistics.Add(stats);
                }

                await context.SaveChangesAsync();
            }

            await context.SaveChangesAsync();
        }
    }
}
