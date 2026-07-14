using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using quizportal.Models.Enums;

namespace quizportal.Models
{
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Topic { get; set; } = string.Empty;

        [Required]
        public string Statement { get; set; } = string.Empty;

        [Required]
        public QuestionType QuestionType { get; set; }

        [Required]
        public DifficultyLevel Difficulty { get; set; }

        [Range(1, 10)]
        public int Score { get; set; }

        public bool IsFavourite { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Choice> Choices { get; set; } = new List<Choice>();

        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

        public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
    }
}
