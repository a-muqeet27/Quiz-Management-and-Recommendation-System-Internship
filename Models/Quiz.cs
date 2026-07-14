using quizportal.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quizportal.Models
{
    /// <summary>
    /// A saved quiz attempt with scoring and status.
    /// </summary>
    public class Quiz
    {
        [Key]
        public int QuizId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;

        /// <summary>
        /// Optional link to the quiz template that generated this attempt.
        /// </summary>
        public int? QuizTemplateId { get; set; }

        [ForeignKey(nameof(QuizTemplateId))]
        public QuizInfo? QuizTemplate { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        public DateTime? SavedAt { get; set; }

        public bool IsCompleted { get; set; }

        public int RequestedQuestionCount { get; set; }

        public int TotalQuestions { get; set; }

        public int TotalMarks { get; set; }

        public int ObtainedMarks { get; set; }

        public double Percentage { get; set; }

        public QuizStatus Status { get; set; }

        public int TimeLimitMinutes { get; set; }

        public int TimeTaken { get; set; }

        public DifficultyLevel? DifficultyFilter { get; set; }

        public QuestionType? QuestionTypeFilter { get; set; }

        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

        public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }
}
