using Microsoft.EntityFrameworkCore;
using quizportal.Data;
using quizportal.Models;
using quizportal.Models.ViewModels;

namespace quizportal.Services;

public static class CommentQueryHelper
{
    public static async Task<List<CommentThreadViewModel>> LoadCommentThreadsAsync(
        ApplicationDbContext context,
        int questionId,
        int currentUserId)
    {
        var comments = await context.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.QuestionId == questionId && !c.IsDeleted)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();

        if (comments.Count == 0)
            return [];

        var commentIds = comments.Select(c => c.CommentId).ToList();

        var reactions = await context.CommentReactions
            .AsNoTracking()
            .Where(r => commentIds.Contains(r.CommentId))
            .ToListAsync();

        var shares = await context.CommentShares
            .AsNoTracking()
            .Where(s => commentIds.Contains(s.CommentId))
            .ToListAsync();

        var topLevel = comments
            .Where(c => c.ParentCommentId == null)
            .Select(c => MapComment(c, comments, reactions, shares, currentUserId))
            .ToList();

        return topLevel;
    }

    private static CommentThreadViewModel MapComment(
        Comment comment,
        List<Comment> allComments,
        List<CommentReaction> reactions,
        List<CommentShare> shares,
        int currentUserId)
    {
        var commentReactions = reactions.Where(r => r.CommentId == comment.CommentId).ToList();
        var userReaction = commentReactions.FirstOrDefault(r => r.UserId == currentUserId);

        var thread = new CommentThreadViewModel
        {
            CommentId = comment.CommentId,
            QuestionId = comment.QuestionId,
            UserId = comment.UserId,
            AuthorName = comment.User?.FullName ?? comment.User?.Username ?? "User",
            AuthorRole = AppRoles.ToRoleName(comment.User?.UserRole ?? AppRoles.Student),
            CommentText = comment.CommentText,
            IsEdited = comment.IsEdited,
            CreatedDate = comment.CreatedDate,
            IsOwnComment = comment.UserId == currentUserId,
            LikeCount = commentReactions.Count(r => r.ReactionType == CommentReactionTypes.Like),
            DislikeCount = commentReactions.Count(r => r.ReactionType == CommentReactionTypes.Dislike),
            ShareCount = shares.Count(s => s.CommentId == comment.CommentId),
            CurrentUserReaction = userReaction?.ReactionType,
            Replies = allComments
                .Where(c => c.ParentCommentId == comment.CommentId)
                .Select(c => MapComment(c, allComments, reactions, shares, currentUserId))
                .ToList()
        };

        return thread;
    }
}
