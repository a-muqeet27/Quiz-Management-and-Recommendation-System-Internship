using System.ComponentModel.DataAnnotations;

namespace quizportal.Models.ViewModels;

public class QuestionDetailViewModel
{
    public Question Question { get; set; } = null!;

    public bool ShowCorrectAnswers { get; set; }

    public List<CommentThreadViewModel> Comments { get; set; } = [];

    public CommentInputViewModel NewComment { get; set; } = new();
}

public class CommentThreadViewModel
{
    public int CommentId { get; set; }

    public int QuestionId { get; set; }

    public int UserId { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string AuthorRole { get; set; } = string.Empty;

    public string CommentText { get; set; } = string.Empty;

    public bool IsEdited { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsOwnComment { get; set; }

    public int LikeCount { get; set; }

    public int DislikeCount { get; set; }

    public int ShareCount { get; set; }

    public int? CurrentUserReaction { get; set; }

    public List<CommentThreadViewModel> Replies { get; set; } = [];

    public static int CountAll(IEnumerable<CommentThreadViewModel> comments) =>
        comments.Sum(c => 1 + CountAll(c.Replies));
}

public class CommentInputViewModel
{
    public int QuestionId { get; set; }

    public int? ParentCommentId { get; set; }

    [Required(ErrorMessage = "Comment cannot be empty.")]
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string CommentText { get; set; } = string.Empty;
}

public class CommentEditViewModel
{
    public int CommentId { get; set; }

    public int QuestionId { get; set; }

    [Required(ErrorMessage = "Comment cannot be empty.")]
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string CommentText { get; set; } = string.Empty;
}
