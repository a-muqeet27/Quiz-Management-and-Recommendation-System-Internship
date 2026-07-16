using Microsoft.AspNetCore.Mvc.Rendering;

namespace quizportal.Models.ViewModels;

public class QuizFormViewModel
{
    public int QuizId { get; set; }

    public int SubjectId { get; set; }

    public int? TopicId { get; set; }

    public string? Title { get; set; }

    public int NoOfQuestions { get; set; } = 10;

    public int TimeLimitMinutes { get; set; } = 15;

    public int TotalMarks { get; set; } = 100;

    public int? DifficultyFilter { get; set; }

    public int? QuestionTypeFilter { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectListItem> Subjects { get; set; } = [];

    public List<SelectListItem> Topics { get; set; } = [];
}
