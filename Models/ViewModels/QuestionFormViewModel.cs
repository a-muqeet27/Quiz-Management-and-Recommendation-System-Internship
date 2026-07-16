using Microsoft.AspNetCore.Mvc.Rendering;

namespace quizportal.Models.ViewModels;

public class QuestionChoiceInput
{
    public int ChoiceId { get; set; }

    public string ChoiceText { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}

public class QuestionFormViewModel
{
    public int QuestionId { get; set; }

    public int SubjectId { get; set; }

    public int? TopicId { get; set; }

    public string QuestionStatement { get; set; } = string.Empty;

    public int QuestionType { get; set; } = 1;

    public int Difficulty { get; set; } = 1;

    public int Score { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public List<QuestionChoiceInput> Choices { get; set; } =
    [
        new(),
        new(),
        new(),
        new()
    ];

    public List<SelectListItem> Subjects { get; set; } = [];

    public List<SelectListItem> Topics { get; set; } = [];
}
