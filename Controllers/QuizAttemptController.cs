using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers;

public class QuizAttemptController : Controller
{
    private const string StudentUserIdSessionKey = "StudentUserId";
    private readonly ApplicationDbContext _context;

    public QuizAttemptController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? subjectId = null)
    {
        var query = _context.Quizzes
            .Include(q => q.Subject)
            .Include(q => q.Topic)
            .Include(q => q.QuizQuestions)
            .Where(q => q.IsActive && q.QuizQuestions.Any())
            .AsQueryable();

        if (subjectId.HasValue && subjectId > 0)
            query = query.Where(q => q.SubjectId == subjectId);

        var quizzes = await query
            .OrderBy(q => q.QuizId)
            .ToListAsync();

        await PopulateFilterDropdownsAsync(subjectId);
        ViewBag.RecentAttempts = await LoadRecentAttemptsAsync();
        ViewBag.StudentName = await GetCurrentStudentNameAsync();
        return View(quizzes);
    }

    public async Task<IActionResult> History()
    {
        var attempts = await LoadRecentAttemptsAsync();
        ViewBag.StudentName = await GetCurrentStudentNameAsync();
        return View(attempts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await BuildDetailsViewModelAsync(id);
        if (model == null)
            return NotFound();

        var currentUserId = GetCurrentStudentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Please start a quiz with your name to view saved attempts.";
            return RedirectToAction(nameof(Index));
        }

        var attempt = await _context.QuizAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.QuizAttemptId == id);

        if (attempt == null || attempt.UserId != currentUserId.Value)
        {
            TempData["ErrorMessage"] = "You can only view your own saved attempts.";
            return RedirectToAction(nameof(History));
        }

        return View(model);
    }

    public async Task<IActionResult> Review(int id)
    {
        var currentUserId = GetCurrentStudentUserId();
        if (!currentUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Please start a quiz with your name to review answers.";
            return RedirectToAction(nameof(Index));
        }

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

        var model = new QuizAttemptStartViewModel
        {
            QuizId = quiz.QuizId,
            Title = quiz.Title ?? "Quiz",
            SubjectName = quiz.Subject?.SubjectName,
            QuestionCount = quiz.QuizQuestions.Count,
            TotalMarks = quiz.TotalMarks,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            StudentName = await GetCurrentStudentNameAsync() ?? string.Empty
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

        if (!quiz.IsActive || !quiz.QuizQuestions.Any())
        {
            TempData["ErrorMessage"] = "This quiz cannot be attempted right now.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.Title = quiz.Title ?? "Quiz";
            model.SubjectName = quiz.Subject?.SubjectName;
            model.QuestionCount = quiz.QuizQuestions.Count;
            model.TotalMarks = quiz.TotalMarks;
            model.TimeLimitMinutes = quiz.TimeLimitMinutes;
            return View(model);
        }

        var user = await GetOrCreateStudentUserAsync(model.StudentName.Trim());
        SetCurrentStudentUserId(user.UserId);

        var attempt = new QuizAttempt
        {
            QuizId = quiz.QuizId,
            UserId = user.UserId,
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

        var model = new QuizAttemptTakeViewModel
        {
            QuizAttemptId = attempt.QuizAttemptId,
            QuizTitle = attempt.Quiz?.Title ?? "Quiz",
            TimeLimitMinutes = attempt.Quiz?.TimeLimitMinutes ?? 0,
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
        var obtainedMarks = 0;
        var totalMarks = 0;

        foreach (var quizQuestion in quizQuestions)
        {
            var question = quizQuestion.Question;
            totalMarks += question.Score;

            answersByQuestion.TryGetValue(question.QuestionId, out var selectedChoiceIds);
            selectedChoiceIds ??= [];

            var correctChoiceIds = question.Choices
                .Where(c => c.IsCorrect)
                .Select(c => c.ChoiceId)
                .OrderBy(id => id)
                .ToList();

            var selectedSorted = selectedChoiceIds.OrderBy(id => id).ToList();
            var isAttempted = selectedSorted.Count > 0;
            var isCorrect = isAttempted && selectedSorted.SequenceEqual(correctChoiceIds);

            if (!isAttempted)
                unattemptedCount++;
            else if (isCorrect)
                correctCount++;
            else
                wrongCount++;

            var questionMarks = isCorrect ? question.Score : 0;
            obtainedMarks += questionMarks;

            foreach (var choice in question.Choices)
            {
                var isSelected = selectedChoiceIds.Contains(choice.ChoiceId);
                _context.QuizChoices.Add(new QuizChoice
                {
                    QuizAttemptId = attempt.QuizAttemptId,
                    QuestionId = question.QuestionId,
                    ChoiceId = choice.ChoiceId,
                    IsSelected = isSelected,
                    IsCorrect = choice.IsCorrect,
                    MarksObtained = isSelected && choice.IsCorrect && isCorrect ? question.Score : 0
                });
            }
        }

        var percentage = totalMarks > 0
            ? Math.Round((decimal)obtainedMarks / totalMarks * 100, 2)
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

        TempData["SuccessMessage"] = "Your quiz attempt has been saved.";
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

            var correctIds = questionChoices
                .Where(c => c.IsCorrect)
                .Select(c => c.ChoiceId)
                .OrderBy(x => x)
                .ToList();

            var status = selectedIds.Count == 0
                ? "Unattempted"
                : selectedIds.SequenceEqual(correctIds) ? "Correct" : "Wrong";

            return new QuizAttemptReviewQuestionViewModel
            {
                QuestionId = qq.QuestionId,
                QuestionStatement = qq.Question.QuestionStatement,
                Score = qq.Question.Score,
                MarksObtained = questionChoices.Max(c => c.MarksObtained),
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

    private async Task<User> GetOrCreateStudentUserAsync(string studentName)
    {
        var username = studentName.Trim();
        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (existing != null)
            return existing;

        var user = new User
        {
            Username = username,
            FullName = studentName,
            Email = $"{Guid.NewGuid():N}@guest.local",
            PasswordHash = "guest",
            UserRole = 0,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
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

    private int? GetCurrentStudentUserId() => HttpContext.Session.GetInt32(StudentUserIdSessionKey);

    private void SetCurrentStudentUserId(int userId) =>
        HttpContext.Session.SetInt32(StudentUserIdSessionKey, userId);

    private async Task<string?> GetCurrentStudentNameAsync()
    {
        var userId = GetCurrentStudentUserId();
        if (!userId.HasValue)
            return null;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        return user?.FullName ?? user?.Username;
    }
}
