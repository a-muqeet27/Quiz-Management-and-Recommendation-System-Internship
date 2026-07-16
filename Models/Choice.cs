using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class Choice
{
    public int ChoiceId { get; set; }

    public int QuestionId { get; set; }

    public string ChoiceText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<QuizChoice> QuizChoices { get; set; } = new List<QuizChoice>();
}
