using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;
using quizportal.Services;

namespace quizportal.Controllers
{
    [Authorize(Roles = AppRoles.ContentManagers)]
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? subjectId = null, int? topicId = null)
        {
            var query = _context.Questions
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.Choices)
                .AsQueryable();

            if (subjectId.HasValue && subjectId > 0)
                query = query.Where(q => q.SubjectId == subjectId);

            if (topicId.HasValue && topicId > 0)
                query = query.Where(q => q.TopicId == topicId);

            var questions = await query
                .OrderBy(q => q.QuestionId)
                .ToListAsync();

            await PopulateFilterDropdownsAsync(subjectId, topicId);
            return View(questions);
        }

        public async Task<IActionResult> Create(int? subjectId = null, int? topicId = null)
        {
            var model = new QuestionFormViewModel
            {
                SubjectId = subjectId ?? 0,
                TopicId = topicId,
                Choices =
                [
                    new QuestionChoiceInput(),
                    new QuestionChoiceInput(),
                    new QuestionChoiceInput(),
                    new QuestionChoiceInput()
                ]
            };

            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionFormViewModel model)
        {
            ValidateQuestionForm(model);

            if (ModelState.IsValid)
            {
                var question = MapToEntity(model);
                question.CreatedDate = DateTime.UtcNow;
                question.IsActive = true;

                var choices = BuildChoices(model);
                foreach (var choice in choices)
                {
                    choice.CreatedDate = DateTime.UtcNow;
                    question.Choices.Add(choice);
                }

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question created successfully.";
                return RedirectToAction(nameof(Create), new { subjectId = question.SubjectId, topicId = question.TopicId });
            }

            EnsureChoiceSlots(model);
            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return RedirectToAction("Login", "Account");

            var question = await _context.Questions
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            var comments = await CommentQueryHelper.LoadCommentThreadsAsync(_context, id, currentUserId.Value);

            var model = new QuestionDetailViewModel
            {
                Question = question,
                ShowCorrectAnswers = true,
                Comments = comments,
                NewComment = new CommentInputViewModel { QuestionId = id }
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            var model = MapToViewModel(question);
            EnsureChoiceSlots(model);
            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuestionFormViewModel model)
        {
            if (id != model.QuestionId)
                return NotFound();

            ValidateQuestionForm(model);

            if (ModelState.IsValid)
            {
                var question = await _context.Questions
                    .Include(q => q.Choices)
                    .FirstOrDefaultAsync(q => q.QuestionId == id);

                if (question == null)
                    return NotFound();

                question.SubjectId = model.SubjectId;
                question.TopicId = model.TopicId;
                question.QuestionStatement = model.QuestionStatement.Trim();
                question.QuestionType = model.QuestionType;
                question.Difficulty = model.Difficulty;
                question.Score = model.Score;
                question.IsActive = model.IsActive;
                question.ModifiedDate = DateTime.UtcNow;

                _context.Choices.RemoveRange(question.Choices);

                var choices = BuildChoices(model);
                foreach (var choice in choices)
                {
                    choice.QuestionId = question.QuestionId;
                    choice.CreatedDate = DateTime.UtcNow;
                    question.Choices.Add(choice);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }

            EnsureChoiceSlots(model);
            await PopulateFormDropdownsAsync(model, model.SubjectId, model.TopicId);
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Subject)
                .Include(q => q.Topic)
                .Include(q => q.Choices)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Choices)
                .Include(q => q.QuizQuestions)
                .Include(q => q.Comments)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            if (question.QuizQuestions.Any() || question.Comments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this question because it is used in quizzes or has comments.";
                return RedirectToAction(nameof(Index));
            }

            _context.Choices.RemoveRange(question.Choices);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question deleted successfully.";
            return RedirectToAction(nameof(Index));
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

        private void ValidateQuestionForm(QuestionFormViewModel model)
        {
            if (model.SubjectId <= 0)
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");

            if (string.IsNullOrWhiteSpace(model.QuestionStatement))
                ModelState.AddModelError(nameof(model.QuestionStatement), "Question statement is required.");

            if (model.Score < 1)
                ModelState.AddModelError(nameof(model.Score), "Score must be at least 1.");

            // Database CHECK constraint allows only 0 or 1 for QuestionType.
            // 1 = single correct, 0 = multiple correct.
            if (model.QuestionType is < 0 or > 1)
                ModelState.AddModelError(nameof(model.QuestionType), "Please select a valid question type.");

            if (model.Difficulty is < 0 or > 2)
                ModelState.AddModelError(nameof(model.Difficulty), "Please select a valid difficulty.");

            var filledChoices = (model.Choices ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.ChoiceText))
                .ToList();

            if (filledChoices.Count < 2)
                ModelState.AddModelError(string.Empty, "Add at least 2 answer choices.");

            if (!filledChoices.Any(c => c.IsCorrect))
                ModelState.AddModelError(string.Empty, "Mark at least one choice as correct.");

            if (model.QuestionType == 1)
            {
                // Single correct: exactly one checked correct choice.
                if (filledChoices.Count(c => c.IsCorrect) != 1)
                    ModelState.AddModelError(string.Empty, "Single-correct questions must have exactly one correct answer.");
            }
            else if (model.QuestionType == 0)
            {
                // Multiple correct: one or more checked correct choices.
                if (filledChoices.Count(c => c.IsCorrect) < 1)
                    ModelState.AddModelError(string.Empty, "Multiple-correct questions must have at least one correct answer.");
            }
        }

        private static List<Choice> BuildChoices(QuestionFormViewModel model) =>
            (model.Choices ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.ChoiceText))
                .Select(c => new Choice
                {
                    ChoiceText = c.ChoiceText.Trim(),
                    IsCorrect = c.IsCorrect
                })
                .ToList();

        private static void EnsureChoiceSlots(QuestionFormViewModel model)
        {
            model.Choices ??= [];

            while (model.Choices.Count < 4)
                model.Choices.Add(new QuestionChoiceInput());
        }

        private async Task PopulateFilterDropdownsAsync(int? selectedSubjectId, int? selectedTopicId)
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

            var subjectItems = subjects
                .Select(s => new SelectListItem
                {
                    Value = s.SubjectId.ToString(),
                    Text = s.SubjectName,
                    Selected = selectedSubjectId == s.SubjectId
                })
                .ToList();

            subjectItems.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Subjects",
                Selected = !selectedSubjectId.HasValue || selectedSubjectId <= 0
            });

            ViewBag.Subjects = subjectItems;

            var topicItems = new List<SelectListItem>
            {
                new()
                {
                    Value = "",
                    Text = "All Topics",
                    Selected = !selectedTopicId.HasValue || selectedTopicId <= 0
                }
            };

            if (selectedSubjectId.HasValue && selectedSubjectId > 0)
            {
                var topics = await _context.Topics
                    .Where(t => t.SubjectId == selectedSubjectId)
                    .OrderBy(t => t.TopicName)
                    .ToListAsync();

                topicItems.AddRange(topics.Select(t => new SelectListItem
                {
                    Value = t.TopicId.ToString(),
                    Text = t.TopicName,
                    Selected = selectedTopicId == t.TopicId
                }));
            }

            ViewBag.Topics = topicItems;
            ViewBag.SelectedSubjectId = selectedSubjectId;
            ViewBag.SelectedTopicId = selectedTopicId;
        }

        private async Task PopulateFormDropdownsAsync(QuestionFormViewModel model, int? selectedSubjectId, int? selectedTopicId)
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

            model.Subjects.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Select Subject --",
                Selected = !selectedSubjectId.HasValue || selectedSubjectId <= 0
            });

            model.Topics =
            [
                new SelectListItem
                {
                    Value = "",
                    Text = "-- Any Topic --",
                    Selected = !selectedTopicId.HasValue || selectedTopicId <= 0
                }
            ];

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

                model.Topics.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = "-- Any Topic --",
                    Selected = !selectedTopicId.HasValue || selectedTopicId <= 0
                });
            }

            ViewBag.HasSubjects = subjects.Count > 0;
        }

        private static Question MapToEntity(QuestionFormViewModel model) => new()
        {
            SubjectId = model.SubjectId,
            TopicId = model.TopicId,
            QuestionStatement = model.QuestionStatement.Trim(),
            QuestionType = model.QuestionType,
            Difficulty = model.Difficulty,
            Score = model.Score,
            IsActive = model.IsActive
        };

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private static QuestionFormViewModel MapToViewModel(Question question) => new()
        {
            QuestionId = question.QuestionId,
            SubjectId = question.SubjectId,
            TopicId = question.TopicId,
            QuestionStatement = question.QuestionStatement,
            QuestionType = question.QuestionType,
            Difficulty = question.Difficulty,
            Score = question.Score,
            IsActive = question.IsActive,
            Choices = question.Choices
                .Select(c => new QuestionChoiceInput
                {
                    ChoiceId = c.ChoiceId,
                    ChoiceText = c.ChoiceText,
                    IsCorrect = c.IsCorrect
                })
                .ToList()
        };
    }
}