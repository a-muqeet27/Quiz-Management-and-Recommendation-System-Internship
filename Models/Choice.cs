using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quizportal.Models
{
    public class Choice
    {
        [Key]
        public int ChoiceId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;

        [Required]
        public string ChoiceText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public ICollection<QuizAnswerChoice> QuizAnswerChoices { get; set; } = new List<QuizAnswerChoice>();
    }
}
