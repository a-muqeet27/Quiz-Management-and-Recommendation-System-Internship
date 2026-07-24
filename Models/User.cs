using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public int UserRole { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();

    public virtual ICollection<CommentShare> CommentShares { get; set; } = new List<CommentShare>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<QuizSetAssignment> QuizSetAssignments { get; set; } = new List<QuizSetAssignment>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<UserFavouriteQuestion> UserFavouriteQuestions { get; set; } = new List<UserFavouriteQuestion>();
}
