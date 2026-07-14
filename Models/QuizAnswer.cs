using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quizportal.Models
{
    public class QuizAnswer
    {
        [Key]
        public int QuizAnswerId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz Quiz { get; set; } = null!;

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;

        /// Primary selected choice for MCQ. SelectedChoices for Multiple Response.
        public int? SelectedChoiceId { get; set; }

        [ForeignKey(nameof(SelectedChoiceId))]
        public Choice? SelectedChoice { get; set; }

        public bool IsCorrect { get; set; }

        public bool IsAttempted { get; set; }

        public int MarksObtained { get; set; }

        public int TimeSpent { get; set; }

        public ICollection<QuizAnswerChoice> SelectedChoices { get; set; } = new List<QuizAnswerChoice>();
    }
}
