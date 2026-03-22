using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FoodSafetyTracker.Web.Data;
using FoodSafetyTracker.Web.Models;

namespace FoodSafetyTracker.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PremisesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PremisesController> _logger;

        public PremisesController(ApplicationDbContext context, ILogger<PremisesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Premises
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Premises list viewed by {User}", User.Identity?.Name ?? "Anonymous");
            return View(await _context.Premises.ToListAsync());
        }

        // GET: Premises/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var premises = await _context.Premises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (premises == null)
            {
                return NotFound();
            }

            return View(premises);
        }

        // GET: Premises/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Premises/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Address,Town,RiskRating")] Premises premises)
        {
            if (ModelState.IsValid)
            {
                _context.Add(premises);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Premises created: {@Premises}", new
                {
                    PremisesId = premises.Id,
                    Name = premises.Name,
                    Town = premises.Town,
                    RiskRating = premises.RiskRating,
                    User = User.Identity?.Name ?? "Unknown"
                });

                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("Premises creation failed validation: {@ModelErrors}",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

            return View(premises);
        }

        // GET: Premises/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var premises = await _context.Premises.FindAsync(id);
            if (premises == null)
            {
                return NotFound();
            }
            return View(premises);
        }

        // POST: Premises/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Town,RiskRating")] Premises premises)
        {
            if (id != premises.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalPremises = await _context.Premises.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id);

                    _context.Update(premises);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Premises updated: {@PremisesChanges}", new
                    {
                        PremisesId = premises.Id,
                        OldName = originalPremises?.Name,
                        NewName = premises.Name,
                        OldTown = originalPremises?.Town,
                        NewTown = premises.Town,
                        OldRiskRating = originalPremises?.RiskRating,
                        NewRiskRating = premises.RiskRating,
                        User = User.Identity?.Name ?? "Unknown"
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PremisesExists(premises.Id))
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

            _logger.LogWarning("Premises edit failed validation: PremisesId {PremisesId}, User {User}",
                premises.Id, User.Identity?.Name ?? "Unknown");

            return View(premises);
        }

        // GET: Premises/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var premises = await _context.Premises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (premises == null)
            {
                return NotFound();
            }

            return View(premises);
        }

        // POST: Premises/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var premises = await _context.Premises.FindAsync(id);
            if (premises != null)
            {
                _context.Premises.Remove(premises);

                _logger.LogWarning("Premises deleted: {@PremisesDeleted}", new
                {
                    PremisesId = id,
                    Name = premises.Name,
                    Town = premises.Town,
                    RiskRating = premises.RiskRating,
                    User = User.Identity?.Name ?? "Unknown"
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PremisesExists(int id)
        {
            return _context.Premises.Any(e => e.Id == id);
        }
    }
}