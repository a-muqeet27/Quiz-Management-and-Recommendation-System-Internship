using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers
{
    [Authorize(Roles = AppRoles.ContentManagers)]
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
                .Include(q => q.ArrangementQuizzes)
                .Where(q => q.ParentQuizId == null)
                .AsQueryable();

            // Teachers only see quizzes they created; admins see all.
            if (!User.IsInRole(AppRoles.AdminName))
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return RedirectToAction("Login", "Account");

                query = query.Where(q => q.CreatedBy == userId.Value);
            }

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
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return RedirectToAction("Login", "Account");

                var arrangementCount = Math.Clamp(model.ArrangementCount, 1, 20);
                var baseTitle = model.Title?.Trim() ?? "Quiz";
                var questionIds = await SelectRandomQuestionIdsAsync(
                    model.SubjectId,
                    model.TopicId,
                    model.DifficultyFilter,
                    model.QuestionTypeFilter,
                    model.NoOfQuestions);

                var parentQuiz = MapToEntity(model);
                parentQuiz.Title = baseTitle;
                parentQuiz.CreatedDate = DateTime.UtcNow;
                parentQuiz.ParentQuizId = null;
                parentQuiz.CreatedBy = currentUserId.Value;

                _context.Quizzes.Add(parentQuiz);
                await _context.SaveChangesAsync();

                await AssignQuestionsToQuizAsync(parentQuiz.QuizId, questionIds, shuffle: false);
                await _context.SaveChangesAsync();

                for (var set = 2; set <= arrangementCount; set++)
                {
                    var arrangement = MapToEntity(model);
                    arrangement.Title = $"{baseTitle} - Set {set}";
                    arrangement.CreatedDate = DateTime.UtcNow;
                    arrangement.ParentQuizId = parentQuiz.QuizId;
                    arrangement.CreatedBy = currentUserId.Value;

                    _context.Quizzes.Add(arrangement);
                    await _context.SaveChangesAsync();

                    await AssignQuestionsToQuizAsync(arrangement.QuizId, questionIds, shuffle: true);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = arrangementCount == 1
                    ? $"Quiz \"{parentQuiz.Title}\" was created with {parentQuiz.NoOfQuestions} randomly selected questions."
                    : $"Created quiz \"{parentQuiz.Title}\" with {arrangementCount} arrangements (same questions, different order). Sets are listed on this quiz.";

                return RedirectToAction(nameof(GeneratedQuestions), new { id = parentQuiz.QuizId });
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

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

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

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

            var parentId = quiz.ParentQuizId ?? quiz.QuizId;
            var parent = parentId == quiz.QuizId
                ? quiz
                : await _context.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.QuizId == parentId);

            var arrangements = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.ParentQuizId == parentId)
                .OrderBy(q => q.QuizId)
                .ToListAsync();

            ViewBag.ParentQuizId = parentId;
            ViewBag.ParentQuizTitle = parent?.Title ?? quiz.Title;
            ViewBag.IsArrangement = quiz.ParentQuizId.HasValue;
            ViewBag.Arrangements = arrangements;

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

            // Always regenerate from the main quiz so arrangements stay in sync
            var parentId = quiz.ParentQuizId ?? quiz.QuizId;
            var parent = parentId == quiz.QuizId
                ? quiz
                : await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                    .FirstOrDefaultAsync(q => q.QuizId == parentId);

            if (parent == null)
                return NotFound();

            if (!await CanManageQuizAsync(parent))
                return ForbidQuizAccess();

            var availableCount = await BuildQuestionQuery(
                parent.SubjectId,
                parent.TopicId,
                parent.DifficultyFilter,
                parent.QuestionTypeFilter).CountAsync();

            if (availableCount < parent.NoOfQuestions)
            {
                TempData["ErrorMessage"] =
                    $"Not enough questions in the bank. Need {parent.NoOfQuestions}, but only {availableCount} match the quiz filters.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
            }

            var arrangementCount = await RegenerateQuestionsForQuizAndArrangementsAsync(parent);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = arrangementCount == 0
                ? $"Generated a new random set of {parent.NoOfQuestions} questions."
                : $"Generated a new random set of {parent.NoOfQuestions} questions and updated {arrangementCount} arrangement(s) with the same questions (different order).";

            return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateArrangements(int id, int arrangementCount = 1)
        {
            arrangementCount = Math.Clamp(arrangementCount, 1, 20);

            var source = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (source == null)
                return NotFound();

            var parentId = source.ParentQuizId ?? source.QuizId;
            var parent = parentId == source.QuizId
                ? source
                : await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == parentId);

            if (parent == null)
                return NotFound();

            if (!await CanManageQuizAsync(parent))
                return ForbidQuizAccess();

            // Prefer the parent's question set; fall back to the quiz being viewed
            var questionSource = parent.QuizId == source.QuizId
                ? source
                : await _context.Quizzes
                    .Include(q => q.QuizQuestions)
                    .FirstOrDefaultAsync(q => q.QuizId == parentId) ?? source;

            var questionIds = questionSource.QuizQuestions
                .OrderBy(qq => qq.DisplayOrder)
                .Select(qq => qq.QuestionId)
                .ToList();

            if (questionIds.Count == 0)
            {
                TempData["ErrorMessage"] = "This quiz has no questions to rearrange. Generate questions first.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
            }

            var baseTitle = StripSetSuffix(parent.Title) ?? "Quiz";
            var nextSetNumber = await GetNextSetNumberAsync(parentId);

            for (var i = 0; i < arrangementCount; i++)
            {
                var setNumber = nextSetNumber + i;
                var quiz = new Quiz
                {
                    SubjectId = parent.SubjectId,
                    TopicId = parent.TopicId,
                    Title = $"{baseTitle} - Set {setNumber}",
                    NoOfQuestions = parent.NoOfQuestions,
                    TimeLimitMinutes = parent.TimeLimitMinutes,
                    TotalMarks = parent.TotalMarks,
                    DifficultyFilter = parent.DifficultyFilter,
                    QuestionTypeFilter = parent.QuestionTypeFilter,
                    IsActive = parent.IsActive,
                    CreatedBy = parent.CreatedBy ?? GetCurrentUserId(),
                    CreatedDate = DateTime.UtcNow,
                    ParentQuizId = parentId
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                await AssignQuestionsToQuizAsync(quiz.QuizId, questionIds, shuffle: true);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                $"Created {arrangementCount} arrangement(s) under this quiz (same questions, different order).";
            return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);

            if (quiz == null)
                return NotFound();

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

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

                if (!await CanManageQuizAsync(quiz))
                    return ForbidQuizAccess();

                quiz.SubjectId = model.SubjectId;
                quiz.TopicId = model.TopicId;
                quiz.Title = model.Title?.Trim();
                quiz.NoOfQuestions = model.NoOfQuestions;
                quiz.TimeLimitMinutes = model.TimeLimitMinutes;
                quiz.TotalMarks = model.TotalMarks;
                quiz.DifficultyFilter = model.DifficultyFilter;
                quiz.QuestionTypeFilter = model.QuestionTypeFilter;
                quiz.IsActive = model.IsActive;

                if (quiz.ParentQuizId.HasValue)
                {
                    TempData["ErrorMessage"] = "Edit the main quiz to change settings and regenerate questions for all arrangements.";
                    return RedirectToAction(nameof(GeneratedQuestions), new { id = quiz.ParentQuizId });
                }

                var arrangementCount = await RegenerateQuestionsForQuizAndArrangementsAsync(quiz);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = arrangementCount == 0
                    ? "Quiz updated and questions were randomly regenerated."
                    : $"Quiz updated. Regenerated questions for the main quiz and {arrangementCount} arrangement(s).";
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

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

            return View(quiz);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                .Include(q => q.QuizAttempts)
                .Include(q => q.ArrangementQuizzes)
                    .ThenInclude(a => a.QuizAttempts)
                .Include(q => q.ArrangementQuizzes)
                    .ThenInclude(a => a.QuizQuestions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

            if (quiz.ParentQuizId.HasValue)
            {
                TempData["ErrorMessage"] = "Delete arrangements from the parent quiz’s Generated Questions page, or delete the parent quiz.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id = quiz.ParentQuizId });
            }

            if (quiz.QuizAttempts.Any() || quiz.ArrangementQuizzes.Any(a => a.QuizAttempts.Any()))
            {
                TempData["ErrorMessage"] = "Cannot delete this quiz because it (or one of its arrangements) has attempts.";
                return RedirectToAction(nameof(QuizList));
            }

            var assignmentRows = await _context.QuizSetAssignments
                .Where(a => a.ParentQuizId == quiz.QuizId)
                .ToListAsync();
            _context.QuizSetAssignments.RemoveRange(assignmentRows);

            foreach (var arrangement in quiz.ArrangementQuizzes.ToList())
            {
                _context.QuizQuestions.RemoveRange(arrangement.QuizQuestions);
                _context.Quizzes.Remove(arrangement);
            }

            _context.QuizQuestions.RemoveRange(quiz.QuizQuestions);
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Quiz deleted successfully.";
            return RedirectToAction(nameof(QuizList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArrangement(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.QuizQuestions)
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            if (!quiz.ParentQuizId.HasValue)
            {
                TempData["ErrorMessage"] = "This is the main quiz, not an arrangement.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id });
            }

            if (!await CanManageQuizAsync(quiz))
                return ForbidQuizAccess();

            var parentId = quiz.ParentQuizId.Value;

            if (quiz.QuizAttempts.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete this arrangement because it has attempts.";
                return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
            }

            var setAssignments = await _context.QuizSetAssignments
                .Where(a => a.QuizId == quiz.QuizId)
                .ToListAsync();
            _context.QuizSetAssignments.RemoveRange(setAssignments);

            _context.QuizQuestions.RemoveRange(quiz.QuizQuestions);
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Arrangement deleted.";
            return RedirectToAction(nameof(GeneratedQuestions), new { id = parentId });
        }

        public async Task<IActionResult> Assignments(int id)
        {
            var parent = await ResolveParentQuizAsync(id);
            if (parent == null)
                return NotFound();

            if (!await CanManageQuizAsync(parent))
                return ForbidQuizAccess();

            var model = await BuildAssignmentsViewModelAsync(parent);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSets(int id)
        {
            var parent = await ResolveParentQuizAsync(id);
            if (parent == null)
                return NotFound();

            if (!await CanManageQuizAsync(parent))
                return ForbidQuizAccess();

            var sets = await GetQuizSetsAsync(parent.QuizId);
            var students = await _context.Users
                .Where(u => u.UserRole == AppRoles.Student && u.IsActive)
                .OrderBy(u => u.UserId)
                .ToListAsync();

            var existing = await _context.QuizSetAssignments
                .Where(a => a.ParentQuizId == parent.QuizId)
                .ToListAsync();
            _context.QuizSetAssignments.RemoveRange(existing);

            var shuffledSets = sets.OrderBy(_ => Guid.NewGuid()).ToList();
            var shuffledStudents = students.OrderBy(_ => Guid.NewGuid()).ToList();
            var pairCount = Math.Min(shuffledSets.Count, shuffledStudents.Count);
            var now = DateTime.UtcNow;

            for (var i = 0; i < shuffledSets.Count; i++)
            {
                var set = shuffledSets[i];
                _context.QuizSetAssignments.Add(new QuizSetAssignment
                {
                    ParentQuizId = parent.QuizId,
                    QuizId = set.QuizId,
                    UserId = i < pairCount ? shuffledStudents[i].UserId : null,
                    AssignedDate = now
                });
            }

            await _context.SaveChangesAsync();

            var unassignedStudents = students.Count - pairCount;
            var unassignedSets = sets.Count - pairCount;
            TempData["SuccessMessage"] =
                $"Assigned {pairCount} set(s) to {pairCount} student(s)."
                + (unassignedSets > 0 ? $" {unassignedSets} set(s) remain unassigned." : "")
                + (unassignedStudents > 0 ? $" {unassignedStudents} student(s) did not get a set." : "");

            return RedirectToAction(nameof(Assignments), new { id = parent.QuizId });
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

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        private IActionResult ForbidQuizAccess()
        {
            TempData["ErrorMessage"] = "You can only manage quizzes that you created.";
            return RedirectToAction(nameof(QuizList));
        }

        private async Task<bool> CanManageQuizAsync(Quiz quiz)
        {
            if (User.IsInRole(AppRoles.AdminName))
                return true;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return false;

            var ownerId = quiz.CreatedBy;
            if (quiz.ParentQuizId.HasValue)
            {
                ownerId = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => q.QuizId == quiz.ParentQuizId.Value)
                    .Select(q => q.CreatedBy)
                    .FirstOrDefaultAsync();
            }

            return ownerId == userId.Value;
        }

        private async Task<Quiz?> ResolveParentQuizAsync(int id)
        {
            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == id);
            if (quiz == null)
                return null;

            if (!quiz.ParentQuizId.HasValue)
                return quiz;

            return await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quiz.ParentQuizId.Value);
        }

        private async Task<List<Quiz>> GetQuizSetsAsync(int parentQuizId)
        {
            var parent = await _context.Quizzes.FirstAsync(q => q.QuizId == parentQuizId);
            var arrangements = await _context.Quizzes
                .Where(q => q.ParentQuizId == parentQuizId)
                .OrderBy(q => q.QuizId)
                .ToListAsync();

            var sets = new List<Quiz> { parent };
            sets.AddRange(arrangements);
            return sets;
        }

        private async Task<QuizSetAssignmentsViewModel> BuildAssignmentsViewModelAsync(Quiz parent)
        {
            var sets = await GetQuizSetsAsync(parent.QuizId);
            var students = await _context.Users
                .Where(u => u.UserRole == AppRoles.Student && u.IsActive)
                .OrderBy(u => u.FullName ?? u.Username)
                .ToListAsync();

            var assignments = await _context.QuizSetAssignments
                .Include(a => a.User)
                .Where(a => a.ParentQuizId == parent.QuizId)
                .ToListAsync();

            var assignmentByQuizId = assignments.ToDictionary(a => a.QuizId);
            var assignedUserIds = assignments
                .Where(a => a.UserId.HasValue)
                .Select(a => a.UserId!.Value)
                .ToHashSet();

            var setRows = sets.Select((set, index) =>
            {
                assignmentByQuizId.TryGetValue(set.QuizId, out var assignment);
                return new QuizSetAssignmentRowViewModel
                {
                    QuizId = set.QuizId,
                    SetNumber = index + 1,
                    SetTitle = set.Title ?? $"Set {index + 1}",
                    IsMainSet = set.QuizId == parent.QuizId,
                    StudentUserId = assignment?.UserId,
                    StudentName = assignment?.User == null
                        ? null
                        : (assignment.User.FullName ?? assignment.User.Username)
                };
            }).ToList();

            var unassignedStudents = students
                .Where(s => !assignedUserIds.Contains(s.UserId))
                .Select(s => new UnassignedStudentViewModel
                {
                    UserId = s.UserId,
                    Username = s.Username,
                    DisplayName = s.FullName ?? s.Username
                })
                .ToList();

            return new QuizSetAssignmentsViewModel
            {
                ParentQuizId = parent.QuizId,
                QuizTitle = parent.Title ?? "Quiz",
                TotalSets = sets.Count,
                TotalStudents = students.Count,
                AssignedCount = assignedUserIds.Count,
                SetAssignments = setRows,
                UnassignedStudents = unassignedStudents
            };
        }

        private async Task ValidateQuizFormAsync(QuizFormViewModel model)
        {
            if (model.SubjectId <= 0)
                ModelState.AddModelError(nameof(model.SubjectId), "Please select a subject.");

            if (string.IsNullOrWhiteSpace(model.Title))
                ModelState.AddModelError(nameof(model.Title), "Quiz title is required.");

            if (model.NoOfQuestions < 1)
                ModelState.AddModelError(nameof(model.NoOfQuestions), "Number of questions must be at least 1.");

            if (model.QuizId == 0)
            {
                if (model.ArrangementCount < 1)
                    ModelState.AddModelError(nameof(model.ArrangementCount), "Number of arrangements must be at least 1.");
                else if (model.ArrangementCount > 20)
                    ModelState.AddModelError(nameof(model.ArrangementCount), "Number of arrangements cannot exceed 20.");
            }

            if (model.TotalMarks <= 0)
                ModelState.AddModelError(nameof(model.TotalMarks), "Total marks must be greater than 0.");

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

        /// <summary>
        /// Picks one new random question set for the parent quiz and applies it to all
        /// arrangements with reshuffled display order. Returns arrangement count updated.
        /// </summary>
        private async Task<int> RegenerateQuestionsForQuizAndArrangementsAsync(Quiz parent)
        {
            var questionIds = await SelectRandomQuestionIdsAsync(
                parent.SubjectId,
                parent.TopicId,
                parent.DifficultyFilter,
                parent.QuestionTypeFilter,
                parent.NoOfQuestions);

            await AssignQuestionsToQuizAsync(parent.QuizId, questionIds, shuffle: false);

            var arrangements = await _context.Quizzes
                .Where(q => q.ParentQuizId == parent.QuizId)
                .ToListAsync();

            foreach (var arrangement in arrangements)
            {
                arrangement.SubjectId = parent.SubjectId;
                arrangement.TopicId = parent.TopicId;
                arrangement.NoOfQuestions = parent.NoOfQuestions;
                arrangement.TimeLimitMinutes = parent.TimeLimitMinutes;
                arrangement.TotalMarks = parent.TotalMarks;
                arrangement.DifficultyFilter = parent.DifficultyFilter;
                arrangement.QuestionTypeFilter = parent.QuestionTypeFilter;
                arrangement.IsActive = parent.IsActive;
                arrangement.CreatedBy = parent.CreatedBy;

                await AssignQuestionsToQuizAsync(arrangement.QuizId, questionIds, shuffle: true);
            }

            return arrangements.Count;
        }

        private async Task<List<int>> SelectRandomQuestionIdsAsync(
            int subjectId,
            int? topicId,
            int? difficulty,
            int? questionType,
            int take)
        {
            var candidateIds = await BuildQuestionQuery(subjectId, topicId, difficulty, questionType)
                .Select(q => q.QuestionId)
                .ToListAsync();

            return candidateIds
                .OrderBy(_ => Guid.NewGuid())
                .Take(take)
                .ToList();
        }

        private async Task AssignQuestionsToQuizAsync(int quizId, IReadOnlyList<int> questionIds, bool shuffle)
        {
            var existing = await _context.QuizQuestions
                .Where(qq => qq.QuizId == quizId)
                .ToListAsync();

            if (existing.Count > 0)
                _context.QuizQuestions.RemoveRange(existing);

            var ordered = shuffle
                ? questionIds.OrderBy(_ => Guid.NewGuid()).ToList()
                : questionIds.ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                _context.QuizQuestions.Add(new QuizQuestion
                {
                    QuizId = quizId,
                    QuestionId = ordered[i],
                    DisplayOrder = i + 1
                });
            }
        }

        private static string? StripSetSuffix(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return title;

            var trimmed = title.Trim();
            var marker = " - Set ";
            var idx = trimmed.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx <= 0)
                return trimmed;

            var suffix = trimmed[(idx + marker.Length)..];
            if (suffix.Length > 0 && suffix.All(char.IsDigit))
                return trimmed[..idx];

            return trimmed;
        }

        private async Task<int> GetNextSetNumberAsync(int parentQuizId)
        {
            var titles = await _context.Quizzes
                .Where(q => q.ParentQuizId == parentQuizId && q.Title != null)
                .Select(q => q.Title!)
                .ToListAsync();

            var maxSet = 1; // parent quiz is Set 1
            foreach (var title in titles)
            {
                var marker = " - Set ";
                var idx = title.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    continue;

                var suffix = title[(idx + marker.Length)..];
                if (int.TryParse(suffix, out var n))
                    maxSet = Math.Max(maxSet, n);
            }

            return maxSet + 1;
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
