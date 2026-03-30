using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FoodSafetyTracker.Web.Data;
using FoodSafetyTracker.Domain.Entities;

namespace FoodSafetyTracker.Web.Controllers
{
    [Authorize]
    public class InspectionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InspectionsController> _logger;
        public InspectionsController(ApplicationDbContext context, ILogger<InspectionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Inspections
        // The Index method was missing in your code!
        public async Task<IActionResult> Index()
        {
            var inspections = await _context.Inspections
                .Include(i => i.Premises)
                .ToListAsync();

            // Log when viewing the list
            _logger.LogInformation("Inspections list viewed by {User}", User.Identity?.Name ?? "Anonymous");

            return View(inspections);
        }

        // GET: Inspections/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inspection == null)
            {
                return NotFound();
            }

            return View(inspection);
        }

        // GET: Inspections/Create
        [Authorize(Roles = "Admin,Inspector")]
        public async Task<IActionResult> Create()
        {
            ViewData["PremisesId"] = new SelectList(await _context.Premises.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Inspections/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Inspector")] // Add authorization attribute
        public async Task<IActionResult> Create([Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inspection);
                await _context.SaveChangesAsync();

                //  Log the creation
                _logger.LogInformation("Inspection created: {@Inspection}", new
                {
                    InspectionId = inspection.Id,
                    PremisesId = inspection.PremisesId,
                    Score = inspection.Score,
                    Outcome = inspection.Outcome,
                    User = User.Identity?.Name ?? "Unknown"
                });

                return RedirectToAction(nameof(Index));
            }

            // Log validation warning
            _logger.LogWarning("Inspection creation failed validation: {@ModelErrors}",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

            ViewData["PremisesId"] = new SelectList(_context.Premises, "Id", "Address", inspection.PremisesId);
            return View(inspection);
        }

        // GET: Inspections/Edit/5
        [Authorize(Roles = "Admin,Inspector")] // Add authorization attribute
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inspection = await _context.Inspections
                .Include(i => i.Premises) // Include Premises for better logging
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inspection == null)
            {
                return NotFound();
            }

            // Use "Name" instead of "Address" for better display
            ViewData["PremisesId"] = new SelectList(_context.Premises, "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }

        // POST: Inspections/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Inspector")] // Add authorization attribute
        public async Task<IActionResult> Edit(int id, [Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
        {
            if (id != inspection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get original values before update for logging
                    var originalInspection = await _context.Inspections.AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Id == id);

                    _context.Update(inspection);
                    await _context.SaveChangesAsync();

                    // Log the edit with changes
                    _logger.LogInformation("Inspection updated: {@InspectionChanges}", new
                    {
                        InspectionId = inspection.Id,
                        PremisesId = inspection.PremisesId,
                        OldScore = originalInspection?.Score,
                        NewScore = inspection.Score,
                        OldOutcome = originalInspection?.Outcome,
                        NewOutcome = inspection.Outcome,
                        OldInspectionDate = originalInspection?.InspectionDate,
                        NewInspectionDate = inspection.InspectionDate,
                        User = User.Identity?.Name ?? "Unknown"
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InspectionExists(inspection.Id))
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

            // Log validation warning
            _logger.LogWarning("Inspection edit failed validation: InspectionId {InspectionId}, User {User}",
                inspection.Id, User.Identity?.Name ?? "Unknown");

            ViewData["PremisesId"] = new SelectList(_context.Premises, "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }

        // GET: Inspections/Delete/5
        [Authorize(Roles = "Admin")] // Only Admin can delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inspection == null)
            {
                return NotFound();
            }

            return View(inspection);
        }

        // POST: Inspections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Only Admin can delete
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Load the Inspection with related FollowUps
                var inspection = await _context.Inspections
                    .Include(i => i.FollowUps)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (inspection == null)
                {
                    _logger.LogWarning("Delete failed: Inspection {Id} not found", id);
                    return NotFound();
                }

                // Log before deleting
                _logger.LogInformation("Attempting to delete Inspection {Id} with {FollowUpCount} follow-ups by {User}",
                    id, inspection.FollowUps?.Count ?? 0, User.Identity?.Name ?? "Unknown");

                // Remove related FollowUps first
                if (inspection.FollowUps != null && inspection.FollowUps.Any())
                {
                    _context.FollowUps.RemoveRange(inspection.FollowUps);
                    _logger.LogInformation("Removed {Count} follow-ups for Inspection {Id}",
                        inspection.FollowUps.Count, id);
                }

                // Remove the Inspection
                _context.Inspections.Remove(inspection);
                await _context.SaveChangesAsync();

                // Log success
                _logger.LogInformation("Inspection deleted successfully: {@InspectionDeleted}", new
                {
                    InspectionId = id,
                    PremisesId = inspection.PremisesId,
                    Score = inspection.Score,
                    Outcome = inspection.Outcome,
                    User = User.Identity?.Name ?? "Unknown"
                });

                TempData["Success"] = "Inspection deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency - check if inspection still exists
                if (!InspectionExists(id))
                {
                    _logger.LogWarning("Delete failed: Inspection {Id} was already deleted", id);
                    return NotFound();
                }

                _logger.LogError(ex, "Concurrency error deleting Inspection {Id} by {User}",
                    id, User.Identity?.Name ?? "Unknown");

                TempData["Error"] = "The inspection was modified by another user. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Inspection {Id} by {User}",
                    id, User.Identity?.Name ?? "Unknown");

                TempData["Error"] = "An error occurred while deleting the inspection.";
                return RedirectToAction(nameof(Index));
            }

        }
    private bool InspectionExists(int id)
        {
            return _context.Inspections.Any(e => e.Id == id);
        }
    }
}

