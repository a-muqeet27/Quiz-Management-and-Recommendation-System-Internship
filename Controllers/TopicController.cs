using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;

namespace quizportal.Controllers
{
    [Authorize(Roles = AppRoles.ContentManagers)]
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? subjectId = null)
        {
            var query = _context.Topics
                .Include(t => t.Subject)
                .AsQueryable();

            if (subjectId.HasValue && subjectId > 0)
            {
                query = query.Where(t => t.SubjectId == subjectId);
            }

            var topics = await query
                .OrderBy(t => t.TopicId)
                .ToListAsync();

            ViewBag.SelectedSubjectId = subjectId;
            await PopulateSubjectsAsync(subjectId, includeEmptyOption: true, emptyText: "All Subjects");

            return View(topics);
        }

        public async Task<IActionResult> Create(int? subjectId = null)
        {
            await PopulateSubjectsAsync(subjectId, includeEmptyOption: true, emptyText: "-- Select Subject --");
            return View(new Topic { SubjectId = subjectId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Topic topic)
        {
            ModelState.Remove(nameof(Topic.Subject));
            ModelState.Remove(nameof(Topic.Questions));
            ModelState.Remove(nameof(Topic.Quizzes));

            if (topic.SubjectId <= 0)
            {
                ModelState.AddModelError(nameof(topic.SubjectId), "Please select a subject.");
            }

            if (string.IsNullOrWhiteSpace(topic.TopicName))
            {
                ModelState.AddModelError(nameof(topic.TopicName), "Topic name is required.");
            }

            if (ModelState.IsValid)
            {
                var exists = await _context.Topics.AnyAsync(t =>
                    t.SubjectId == topic.SubjectId &&
                    t.TopicName == topic.TopicName.Trim());

                if (exists)
                {
                    ModelState.AddModelError(nameof(topic.TopicName), "This topic already exists for the selected subject.");
                }
            }

            if (ModelState.IsValid)
            {
                topic.TopicName = topic.TopicName.Trim();
                topic.CreatedDate = DateTime.UtcNow;

                _context.Topics.Add(topic);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Topic \"{topic.TopicName}\" created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSubjectsAsync(topic.SubjectId, includeEmptyOption: true, emptyText: "-- Select Subject --");
            return View(topic);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
                return NotFound();

            await PopulateSubjectsAsync(topic.SubjectId, includeEmptyOption: true, emptyText: "-- Select Subject --");
            return View(topic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Topic model)
        {
            if (id != model.TopicId)
                return NotFound();

            ModelState.Remove(nameof(Topic.Subject));
            ModelState.Remove(nameof(Topic.Questions));
            ModelState.Remove(nameof(Topic.Quizzes));

            if (model.SubjectId <= 0)
            {
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");
            }

            if (string.IsNullOrWhiteSpace(model.TopicName))
            {
                ModelState.AddModelError(nameof(model.TopicName), "Topic name is required.");
            }

            if (ModelState.IsValid)
            {
                var exists = await _context.Topics.AnyAsync(t =>
                    t.SubjectId == model.SubjectId &&
                    t.TopicName == model.TopicName.Trim() &&
                    t.TopicId != id);

                if (exists)
                {
                    ModelState.AddModelError(nameof(model.TopicName), "This topic already exists for the selected subject.");
                }
            }

            if (ModelState.IsValid)
            {
                var topic = await _context.Topics.FindAsync(id);
                if (topic == null)
                    return NotFound();

                topic.SubjectId = model.SubjectId;
                topic.TopicName = model.TopicName.Trim();
                topic.TopicDescription = model.TopicDescription;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Topic updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateSubjectsAsync(model.SubjectId, includeEmptyOption: true, emptyText: "-- Select Subject --");
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var topic = await _context.Topics
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
                return NotFound();

            return View(topic);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics
                .Include(t => t.Questions)
                .Include(t => t.Quizzes)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
                return NotFound();

            if (topic.Questions.Any() || topic.Quizzes.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this topic because it has related quizzes or questions.";
                return RedirectToAction(nameof(Index));
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Topic deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSubjectsAsync(int? selectedSubjectId = null, bool includeEmptyOption = false, string emptyText = "-- Select Subject --")
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            var items = subjects
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.IsActive ? s.SubjectName : $"{s.SubjectName} (inactive)",
                    Selected = selectedSubjectId.HasValue && selectedSubjectId.Value == s.SubjectId
                })
                .ToList();

            if (includeEmptyOption)
            {
                items.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = emptyText,
                    Selected = !selectedSubjectId.HasValue || selectedSubjectId.Value <= 0
                });
            }

            ViewBag.Subjects = items;
            ViewBag.HasSubjects = subjects.Count > 0;
        }
    }
}
