using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SVM.Models;

namespace SVM.Controllers
{
    public class UpdatesController : Controller
    {
        private readonly SvmContext _context;

        public UpdatesController(SvmContext context)
        {
            _context = context;
        }

        // GET: Updates
        public async Task<IActionResult> Index()
        {
            return View(await _context.Updates.ToListAsync());
        }

        // GET: Updates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var updates = await _context.Updates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (updates == null)
            {
                return NotFound();
            }

            return View(updates);
        }

        // GET: Updates/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Updates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Category,FilePath,Status,CreatedAt")] Updates updates)
        {
            if (ModelState.IsValid)
            {
                _context.Add(updates);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(updates);
        }

        // GET: Updates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var updates = await _context.Updates.FindAsync(id);
            if (updates == null)
            {
                return NotFound();
            }
            return View(updates);
        }

        // POST: Updates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Category,FilePath,Status,CreatedAt")] Updates updates)
        {
            if (id != updates.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(updates);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UpdatesExists(updates.Id))
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
            return View(updates);
        }

        // GET: Updates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var updates = await _context.Updates
                .FirstOrDefaultAsync(m => m.Id == id);
            if (updates == null)
            {
                return NotFound();
            }

            return View(updates);
        }

        // POST: Updates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var updates = await _context.Updates.FindAsync(id);
            if (updates != null)
            {
                _context.Updates.Remove(updates);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UpdatesExists(int id)
        {
            return _context.Updates.Any(e => e.Id == id);
        }
    }
}
