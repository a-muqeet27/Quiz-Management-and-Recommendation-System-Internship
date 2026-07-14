using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using quizportal.Models.Enums;

namespace quizportal.Models
{
    public class QuizInfo
    {
        [Key]
        public int QuizID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public int NoOfQuestions { get; set; }

        public int TimeLimit { get; set; }

        public int TotalMarks { get; set; }

        public int? SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }

        public DifficultyLevel? DifficultyFilter { get; set; }

        public QuestionType? QuestionTypeFilter { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Quiz> QuizAttempts { get; set; } = new List<Quiz>();
    }
}
