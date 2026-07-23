using Microsoft.AspNetCore.Mvc.Rendering;

namespace quizportal.Models.ViewModels;

public class QuizFormViewModel
{
    public int QuizId { get; set; }

    public int SubjectId { get; set; }

    public int? TopicId { get; set; }

    public string? Title { get; set; }

    public int NoOfQuestions { get; set; } = 10;

    /// <summary>
    /// How many quizzes to create from one random question set.
    /// Each quiz gets the same questions in a different order. Create flow only.
    /// </summary>
    public int ArrangementCount { get; set; } = 1;

    public int TimeLimitMinutes { get; set; } = 15;

    public int TotalMarks { get; set; } = 100;

    public int? DifficultyFilter { get; set; }

    public int? QuestionTypeFilter { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Subjects { get; set; } = [];

    public List<SelectListItem> Topics { get; set; } = [];
}
