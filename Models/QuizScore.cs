using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class QuizScore
{
    public int QuizScoreId { get; set; }

    public int QuizAttemptId { get; set; }

    public decimal TotalMarks { get; set; }

    public decimal ObtainedMarks { get; set; }

    public decimal QuizPercentage { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Unattempted { get; set; }

    public int PerformanceStatus { get; set; }

    public virtual QuizAttempt QuizAttempt { get; set; } = null!;
}
