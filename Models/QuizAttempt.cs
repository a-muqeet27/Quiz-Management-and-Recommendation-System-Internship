using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class QuizAttempt
{
    public int QuizAttemptId { get; set; }

    public int QuizId { get; set; }

    public int UserId { get; set; }

    public DateTime AttemptDate { get; set; }

    public DateTime? SavedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int QuizStatus { get; set; }

    public int? TimeTaken { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<QuizChoice> QuizChoices { get; set; } = new List<QuizChoice>();

    public virtual ICollection<QuizScore> QuizScores { get; set; } = new List<QuizScore>();

    public virtual User User { get; set; } = null!;
}
