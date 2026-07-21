using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers
{
    [Authorize(Roles = AppRoles.TeacherName)]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> QuizList(int? subjectId = null)
        {
            var query = _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.QuizQuestions)
                .AsQueryable();

            if (subjectId.HasValue && subjectId > 0)
                query = query.Where(q => q.SubjectId == subjectId);

            var quizzes = await query
                .OrderBy(q => q.QuizId)
                .ToListAsync();

            await PopulateFilterDropdownsAsync(subjectId);
            return View(quizzes);
        }

        public async Task<IActionResult> Create()
        {
            var model = new QuizFormViewModel();
            await PopulateFormDropdownsAsync(model);
            ViewBag.HasSubjects = model.Subjects.Any(s => !string.IsNullOrEmpty(s.Value));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizFormViewModel model)
        {
            await ValidateQuizFormAsync(model);

            if (ModelState.IsValid)
            {
                var quiz = MapToEntity(model);
                quiz.CreatedDate = DateTime.UtcNow;

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                await GenerateRandomQuestionsAsync(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Quiz \"{quiz.Title}\" was created with {quiz.NoOfQuestions} randomly selected questions.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id = quiz.QuizId });
            }

            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            ViewBag.HasSubjects = model.Subjects.Any(s => !string.IsNullOrEmpty(s.Value));
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        public async Task<IActionResult> GeneratedQuestions(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.QuizQuestions)
                    .ThenInclude(qq => qq.Question)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var availableCount = await BuildQuestionQuery(
                quiz.SubjectId,
                quiz.TopicId,
                quiz.DifficultyFilter,
                quiz.QuestionTypeFilter).CountAsync();

            if (availableCount < quiz.NoOfQuestions)
            {
                TempData["ErrorMessage"] =
                    $"Not enough questions in the bank. Need {quiz.NoOfQuestions}, but only {availableCount} match the quiz filters.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id });
            }

            await GenerateRandomQuestionsAsync(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Generated a new random set of {quiz.NoOfQuestions} questions.";
            return RedirectToAction(nameof(GeneratedQuestions), new { id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz == null)
                return NotFound();

            var model = MapToViewModel(quiz);
            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            ViewBag.HasSubjects = model.Subjects.Any(s => !string.IsNullOrEmpty(s.Value));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuizFormViewModel model)
        {
            if (id != model.QuizId)
                return NotFound();

            await ValidateQuizFormAsync(model);

            if (ModelState.IsValid)
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                    .FirstOrDefaultAsync(q => q.QuizId == id);

                if (quiz == null)
                    return NotFound();

                quiz.SubjectId = model.SubjectId;
                quiz.TopicId = model.TopicId;
                quiz.Title = model.Title?.Trim();
                quiz.NoOfQuestions = model.NoOfQuestions;
                quiz.TimeLimitMinutes = model.TimeLimitMinutes;
                quiz.TotalMarks = model.TotalMarks;
                quiz.DifficultyFilter = model.DifficultyFilter;
                quiz.QuestionTypeFilter = model.QuestionTypeFilter;
                quiz.IsActive = model.IsActive;

                await GenerateRandomQuestionsAsync(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Quiz updated and questions were randomly regenerated.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id });
            }

            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            ViewBag.HasSubjects = model.Subjects.Any(s => !string.IsNullOrEmpty(s.Value));
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (quiz.QuizAttempts.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this quiz because it has attempts.";
                return RedirectToAction(nameof(QuizList));
            }

            _context.QuizQuestions.RemoveRange(quiz.QuizQuestions);
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Quiz deleted successfully.";
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
            var count = await BuildQuestionQuery(subjectId, topicId, difficulty, questionType).CountAsync();
            return Json(new { count });
        }

        private async Task ValidateQuizFormAsync(QuizFormViewModel model)
        {
            if (model.SubjectId <= 0)
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), "Quiz title is required.");

            if (model.NoOfQuestions < 1)
                ModelState.AddModelError(nameof(model.NoOfQuestions), "Number of questions must be at least 1.");

            if (model.TotalMarks < 1)
                ModelState.AddModelError(nameof(model.TotalMarks), "Total marks must be at least 1.");

            if (model.SubjectId > 0 && model.NoOfQuestions >= 1)
            {
                var availableCount = await BuildQuestionQuery(
                    model.SubjectId,
                    model.TopicId,
                    model.DifficultyFilter,
                    model.QuestionTypeFilter).CountAsync();

                if (availableCount < model.NoOfQuestions)
                {
                    ModelState.AddModelError(nameof(model.NoOfQuestions),
                        $"Not enough questions in the bank. Need {model.NoOfQuestions}, but only {availableCount} match your filters.");
                }
            }
        }

        private async Task GenerateRandomQuestionsAsync(Quiz quiz)
        {
            var existing = await _context.QuizQuestions
                .Where(qq => qq.QuizId == quiz.QuizId)
                .ToListAsync();

            if (existing.Count > 0)
                _context.QuizQuestions.RemoveRange(existing);

            var candidateIds = await BuildQuestionQuery(
                    quiz.SubjectId,
                    quiz.TopicId,
                    quiz.DifficultyFilter,
                    quiz.QuestionTypeFilter)
                .Select(q => q.QuestionId)
                .ToListAsync();

            var randomIds = candidateIds
                .OrderBy(_ => Guid.NewGuid())
                .Take(quiz.NoOfQuestions)
                .ToList();

            for (var i = 0; i < randomIds.Count; i++)
            {
                _context.QuizQuestions.Add(new QuizQuestion
                {
                    QuizId = quiz.QuizId,
                    QuestionId = randomIds[i],
                    DisplayOrder = i + 1
                });
            }
        }

        private IQueryable<Question> BuildQuestionQuery(int subjectId, int? topicId, int? difficulty, int? questionType)
        {
            var query = _context.Questions
                .Where(q => q.IsActive && q.SubjectId == subjectId);

            if (topicId.HasValue && topicId > 0)
                query = query.Where(q => q.TopicId == topicId);

            if (difficulty.HasValue)
                query = query.Where(q => q.Difficulty == difficulty);

            if (questionType.HasValue)
                query = query.Where(q => q.QuestionType == questionType);

            return query;
        }

        private async Task PopulateFilterDropdownsAsync(int? selectedSubjectId)
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            var items = subjects
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.SubjectName,
                    Selected = selectedSubjectId == s.SubjectId
                })
                .ToList();

            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Subjects",
                Selected = !selectedSubjectId.HasValue || selectedSubjectId <= 0
            });

            ViewBag.Subjects = items;
        }

        private async Task PopulateFormDropdownsAsync(QuizFormViewModel model, int? selectedSubjectId = null, int? selectedTopicId = null)
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            model.Subjects = subjects
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.IsActive ? s.SubjectName : $"{s.SubjectName} (inactive)",
                    Selected = selectedSubjectId == s.SubjectId
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
                        Selected = selectedTopicId == t.TopicId
                    })
                    .ToList();

                model.Topics.Insert(0, new SelectListItem { Value = "", Text = "-- Any Topic --" });
            }
        }

        private static Quiz MapToEntity(QuizFormViewModel model) => new()
        {
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
            Title = model.Title?.Trim(),
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
