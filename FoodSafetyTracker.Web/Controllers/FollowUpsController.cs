using FoodSafetyTracker.Web.Data;
using FoodSafetyTracker.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyTracker.Web.Controllers
{
    [Authorize] // Require authentication for all actions
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FollowUpsController> _logger; // Logger

        public FollowUpsController(ApplicationDbContext context, ILogger<FollowUpsController> logger) // Add logger parameter
        {
            _context = context;
            _logger = logger; // THIS
        }

        // GET: FollowUps
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i.Premises); // Include premises for better display

            // Log when viewing the list
            _logger.LogInformation("Follow-ups list viewed by {User}", User.Identity?.Name ?? "Anonymous");

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: FollowUps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var followUp = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i.Premises) // Include premises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (followUp == null)
            {
                return NotFound();
            }

            return View(followUp);
        }

        // GET: FollowUps/Create
        [Authorize(Roles = "Admin,Inspector")] // Only Admin and Inspector can create
        public IActionResult Create()
        {
            // Show better dropdown with premises name
            var inspections = _context.Inspections
                .Include(i => i.Premises)
                .Select(i => new {
                    i.Id,
                    DisplayText = $"{i.Premises.Name} - {i.InspectionDate:d} - Score: {i.Score}"
                })
                .ToList();

            ViewData["InspectionId"] = new SelectList(inspections, "Id", "DisplayText");
            return View();
        }

        // POST: FollowUps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Inspector")] // Authorization
        public async Task<IActionResult> Create([Bind("Id,InspectionId,DueDate,Status,ClosedDate")] FollowUp followUp)
        {
            // Get the inspection for validation
            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .FirstOrDefaultAsync(i => i.Id == followUp.InspectionId);

            // Business rule: Due date should not be before inspection date
            if (inspection != null && followUp.DueDate < inspection.InspectionDate)
            {
                ModelState.AddModelError("DueDate", "Due date cannot be before the inspection date");

                _logger.LogWarning("Business rule violation: FollowUp due date {DueDate} is before inspection date {InspectionDate}. Premises: {PremisesName}, User: {User}",
                    followUp.DueDate, inspection.InspectionDate, inspection.Premises?.Name ?? "Unknown",
                    User.Identity?.Name ?? "Unknown");
            }

            // Business rule: Cannot close without ClosedDate
            if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
            {
                ModelState.AddModelError("ClosedDate", "Closed date is required when status is Closed");

                _logger.LogWarning("Business rule violation: Attempted to create FollowUp with Closed status but no ClosedDate. User: {User}",
                    User.Identity?.Name ?? "Unknown");
            }

            // Business rule: Only create follow-ups for failed inspections
            if (inspection != null && inspection.Outcome != "Fail")
            {
                ModelState.AddModelError("InspectionId", "Follow-ups can only be created for failed inspections");

                _logger.LogWarning("Business rule violation: Attempted to create FollowUp for non-failed inspection {InspectionId}. User: {User}",
                    followUp.InspectionId, User.Identity?.Name ?? "Unknown");
            }

            if (ModelState.IsValid)
            {
                _context.Add(followUp);
                await _context.SaveChangesAsync();

                // Log the creation
                _logger.LogInformation("FollowUp created: {@FollowUp}", new
                {
                    FollowUpId = followUp.Id,
                    InspectionId = followUp.InspectionId,
                    DueDate = followUp.DueDate,
                    Status = followUp.Status,
                    ClosedDate = followUp.ClosedDate,
                    User = User.Identity?.Name ?? "Unknown"
                });

                return RedirectToAction(nameof(Index));
            }

            // Log validation warning
            _logger.LogWarning("FollowUp creation failed validation: {@ModelErrors}",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

            // Repopulate dropdown with the same data
            var inspectionsList = _context.Inspections
                .Include(i => i.Premises)
                .Select(i => new {
                    i.Id,
                    DisplayText = $"{i.Premises.Name} - {i.InspectionDate:d} - Score: {i.Score}"
                })
                .ToList();
            ViewData["InspectionId"] = new SelectList(inspectionsList, "Id", "DisplayText", followUp.InspectionId);

            return View(followUp);
        }

        // GET: FollowUps/Edit/5
        [Authorize(Roles = "Admin,Inspector")] // Authorization
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var followUp = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i.Premises) // Include for display
                .FirstOrDefaultAsync(m => m.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            // Show better dropdown
            var inspections = _context.Inspections
                .Include(i => i.Premises)
                .Select(i => new {
                    i.Id,
                    DisplayText = $"{i.Premises.Name} - {i.InspectionDate:d} - Score: {i.Score}"
                })
                .ToList();

            ViewData["InspectionId"] = new SelectList(inspections, "Id", "DisplayText", followUp.InspectionId);
            return View(followUp);
        }

        // POST: FollowUps/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Inspector")] // Authorization
        public async Task<IActionResult> Edit(int id, [Bind("Id,InspectionId,DueDate,Status,ClosedDate")] FollowUp followUp)
        {
            if (id != followUp.Id)
            {
                return NotFound();
            }

            // Get the original values for logging
            var originalFollowUp = await _context.FollowUps.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            // Get inspection for validation
            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .FirstOrDefaultAsync(i => i.Id == followUp.InspectionId);

            // Business rule validation
            if (inspection != null && followUp.DueDate < inspection.InspectionDate)
            {
                ModelState.AddModelError("DueDate", "Due date cannot be before the inspection date");

                _logger.LogWarning("Business rule violation: FollowUp due date {DueDate} is before inspection date {InspectionDate}. User: {User}",
                    followUp.DueDate, inspection.InspectionDate, User.Identity?.Name ?? "Unknown");
            }

            // Business rule: Cannot close without ClosedDate
            if (followUp.Status == "Closed" && !followUp.ClosedDate.HasValue)
            {
                ModelState.AddModelError("ClosedDate", "Closed date is required when status is Closed");

                _logger.LogWarning("Business rule violation: Attempted to close FollowUp {FollowUpId} without ClosedDate. User: {User}",
                    followUp.Id, User.Identity?.Name ?? "Unknown");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(followUp);
                    await _context.SaveChangesAsync();

                    // Log the edit with changes
                    _logger.LogInformation("FollowUp updated: {@FollowUpChanges}", new
                    {
                        FollowUpId = followUp.Id,
                        OldStatus = originalFollowUp?.Status,
                        NewStatus = followUp.Status,
                        OldDueDate = originalFollowUp?.DueDate,
                        NewDueDate = followUp.DueDate,
                        OldClosedDate = originalFollowUp?.ClosedDate,
                        NewClosedDate = followUp.ClosedDate,
                        User = User.Identity?.Name ?? "Unknown"
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FollowUpExists(followUp.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // ADD THIS - Log validation warning
            _logger.LogWarning("FollowUp edit failed validation: FollowUpId {FollowUpId}, User {User}",
                followUp.Id, User.Identity?.Name ?? "Unknown");

            // Repopulate dropdown
            var inspectionsList = _context.Inspections
                .Include(i => i.Premises)
                .Select(i => new {
                    i.Id,
                    DisplayText = $"{i.Premises.Name} - {i.InspectionDate:d} - Score: {i.Score}"
                })
                .ToList();
            ViewData["InspectionId"] = new SelectList(inspectionsList, "Id", "DisplayText", followUp.InspectionId);

            return View(followUp);
        }

        // GET: FollowUps/Delete/5
        [Authorize(Roles = "Admin")] // Only Admin can delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var followUp = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i.Premises) // Include for display
                .FirstOrDefaultAsync(m => m.Id == id);
            if (followUp == null)
            {
                return NotFound();
            }

            return View(followUp);
        }

        // POST: FollowUps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] //  Only Admin can delete
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp != null)
            {
                _context.FollowUps.Remove(followUp);

                // Log deletion
                _logger.LogWarning("FollowUp deleted: {@FollowUpDeleted}", new
                {
                    FollowUpId = id,
                    InspectionId = followUp.InspectionId,
                    DueDate = followUp.DueDate,
                    Status = followUp.Status,
                    User = User.Identity?.Name ?? "Unknown"
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FollowUpExists(int id)
        {
            return _context.FollowUps.Any(e => e.Id == id);
        }
    }
}