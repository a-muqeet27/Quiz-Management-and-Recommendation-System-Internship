using System.ComponentModel.DataAnnotations;

namespace quizportal.Models.ViewModels;

public class QuizAttemptStartViewModel
{
    public int QuizId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? SubjectName { get; set; }

    public int QuestionCount { get; set; }

    public decimal TotalMarks { get; set; }

    public int TimeLimitMinutes { get; set; }

    [Display(Name = "Your Name")]
    [StringLength(100)]
    public string StudentName { get; set; } = string.Empty;
}

public class QuizAttemptTakeViewModel
{
    public int QuizAttemptId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public int TimeLimitMinutes { get; set; }

    public int RemainingSeconds { get; set; }

    public bool HasTimeLimit => TimeLimitMinutes > 0;

    public List<QuizAttemptQuestionViewModel> Questions { get; set; } = [];
}

public class QuizAttemptQuestionViewModel
{
    public int QuestionId { get; set; }

    public string QuestionStatement { get; set; } = string.Empty;

    public int QuestionType { get; set; }

    public decimal Score { get; set; }

    public int DisplayOrder { get; set; }

    public List<QuizAttemptChoiceViewModel> Choices { get; set; } = [];
}

public class QuizAttemptChoiceViewModel
{
    public int ChoiceId { get; set; }

    public string ChoiceText { get; set; } = string.Empty;
}

public class QuizAttemptSubmitViewModel
{
    public int QuizAttemptId { get; set; }

    public bool TimedOut { get; set; }

    public List<QuizAttemptAnswerViewModel> Answers { get; set; } = [];
}

public class QuizAttemptAnswerViewModel
{
    public int QuestionId { get; set; }

    public List<int> SelectedChoiceIds { get; set; } = [];
}

public class QuizAttemptResultViewModel
{
    public int QuizAttemptId { get; set; }

    public int QuizId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public decimal TotalMarks { get; set; }

    public decimal ObtainedMarks { get; set; }

    public decimal QuizPercentage { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Unattempted { get; set; }

    public string PerformanceLabel { get; set; } = string.Empty;
}

public class QuizAttemptHistoryViewModel
{
    public int QuizAttemptId { get; set; }

    public int QuizId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public DateTime AttemptDate { get; set; }

    public decimal ObtainedMarks { get; set; }

    public decimal TotalMarks { get; set; }

    public decimal QuizPercentage { get; set; }
}

public class QuizAttemptReviewViewModel
{
    public int QuizAttemptId { get; set; }

    public int QuizId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public decimal TotalMarks { get; set; }

    public decimal ObtainedMarks { get; set; }

    public decimal QuizPercentage { get; set; }

    public int CorrectAnswers { get; set; }

    public int WrongAnswers { get; set; }

    public int Unattempted { get; set; }

    public List<QuizAttemptReviewQuestionViewModel> Questions { get; set; } = [];
}

public class QuizAttemptReviewQuestionViewModel
{
    public int QuestionId { get; set; }

    public string QuestionStatement { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public decimal MarksObtained { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<QuizAttemptReviewChoiceViewModel> Choices { get; set; } = [];
}

public class QuizAttemptReviewChoiceViewModel
{
    public int ChoiceId { get; set; }

    public string ChoiceText { get; set; } = string.Empty;

    public bool IsSelected { get; set; }

    public bool IsCorrect { get; set; }
}

public class QuizAttemptDetailsViewModel
{
    public int QuizAttemptId { get; set; }

    public int QuizId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public DateTime AttemptDate { get; set; }

    public decimal TotalMarks { get; set; }

    public decimal ObtainedMarks { get; set; }

    public decimal QuizPercentage { get; set; }

    public List<QuizAttemptDetailsQuestionViewModel> Questions { get; set; } = [];
}

public class QuizAttemptDetailsQuestionViewModel
{
    public string QuestionStatement { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public List<QuizAttemptDetailsChoiceViewModel> Choices { get; set; } = [];
}

public class QuizAttemptDetailsChoiceViewModel
{
    public string ChoiceText { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
