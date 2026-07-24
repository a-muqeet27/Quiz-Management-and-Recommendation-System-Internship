using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers;

[Authorize(Roles = AppRoles.StudentName)]
public class QuizAttemptController : Controller
{
    private readonly ApplicationDbContext _context;

    public QuizAttemptController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? subjectId = null)
    {
        var userId = GetCurrentStudentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var assignedQuizIds = await _context.QuizSetAssignments
            .Where(a => a.UserId == userId.Value)
            .Select(a => a.QuizId)
            .ToListAsync();

        var parentsWithAssignments = await _context.QuizSetAssignments
            .Select(a => a.ParentQuizId)
            .Distinct()
            .ToListAsync();

        var parentsWithArrangements = await _context.Quizzes
            .Where(q => q.ParentQuizId != null)
            .Select(q => q.ParentQuizId!.Value)
            .Distinct()
            .ToListAsync();

        var query = _context.Quizzes
            .Include(q => q.Subject)
            .Include(q => q.Topic)
            .Include(q => q.QuizQuestions)
            .Where(q => q.IsActive && q.QuizQuestions.Any())
            .Where(q =>
                assignedQuizIds.Contains(q.QuizId)
                || (
                    // Single-set quizzes (no arrangements) with no assignment rows: open to all students
                    q.ParentQuizId == null
                    && !parentsWithArrangements.Contains(q.QuizId)
                    && !parentsWithAssignments.Contains(q.QuizId)
                ))
            .AsQueryable();

        if (subjectId.HasValue && subjectId > 0)
            query = query.Where(q => q.SubjectId == subjectId);

        var quizzes = await query
            .OrderBy(q => q.QuizId)
            .ToListAsync();

        // Prefer showing the parent title so students don't see "Set N" labels.
        var parentIdsNeeded = quizzes
            .Where(q => q.ParentQuizId.HasValue)
            .Select(q => q.ParentQuizId!.Value)
            .Distinct()
            .ToList();

        if (parentIdsNeeded.Count > 0)
        {
            var parentTitles = await _context.Quizzes
                .AsNoTracking()
                .Where(q => parentIdsNeeded.Contains(q.QuizId))
                .ToDictionaryAsync(q => q.QuizId, q => q.Title);

            foreach (var quiz in quizzes.Where(q => q.ParentQuizId.HasValue))
            {
                if (parentTitles.TryGetValue(quiz.ParentQuizId!.Value, out var parentTitle)
                    && !string.IsNullOrWhiteSpace(parentTitle))
                {
                    quiz.Title = parentTitle;
                }
            }
        }

        await PopulateFilterDropdownsAsync(subjectId);
        ViewBag.RecentAttempts = await LoadRecentAttemptsAsync();
        ViewBag.StudentName = GetCurrentStudentDisplayName();
        return View(quizzes);
    }

    public async Task<IActionResult> History()
    {
        var attempts = await LoadRecentAttemptsAsync();
        ViewBag.StudentName = GetCurrentStudentDisplayName();
        return View(attempts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentStudentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        var attempt = await _context.QuizAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.QuizAttemptId == id);

        if (attempt == null || attempt.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "You can only view your own saved attempts.";
            return RedirectToAction(nameof(History));
        }

        var model = await BuildDetailsViewModelAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    public async Task<IActionResult> Review(int id)
    {
        var currentUserId = GetCurrentStudentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        var attempt = await _context.QuizAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.QuizAttemptId == id);

        if (attempt == null || attempt.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "You can only review your own attempts.";
            return RedirectToAction(nameof(History));
        }

        var model = await BuildReviewViewModelAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    public async Task<IActionResult> Start(int id)
    {
        var quiz = await LoadQuizForAttemptAsync(id);
        if (quiz == null)
            return NotFound();

        var accessError = await EnsureStudentCanAttemptQuizAsync(quiz);
        if (accessError != null)
        {
            TempData["ErrorMessage"] = accessError;
            return RedirectToAction(nameof(Index));
        }

        if (!quiz.IsActive)
        {
            TempData["ErrorMessage"] = "This quiz is not active.";
            return RedirectToAction(nameof(Index));
        }

        if (!quiz.QuizQuestions.Any())
        {
            TempData["ErrorMessage"] = "This quiz has no generated questions yet.";
            return RedirectToAction(nameof(Index));
        }

        var displayTitle = await GetStudentFacingQuizTitleAsync(quiz);

        var model = new QuizAttemptStartViewModel
        {
            QuizId = quiz.QuizId,
            Title = displayTitle,
            SubjectName = quiz.Subject?.SubjectName,
            QuestionCount = quiz.QuizQuestions.Count,
            TotalMarks = quiz.TotalMarks,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            StudentName = GetCurrentStudentDisplayName() ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(QuizAttemptStartViewModel model)
    {
        var quiz = await LoadQuizForAttemptAsync(model.QuizId);
        if (quiz == null)
            return NotFound();

        var accessError = await EnsureStudentCanAttemptQuizAsync(quiz);
        if (accessError != null)
        {
            TempData["ErrorMessage"] = accessError;
            return RedirectToAction(nameof(Index));
        }

        if (!quiz.IsActive || !quiz.QuizQuestions.Any())
        {
            TempData["ErrorMessage"] = "This quiz cannot be attempted right now.";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentStudentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var attempt = new QuizAttempt
        {
            QuizId = quiz.QuizId,
            UserId = userId.Value,
            AttemptDate = DateTime.UtcNow,
            QuizStatus = 0
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Take), new { id = attempt.QuizAttemptId });
    }

    public async Task<IActionResult> Take(int id)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.QuizScores)
            .FirstOrDefaultAsync(a => a.QuizAttemptId == id);

        if (attempt == null)
            return NotFound();

        if (attempt.QuizScores.Any())
            return RedirectToAction(nameof(Details), new { id });

        var questions = await _context.QuizQuestions
            .Where(qq => qq.QuizId == attempt.QuizId)
            .Include(qq => qq.Question)
                .ThenInclude(q => q.Choices)
            .OrderBy(qq => qq.DisplayOrder)
            .ToListAsync();

        var timeLimitMinutes = attempt.Quiz?.TimeLimitMinutes ?? 0;
        var remainingSeconds = 0;
        if (timeLimitMinutes > 0)
        {
            var deadline = attempt.AttemptDate.AddMinutes(timeLimitMinutes);
            remainingSeconds = Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));
        }

        var model = new QuizAttemptTakeViewModel
        {
            QuizAttemptId = attempt.QuizAttemptId,
            QuizTitle = attempt.Quiz?.Title ?? "Quiz",
            TimeLimitMinutes = timeLimitMinutes,
            RemainingSeconds = remainingSeconds,
            Questions = questions.Select(qq => new QuizAttemptQuestionViewModel
            {
                QuestionId = qq.QuestionId,
                QuestionStatement = qq.Question.QuestionStatement,
                QuestionType = qq.Question.QuestionType,
                Score = qq.Question.Score,
                DisplayOrder = qq.DisplayOrder,
                Choices = qq.Question.Choices
                    .OrderBy(c => c.ChoiceId)
                    .Select(c => new QuizAttemptChoiceViewModel
                    {
                        ChoiceId = c.ChoiceId,
                        ChoiceText = c.ChoiceText
                    })
                    .ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(QuizAttemptSubmitViewModel model)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .Include(a => a.QuizScores)
            .FirstOrDefaultAsync(a => a.QuizAttemptId == model.QuizAttemptId);

        if (attempt == null)
            return NotFound();

        if (attempt.QuizScores.Any())
            return RedirectToAction(nameof(Details), new { id = attempt.QuizAttemptId });

        var quizQuestions = await _context.QuizQuestions
            .Where(qq => qq.QuizId == attempt.QuizId)
            .Include(qq => qq.Question)
                .ThenInclude(q => q.Choices)
            .OrderBy(qq => qq.DisplayOrder)
            .ToListAsync();

        var answersByQuestion = (model.Answers ?? [])
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedChoiceIds?.Distinct().ToList() ?? []);

        var correctCount = 0;
        var wrongCount = 0;
        var unattemptedCount = 0;
        decimal obtainedMarks = 0;
        decimal totalMarks = 0;

        foreach (var quizQuestion in quizQuestions)
        {
            var question = quizQuestion.Question;
            totalMarks += question.Score;

            answersByQuestion.TryGetValue(question.QuestionId, out var selectedChoiceIds);
            selectedChoiceIds ??= [];

            var correctChoiceIds = question.Choices
                .Where(c => c.IsCorrect)
                .Select(c => c.ChoiceId)
                .ToHashSet();

            var selectedSet = selectedChoiceIds.ToHashSet();
            var isAttempted = selectedSet.Count > 0;
            var questionMarks = CalculateQuestionMarks(question, selectedSet, correctChoiceIds);
            var isFullyCorrect = isAttempted && questionMarks == question.Score;

            if (!isAttempted)
                unattemptedCount++;
            else if (isFullyCorrect)
                correctCount++;
            else
                wrongCount++;

            obtainedMarks += questionMarks;

            var marksAssigned = false;
            foreach (var choice in question.Choices)
            {
                var isSelected = selectedSet.Contains(choice.ChoiceId);
                _context.QuizChoices.Add(new QuizChoice
                {
                    QuizAttemptId = attempt.QuizAttemptId,
                    QuestionId = question.QuestionId,
                    ChoiceId = choice.ChoiceId,
                    IsSelected = isSelected,
                    IsCorrect = choice.IsCorrect,
                    // Store question total on one row so review can use Max(MarksObtained).
                    MarksObtained = !marksAssigned ? questionMarks : 0
                });
                marksAssigned = true;
            }
        }

        var percentage = totalMarks > 0
            ? Math.Round(obtainedMarks / totalMarks * 100, 2)
            : 0;

        attempt.CompletedAt = DateTime.UtcNow;
        attempt.QuizStatus = 1;
        attempt.TimeTaken = (int)Math.Ceiling((attempt.CompletedAt.Value - attempt.AttemptDate).TotalMinutes);

        _context.QuizScores.Add(new QuizScore
        {
            QuizAttemptId = attempt.QuizAttemptId,
            TotalMarks = totalMarks,
            ObtainedMarks = obtainedMarks,
            QuizPercentage = percentage,
            CorrectAnswers = correctCount,
            WrongAnswers = wrongCount,
            Unattempted = unattemptedCount,
            PerformanceStatus = GetPerformanceStatus(percentage)
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = model.TimedOut
            ? "Time is up. Your quiz attempt has been saved."
            : "Your quiz attempt has been saved.";
        return RedirectToAction(nameof(Details), new { id = attempt.QuizAttemptId });
    }

    public IActionResult Result(int id) => RedirectToAction(nameof(Details), new { id });

    private async Task<QuizAttemptDetailsViewModel?> BuildDetailsViewModelAsync(int attemptId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .Include(a => a.QuizScores)
            .Include(a => a.QuizChoices)
                .ThenInclude(qc => qc.Choice)
            .FirstOrDefaultAsync(a => a.QuizAttemptId == attemptId);

        if (attempt == null)
            return null;

        var score = attempt.QuizScores.OrderByDescending(s => s.QuizScoreId).FirstOrDefault();
        if (score == null)
            return null;

        var quizQuestions = await _context.QuizQuestions
            .Where(qq => qq.QuizId == attempt.QuizId)
            .Include(qq => qq.Question)
            .OrderBy(qq => qq.DisplayOrder)
            .ToListAsync();

        var choicesByQuestion = attempt.QuizChoices
            .GroupBy(qc => qc.QuestionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.ChoiceId).ToList());

        var questions = quizQuestions.Select(qq =>
        {
            choicesByQuestion.TryGetValue(qq.QuestionId, out var questionChoices);
            questionChoices ??= [];

            return new QuizAttemptDetailsQuestionViewModel
            {
                QuestionStatement = qq.Question.QuestionStatement,
                Score = qq.Question.Score,
                Choices = questionChoices.Select(c => new QuizAttemptDetailsChoiceViewModel
                {
                    ChoiceText = c.Choice.ChoiceText,
                    IsSelected = c.IsSelected
                }).ToList()
            };
        }).ToList();

        return new QuizAttemptDetailsViewModel
        {
            QuizAttemptId = attempt.QuizAttemptId,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz?.Title ?? "Quiz",
            StudentName = attempt.User?.FullName ?? attempt.User?.Username ?? "Student",
            AttemptDate = attempt.CompletedAt ?? attempt.AttemptDate,
            TotalMarks = score.TotalMarks,
            ObtainedMarks = score.ObtainedMarks,
            QuizPercentage = score.QuizPercentage,
            Questions = questions
        };
    }

    private async Task<QuizAttemptReviewViewModel?> BuildReviewViewModelAsync(int attemptId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .Include(a => a.QuizScores)
            .Include(a => a.QuizChoices)
                .ThenInclude(qc => qc.Choice)
            .FirstOrDefaultAsync(a => a.QuizAttemptId == attemptId);

        if (attempt == null)
            return null;

        var score = attempt.QuizScores.OrderByDescending(s => s.QuizScoreId).FirstOrDefault();
        if (score == null)
            return null;

        var quizQuestions = await _context.QuizQuestions
            .Where(qq => qq.QuizId == attempt.QuizId)
            .Include(qq => qq.Question)
            .OrderBy(qq => qq.DisplayOrder)
            .ToListAsync();

        var choicesByQuestion = attempt.QuizChoices
            .GroupBy(qc => qc.QuestionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.ChoiceId).ToList());

        var questions = quizQuestions.Select(qq =>
        {
            choicesByQuestion.TryGetValue(qq.QuestionId, out var questionChoices);
            questionChoices ??= [];

            var selectedIds = questionChoices
                .Where(c => c.IsSelected)
                .Select(c => c.ChoiceId)
                .OrderBy(x => x)
                .ToList();

            var marksObtained = questionChoices.Count == 0
                ? 0
                : questionChoices.Max(c => c.MarksObtained);

            var status = selectedIds.Count == 0
                ? "Unattempted"
                : marksObtained >= qq.Question.Score ? "Correct"
                : marksObtained > 0 ? "Partial"
                : "Wrong";

            return new QuizAttemptReviewQuestionViewModel
            {
                QuestionId = qq.QuestionId,
                QuestionStatement = qq.Question.QuestionStatement,
                Score = qq.Question.Score,
                MarksObtained = marksObtained,
                Status = status,
                Choices = questionChoices.Select(c => new QuizAttemptReviewChoiceViewModel
                {
                    ChoiceId = c.ChoiceId,
                    ChoiceText = c.Choice.ChoiceText,
                    IsSelected = c.IsSelected,
                    IsCorrect = c.IsCorrect
                }).ToList()
            };
        }).ToList();

        return new QuizAttemptReviewViewModel
        {
            QuizAttemptId = attempt.QuizAttemptId,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz?.Title ?? "Quiz",
            StudentName = attempt.User?.FullName ?? attempt.User?.Username ?? "Student",
            TotalMarks = score.TotalMarks,
            ObtainedMarks = score.ObtainedMarks,
            QuizPercentage = score.QuizPercentage,
            CorrectAnswers = score.CorrectAnswers,
            WrongAnswers = score.WrongAnswers,
            Unattempted = score.Unattempted,
            Questions = questions
        };
    }

    private async Task<List<QuizAttemptHistoryViewModel>> LoadRecentAttemptsAsync()
    {
        var userId = GetCurrentStudentUserId();
        if (!userId.HasValue)
            return [];

        var attempts = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.User)
            .Include(a => a.QuizScores)
            .Where(a => a.UserId == userId.Value && a.QuizScores.Any())
            .OrderByDescending(a => a.CompletedAt ?? a.AttemptDate)
            .ToListAsync();

        return attempts.Select(a =>
        {
            var score = a.QuizScores.OrderByDescending(s => s.QuizScoreId).First();
            return new QuizAttemptHistoryViewModel
            {
                QuizAttemptId = a.QuizAttemptId,
                QuizId = a.QuizId,
                QuizTitle = a.Quiz?.Title ?? "Quiz",
                StudentName = a.User?.FullName ?? a.User?.Username ?? "Student",
                AttemptDate = a.CompletedAt ?? a.AttemptDate,
                ObtainedMarks = score.ObtainedMarks,
                TotalMarks = score.TotalMarks,
                QuizPercentage = score.QuizPercentage
            };
        }).ToList();
    }

    private async Task<Quiz?> LoadQuizForAttemptAsync(int quizId)
    {
        return await _context.Quizzes
            .Include(q => q.Subject)
            .Include(q => q.QuizQuestions)
            .FirstOrDefaultAsync(q => q.QuizId == quizId);
    }

    private static decimal CalculateQuestionMarks(
        Question question,
        HashSet<int> selectedChoiceIds,
        HashSet<int> correctChoiceIds)
    {
        if (selectedChoiceIds.Count == 0)
            return 0;

        // Single-correct: all-or-nothing
        if (question.QuestionType == 1)
            return selectedChoiceIds.SetEquals(correctChoiceIds) ? question.Score : 0;

        // Multiple-correct: start from full score, then deduct
        // 0.5 per wrong selection, 0.75 per missed correct option
        var wrongSelections = selectedChoiceIds.Count(id => !correctChoiceIds.Contains(id));
        var missedCorrect = correctChoiceIds.Count(id => !selectedChoiceIds.Contains(id));
        var marks = question.Score - (wrongSelections * 0.5m) - (missedCorrect * 0.75m);
        return Math.Max(0, Math.Round(marks, 2));
    }

    private async Task<string?> EnsureStudentCanAttemptQuizAsync(Quiz quiz)
    {
        var userId = GetCurrentStudentUserId();
        if (!userId.HasValue)
            return "Please sign in to attempt a quiz.";

        var parentId = quiz.ParentQuizId ?? quiz.QuizId;
        var familyHasAssignments = await _context.QuizSetAssignments
            .AnyAsync(a => a.ParentQuizId == parentId);

        if (familyHasAssignments)
        {
            var isAssigned = await _context.QuizSetAssignments
                .AnyAsync(a => a.QuizId == quiz.QuizId && a.UserId == userId.Value);

            return isAssigned
                ? null
                : "You are not assigned to this quiz set.";
        }

        // Multi-set quizzes must be assigned by the teacher before students can attempt them.
        var hasArrangements = await _context.Quizzes.AnyAsync(q => q.ParentQuizId == parentId);
        if (hasArrangements)
            return "This quiz has multiple sets. Ask your teacher to assign a set to you.";

        // Single-set quiz with no assignment rows: only the main quiz is attemptable.
        if (quiz.ParentQuizId.HasValue)
            return "This quiz set is not available.";

        return null;
    }

    private async Task<string> GetStudentFacingQuizTitleAsync(Quiz quiz)
    {
        if (!quiz.ParentQuizId.HasValue)
            return quiz.Title ?? "Quiz";

        var parentTitle = await _context.Quizzes
            .AsNoTracking()
            .Where(q => q.QuizId == quiz.ParentQuizId.Value)
            .Select(q => q.Title)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(parentTitle) ? (quiz.Title ?? "Quiz") : parentTitle;
    }

    private static int GetPerformanceStatus(decimal percentage) => percentage switch
    {
        >= 90 => 3,
        >= 70 => 2,
        >= 50 => 1,
        _ => 0
    };

    private static string GetPerformanceLabel(int status) => status switch
    {
        3 => "Excellent",
        2 => "Good",
        1 => "Pass",
        _ => "Needs Improvement"
    };

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

    private int? GetCurrentStudentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private string? GetCurrentStudentDisplayName()
    {
        return User.FindFirstValue("FullName")
            ?? User.Identity?.Name;
    }
}
