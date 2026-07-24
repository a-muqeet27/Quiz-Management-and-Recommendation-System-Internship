using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;
using quizportal.Services;

namespace quizportal.Controllers;

[Authorize(Roles = AppRoles.StudentName)]
public class StudentQuestionController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentQuestionController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? subjectId = null, int? topicId = null)
    {
        var query = _context.Questions
            .Include(q => q.Subject)
            .Include(q => q.Topic)
            .Include(q => q.Choices)
            .Include(q => q.Comments)
            .Where(q => q.IsActive)
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

    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        var question = await _context.Questions
            .Include(q => q.Subject)
            .Include(q => q.Topic)
            .Include(q => q.Choices)
            .FirstOrDefaultAsync(q => q.QuestionId == id && q.IsActive);

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

    private async Task PopulateFilterDropdownsAsync(int? selectedSubjectId, int? selectedTopicId)
    {
        var subjects = await _context.Subjects
            .Where(s => s.IsActive)
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

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
