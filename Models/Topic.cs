using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class Topic
{
    public int TopicId { get; set; }

    public int SubjectId { get; set; }

    public string TopicName { get; set; } = null!;

    public string? TopicDescription { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual Subject Subject { get; set; } = null!;
}
