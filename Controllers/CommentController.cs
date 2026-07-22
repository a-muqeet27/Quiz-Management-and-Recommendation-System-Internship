using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Controllers;

[Authorize]
public class CommentController : Controller
{
    private readonly ApplicationDbContext _context;

    public CommentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentInputViewModel model)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(model.CommentText))
        {
            TempData["ErrorMessage"] = "Comment cannot be empty.";
            return RedirectToDetails(model.QuestionId);
        }

        var question = await _context.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.QuestionId == model.QuestionId);

        if (question == null)
            return NotFound();

        if (User.IsInRole(AppRoles.StudentName) && !question.IsActive)
            return NotFound();

        if (model.ParentCommentId.HasValue)
        {
            var parentExists = await _context.Comments
                .AnyAsync(c => c.CommentId == model.ParentCommentId
                    && c.QuestionId == model.QuestionId
                    && !c.IsDeleted);

            if (!parentExists)
            {
                TempData["ErrorMessage"] = "The comment you are replying to was not found.";
                return RedirectToDetails(model.QuestionId);
            }
        }

        var comment = new Comment
        {
            QuestionId = model.QuestionId,
            UserId = currentUserId.Value,
            ParentCommentId = model.ParentCommentId,
            CommentText = model.CommentText.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = model.ParentCommentId.HasValue
            ? "Reply posted successfully."
            : "Comment posted successfully.";

        return RedirectToDetails(model.QuestionId, model.ParentCommentId ?? comment.CommentId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CommentEditViewModel model)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(model.CommentText))
        {
            TempData["ErrorMessage"] = "Comment cannot be empty.";
            return RedirectToDetails(model.QuestionId, model.CommentId);
        }

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.CommentId == model.CommentId
                && c.QuestionId == model.QuestionId
                && !c.IsDeleted);

        if (comment == null)
            return NotFound();

        if (comment.UserId != currentUserId.Value)
            return Forbid();

        comment.CommentText = model.CommentText.Trim();
        comment.IsEdited = true;
        comment.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Comment updated.";
        return RedirectToDetails(model.QuestionId, model.CommentId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> React(int commentId, int questionId, int reactionType)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        if (reactionType is not (CommentReactionTypes.Like or CommentReactionTypes.Dislike))
            return BadRequest();

        var commentExists = await _context.Comments
            .AnyAsync(c => c.CommentId == commentId && c.QuestionId == questionId && !c.IsDeleted);

        if (!commentExists)
            return NotFound();

        var existing = await _context.CommentReactions
            .FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == currentUserId.Value);

        if (existing == null)
        {
            _context.CommentReactions.Add(new CommentReaction
            {
                CommentId = commentId,
                UserId = currentUserId.Value,
                ReactionType = reactionType,
                ReactedDate = DateTime.UtcNow
            });
        }
        else if (existing.ReactionType == reactionType)
        {
            _context.CommentReactions.Remove(existing);
        }
        else
        {
            existing.ReactionType = reactionType;
            existing.ReactedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectToDetails(questionId, commentId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int commentId, int questionId, string shareUrl)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        var commentExists = await _context.Comments
            .AnyAsync(c => c.CommentId == commentId && c.QuestionId == questionId && !c.IsDeleted);

        if (!commentExists)
            return NotFound();

        _context.CommentShares.Add(new CommentShare
        {
            CommentId = commentId,
            UserId = currentUserId.Value,
            SharedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Comment link copied to clipboard.";
        TempData["CopiedShareUrl"] = shareUrl;
        return RedirectToDetails(questionId, commentId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int questionId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.CommentId == id && c.QuestionId == questionId && !c.IsDeleted);

        if (comment == null)
            return NotFound();

        var isOwner = comment.UserId == currentUserId.Value;
        var isContentManager = User.IsInRole(AppRoles.TeacherName) || User.IsInRole(AppRoles.AdminName);

        if (!isOwner && !isContentManager)
            return Forbid();

        var now = DateTime.UtcNow;

        var allActiveComments = await _context.Comments
            .Where(c => c.QuestionId == questionId && !c.IsDeleted)
            .Select(c => new { c.CommentId, c.ParentCommentId })
            .ToListAsync();

        var childrenByParent = allActiveComments
            .Where(c => c.ParentCommentId.HasValue)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CommentId).ToList());

        var idsToDelete = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(id);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!idsToDelete.Add(current))
                continue;

            if (childrenByParent.TryGetValue(current, out var childIds))
            {
                foreach (var childId in childIds)
                    stack.Push(childId);
            }
        }

        var toUpdate = await _context.Comments
            .Where(c => c.QuestionId == questionId && idsToDelete.Contains(c.CommentId) && !c.IsDeleted)
            .ToListAsync();

        var reactionsToRemove = await _context.CommentReactions
            .Where(r => idsToDelete.Contains(r.CommentId))
            .ToListAsync();

        if (reactionsToRemove.Count > 0)
            _context.CommentReactions.RemoveRange(reactionsToRemove);

        var sharesToRemove = await _context.CommentShares
            .Where(s => idsToDelete.Contains(s.CommentId))
            .ToListAsync();

        if (sharesToRemove.Count > 0)
            _context.CommentShares.RemoveRange(sharesToRemove);

        foreach (var c in toUpdate)
        {
            c.IsDeleted = true;
            c.ModifiedDate = now;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Comment and its replies removed.";
        return RedirectToDetails(questionId);
    }

    private IActionResult RedirectToDetails(int questionId, int? commentId = null)
    {
        var fragment = commentId.HasValue ? $"#comment-{commentId.Value}" : string.Empty;

        if (User.IsInRole(AppRoles.StudentName))
            return Redirect($"{Url.Action("Details", "StudentQuestion", new { id = questionId })}{fragment}");

        return Redirect($"{Url.Action("Details", "Question", new { id = questionId })}{fragment}");
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
