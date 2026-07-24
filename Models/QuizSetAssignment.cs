using System;

namespace quizportal.Models;

/// <summary>
/// Links one quiz set (parent or arrangement) to at most one student for a parent quiz family.
/// UserId null means the set is unassigned.
/// </summary>
public class QuizSetAssignment
{
    public int QuizSetAssignmentId { get; set; }

    public int ParentQuizId { get; set; }

    public int QuizId { get; set; }

    public int? UserId { get; set; }

    public DateTime AssignedDate { get; set; }

    public virtual Quiz ParentQuiz { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User? User { get; set; }
}
