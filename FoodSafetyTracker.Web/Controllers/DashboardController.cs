using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodSafetyTracker.Web.Data;

namespace FoodSafetyTracker.Web.Controllers
{
    [Authorize] // All authenticated users can view dashboard
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string town, string riskRating)
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // Log dashboard access
            _logger.LogInformation("Dashboard accessed by {User}. Filters - Town: {Town}, RiskRating: {RiskRating}",
                User.Identity?.Name ?? "Anonymous", town ?? "None", riskRating ?? "None");

            // Base queries
            var inspectionsQuery = _context.Inspections.AsNoTracking().AsQueryable();
            var followUpsQuery = _context.FollowUps.AsNoTracking().AsQueryable();

            // Apply filters to inspections for counts
            if (!string.IsNullOrEmpty(town))
            {
                inspectionsQuery = inspectionsQuery.Where(i => i.Premises != null && i.Premises.Town == town);
            }
            if (!string.IsNullOrEmpty(riskRating))
            {
                inspectionsQuery = inspectionsQuery.Where(i => i.Premises != null && i.Premises.RiskRating == riskRating);
            }

            // Get the filtered inspection IDs for follow-up filtering
            var filteredInspectionIds = await inspectionsQuery.Select(i => i.Id).ToListAsync();

            // Dashboard counts
            var inspectionsThisMonth = await inspectionsQuery
                .Where(i => i.InspectionDate >= firstDayOfMonth && i.InspectionDate <= today)
                .CountAsync();

            var failedInspectionsThisMonth = await inspectionsQuery
                .Where(i => i.InspectionDate >= firstDayOfMonth && i.InspectionDate <= today && i.Outcome == "Fail")
                .CountAsync();

            var overdueFollowUps = await followUpsQuery
                .Where(f => filteredInspectionIds.Contains(f.InspectionId)
                            && f.Status == "Open"
                            && f.DueDate < today)
                .CountAsync();

            // Log warning if there are overdue follow-ups
            if (overdueFollowUps > 0)
            {
                _logger.LogWarning("There are {OverdueCount} overdue follow-ups. User: {User}",
                    overdueFollowUps, User.Identity?.Name ?? "Unknown");
            }

            // Get distinct towns for filter dropdown
            var towns = await _context.Premises
                .Select(p => p.Town)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            var riskRatings = new[] { "Low", "Medium", "High" };

            ViewBag.Towns = towns;
            ViewBag.RiskRatings = riskRatings;
            ViewBag.SelectedTown = town;
            ViewBag.SelectedRisk = riskRating;

            var model = new DashboardViewModel
            {
                InspectionsThisMonth = inspectionsThisMonth,
                FailedInspectionsThisMonth = failedInspectionsThisMonth,
                OverdueFollowUps = overdueFollowUps,
                TotalPremises = await _context.Premises.AsNoTracking().CountAsync(),
                TotalInspections = await inspectionsQuery.CountAsync(),
                AverageScore = await inspectionsQuery.AverageAsync(i => (double?)i.Score) ?? 0
            };

            return View(model);
        }
    }

    public class DashboardViewModel
    {
        public int InspectionsThisMonth { get; set; }
        public int FailedInspectionsThisMonth { get; set; }
        public int OverdueFollowUps { get; set; }
        public int TotalPremises { get; set; }
        public int TotalInspections { get; set; }
        public double AverageScore { get; set; }
    }
}