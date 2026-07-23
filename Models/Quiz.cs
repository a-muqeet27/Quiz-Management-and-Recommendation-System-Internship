using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class Quiz
{
    public int QuizId { get; set; }

    public int SubjectId { get; set; }

    public string? Title { get; set; }

    public int NoOfQuestions { get; set; }

    public int TimeLimitMinutes { get; set; }

    public int TotalMarks { get; set; }

    public int? DifficultyFilter { get; set; }

    public int? QuestionTypeFilter { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? TopicId { get; set; }

    /// <summary>
    /// When set, this quiz is an arrangement (same questions, different order)
    /// of the parent quiz and is hidden from the main quiz list.
    /// </summary>
    public int? ParentQuizId { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Quiz? ParentQuiz { get; set; }

    public virtual ICollection<Quiz> ArrangementQuizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual Topic? Topic { get; set; }
}
