using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodSafetyTracker.Domain.Entities;
using Bogus;

namespace FoodSafetyTracker.Web.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create database if it doesn't exist
            await context.Database.EnsureCreatedAsync();

            // Create roles
            string[] roles = { "Admin", "Inspector", "Viewer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create users
            var adminEmail = "admin@foodsafety.gov";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            var inspectorEmail = "inspector@foodsafety.gov";
            if (await userManager.FindByEmailAsync(inspectorEmail) == null)
            {
                var inspectorUser = new IdentityUser
                {
                    UserName = inspectorEmail,
                    Email = inspectorEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(inspectorUser, "Inspector@123");
                await userManager.AddToRoleAsync(inspectorUser, "Inspector");
            }

            var viewerEmail = "viewer@foodsafety.gov";
            if (await userManager.FindByEmailAsync(viewerEmail) == null)
            {
                var viewerUser = new IdentityUser
                {
                    UserName = viewerEmail,
                    Email = viewerEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(viewerUser, "Viewer@123");
                await userManager.AddToRoleAsync(viewerUser, "Viewer");
            }

            // Check if we already have data
            if (context.Premises.Any()) return;

            // Generate Premises
            var towns = new[] { "Dublin City", "Cork City", "Galway City" };
            var riskLevels = new[] { "Low", "Medium", "High" };

            var premisesFaker = new Faker<Premises>()
                .RuleFor(p => p.Name, f => f.Company.CompanyName())
                .RuleFor(p => p.Address, f => f.Address.StreetAddress())
                .RuleFor(p => p.Town, f => f.PickRandom(towns))
                .RuleFor(p => p.RiskRating, f => f.PickRandom(riskLevels));

            var premises = premisesFaker.Generate(12);
            await context.Premises.AddRangeAsync(premises);
            await context.SaveChangesAsync();

            // Generate Inspections
            var inspectionFaker = new Faker<Inspection>()
                .RuleFor(i => i.PremisesId, f => f.PickRandom(premises).Id)
                .RuleFor(i => i.InspectionDate, f => f.Date.Past(90))
                .RuleFor(i => i.Score, f => f.Random.Int(0, 100))
                .RuleFor(i => i.Outcome, (f, i) => i.Score >= 60 ? "Pass" : "Fail")
                .RuleFor(i => i.Notes, (f, i) => GetInspectionNotes(i.Outcome, i.Score)); 

            var inspections = inspectionFaker.Generate(25);
            await context.Inspections.AddRangeAsync(inspections);
            await context.SaveChangesAsync();

            // Generate FollowUps
            var followUpFaker = new Faker<FollowUp>()
                .RuleFor(f => f.InspectionId, f => f.PickRandom(inspections.Where(i => i.Outcome == "Fail")).Id)
                .RuleFor(f => f.DueDate, (f, follow) =>
                {
                    var inspection = inspections.First(i => i.Id == follow.InspectionId);
                    return inspection.InspectionDate.AddDays(f.Random.Int(14, 60));
                })
                .RuleFor(f => f.Status, f => f.PickRandom(new[] { "Open", "Closed" }))
                .RuleFor(f => f.ClosedDate, (f, follow) =>
                    follow.Status == "Closed" ? follow.DueDate.AddDays(f.Random.Int(-10, 10)) : (DateTime?)null);

            var followUps = followUpFaker.Generate(10);
            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }
        private static string GetInspectionNotes(string outcome, int score)
        {
            var passNotes = new[]
            {
                "All food handling practices compliant. Temperature controls within limits. Staff hygiene excellent.",
                "Good hygiene standards observed. Proper storage of raw and cooked foods.",
                "Satisfactory inspection. Minor improvement suggested for record keeping.",
                "Clean premises, proper waste management. No critical violations.",
                "Food safety management system in place. Staff trained adequately."
            };

            var failNotes = new[]
            {
                "CRITICAL: Raw meat stored above ready-to-eat foods. Immediate corrective action required.",
                "Poor handwashing practices observed. No soap in staff washroom.",
                "Pest activity detected in storage area. Professional pest control required.",
                "Food temperatures above safe limits. Refrigeration unit not functioning properly.",
                "Lack of food safety training records. No allergen information displayed.",
                "Dirty kitchen surfaces. Food debris accumulation under equipment.",
                "Expired food products found in storage. Poor stock rotation."
            };

            var borderNotes = new[]
            {
                "Improvement needed in cleaning schedule. Several areas require attention.",
                "Temperature logs incomplete for the past week. Reminded staff to maintain records.",
                "Minor issues with waste disposal. Advised to increase collection frequency.",
                "Some food containers not properly labeled with date. Advised to implement FIFO system.",
                "Handwashing sink obstructed. Cleared during inspection."
            };

            if (outcome == "Pass" && score >= 85)
            {
                return passNotes[new Random().Next(passNotes.Length)];
            }
            else if (outcome == "Pass" && score < 85)
            {
                return borderNotes[new Random().Next(borderNotes.Length)];
            }
            else
            {
                return failNotes[new Random().Next(failNotes.Length)];
            }
        }
    }
}