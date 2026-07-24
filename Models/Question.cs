using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int SubjectId { get; set; }

    public string QuestionStatement { get; set; } = null!;

    public int QuestionType { get; set; }

    public int Difficulty { get; set; }

    public decimal Score { get; set; }

    public bool IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? TopicId { get; set; }

    public virtual ICollection<Choice> Choices { get; set; } = new List<Choice>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<QuizChoice> QuizChoices { get; set; } = new List<QuizChoice>();

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual Topic? Topic { get; set; }

    public virtual ICollection<UserFavouriteQuestion> UserFavouriteQuestions { get; set; } = new List<UserFavouriteQuestion>();
}
