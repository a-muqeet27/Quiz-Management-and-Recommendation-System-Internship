using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class QuizChoice
{
    public int QuizChoiceId { get; set; }

    public int QuizAttemptId { get; set; }

    public int QuestionId { get; set; }

    public int ChoiceId { get; set; }

    public bool IsSelected { get; set; }

    public bool IsCorrect { get; set; }

    public int MarksObtained { get; set; }

    public int TimeSpent { get; set; }

    public virtual Choice Choice { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual QuizAttempt QuizAttempt { get; set; } = null!;
}
