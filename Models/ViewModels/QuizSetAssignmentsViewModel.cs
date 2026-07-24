namespace quizportal.Models.ViewModels;

public class QuizSetAssignmentsViewModel
{
    public int ParentQuizId { get; set; }

    public string QuizTitle { get; set; } = string.Empty;

    public int TotalSets { get; set; }

    public int TotalStudents { get; set; }

    public int AssignedCount { get; set; }

    public List<QuizSetAssignmentRowViewModel> SetAssignments { get; set; } = [];

    public List<UnassignedStudentViewModel> UnassignedStudents { get; set; } = [];
}

public class QuizSetAssignmentRowViewModel
{
    public int QuizId { get; set; }

    public int SetNumber { get; set; }

    public string SetTitle { get; set; } = string.Empty;

    public bool IsMainSet { get; set; }

    public int? StudentUserId { get; set; }

    public string? StudentName { get; set; }

    public string Status => StudentUserId.HasValue ? "Assigned" : "Unassigned";
}

public class UnassignedStudentViewModel
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
}
