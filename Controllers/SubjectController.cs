using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;

namespace quizportal.Controllers
{
    [Authorize(Roles = AppRoles.TeacherName)]
    public class SubjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Topics)
                .OrderBy(s => s.SubjectId)
                .ToListAsync();

            return View(subjects);
        }

        public IActionResult Create()
        {
            return View(new Subject { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            ModelState.Remove(nameof(Subject.CreatedByNavigation));
            ModelState.Remove(nameof(Subject.Questions));
            ModelState.Remove(nameof(Subject.Quizzes));
            ModelState.Remove(nameof(Subject.Topics));

            if (string.IsNullOrWhiteSpace(subject.SubjectName))
            {
                ModelState.AddModelError(nameof(subject.SubjectName), "Subject name is required.");
            }

            if (ModelState.IsValid)
            {
                subject.SubjectName = subject.SubjectName.Trim();
                subject.CreatedDate = DateTime.UtcNow;
                subject.IsActive = true;

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Subject \"{subject.SubjectName}\" created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subject model)
        {
            if (id != model.SubjectId)
                return NotFound();

            ModelState.Remove(nameof(Subject.CreatedByNavigation));
            ModelState.Remove(nameof(Subject.Questions));
            ModelState.Remove(nameof(Subject.Quizzes));
            ModelState.Remove(nameof(Subject.Topics));

            if (string.IsNullOrWhiteSpace(model.SubjectName))
            {
                ModelState.AddModelError(nameof(model.SubjectName), "Subject name is required.");
            }

            if (ModelState.IsValid)
            {
                var subject = await _context.Subjects.FindAsync(id);
                if (subject == null)
                    return NotFound();

                subject.SubjectName = model.SubjectName.Trim();
                subject.SubjectDescription = model.SubjectDescription;
                subject.IsActive = model.IsActive;
                subject.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Subject updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Topics)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
                return NotFound();

            return View(subject);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Topics)
                .Include(s => s.Quizzes)
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
                return NotFound();

            if (subject.Topics.Any() || subject.Quizzes.Any() || subject.Questions.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this subject because it has related topics, quizzes, or questions.";
                return RedirectToAction(nameof(Index));
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subject deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
