using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers
{
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> QuizList()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .OrderByDescending(q => q.CreatedDate)
                .ToListAsync();

            return View(quizzes);
        }

        public async Task<IActionResult> Create()
        {
            var model = new QuizFormViewModel();
            await PopulateFormDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizFormViewModel model)
        {
            if (model.SubjectId <= 0)
            {
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");
            }

            if (ModelState.IsValid)
            {
                var quiz = MapToEntity(model);
                quiz.CreatedDate = DateTime.UtcNow;

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Quiz \"{quiz.Title}\" was created successfully.";
                return RedirectToAction(nameof(Details), new { id = quiz.QuizId });
            }

            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz == null)
                return NotFound();

            var model = MapToViewModel(quiz);
            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuizFormViewModel model)
        {
            if (id != model.QuizId)
                return NotFound();

            if (model.SubjectId <= 0)
            {
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");
            }

            if (ModelState.IsValid)
            {
                var quiz = await _context.Quizzes.FindAsync(id);
                if (quiz == null)
                    return NotFound();

                quiz.SubjectId = model.SubjectId;
                quiz.TopicId = model.TopicId;
                quiz.Title = model.Title;
                quiz.NoOfQuestions = model.NoOfQuestions;
                quiz.TimeLimitMinutes = model.TimeLimitMinutes;
                quiz.TotalMarks = model.TotalMarks;
                quiz.DifficultyFilter = model.DifficultyFilter;
                quiz.QuestionTypeFilter = model.QuestionTypeFilter;
                quiz.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quiz updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz != null)
            {
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Quiz deleted successfully.";
            }

            return RedirectToAction(nameof(QuizList));
        }

        [HttpGet]
        public async Task<IActionResult> GetTopics(int subjectId)
        {
            var topics = await _context.Topics
                .Where(t => t.SubjectId == subjectId)
                .OrderBy(t => t.TopicName)
                .Select(t => new { t.TopicId, t.TopicName })
                .ToListAsync();

            return Json(topics);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionCount(int subjectId, int? topicId, int? difficulty, int? questionType)
        {
            var query = _context.Questions
                .Where(q => q.IsActive && q.SubjectId == subjectId);

            if (topicId.HasValue)
                query = query.Where(q => q.TopicId == topicId);

            if (difficulty.HasValue)
                query = query.Where(q => q.Difficulty == difficulty);

            if (questionType.HasValue)
                query = query.Where(q => q.QuestionType == questionType);

            var count = await query.CountAsync();
            return Json(new { count });
        }

        private async Task PopulateFormDropdownsAsync(QuizFormViewModel model, int? selectedSubjectId = null, int? selectedTopicId = null)
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            model.Subjects = subjects
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.SubjectName,
                    Selected = s.SubjectId == selectedSubjectId
                })
                .ToList();

            model.Subjects.Insert(0, new SelectListItem { Value = "", Text = "-- Select Subject --" });

            model.Topics = [new SelectListItem { Value = "", Text = "-- Any Topic --" }];

            if (selectedSubjectId.HasValue && selectedSubjectId > 0)
            {
                var topics = await _context.Topics
                    .Where(t => t.SubjectId == selectedSubjectId)
                    .OrderBy(t => t.TopicName)
                    .ToListAsync();

                model.Topics = topics
                    .Select(t => new SelectListItem
                    {
                        Value = t.TopicId.ToString(),
                        Text = t.TopicName,
                        Selected = t.TopicId == selectedTopicId
                    })
                    .ToList();

                model.Topics.Insert(0, new SelectListItem { Value = "", Text = "-- Any Topic --" });
            }
        }

        private static Quiz MapToEntity(QuizFormViewModel model) => new()
        {
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
            Title = model.Title,
            NoOfQuestions = model.NoOfQuestions,
            TimeLimitMinutes = model.TimeLimitMinutes,
            TotalMarks = model.TotalMarks,
            DifficultyFilter = model.DifficultyFilter,
            QuestionTypeFilter = model.QuestionTypeFilter,
            IsActive = model.IsActive
        };

        private static QuizFormViewModel MapToViewModel(Quiz quiz) => new()
        {
            QuizId = quiz.QuizId,
            SubjectId = quiz.SubjectId,
            TopicId = quiz.TopicId,
            Title = quiz.Title,
            NoOfQuestions = quiz.NoOfQuestions,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            TotalMarks = quiz.TotalMarks,
            DifficultyFilter = quiz.DifficultyFilter,
            QuestionTypeFilter = quiz.QuestionTypeFilter,
            IsActive = quiz.IsActive
        };
    }
}
