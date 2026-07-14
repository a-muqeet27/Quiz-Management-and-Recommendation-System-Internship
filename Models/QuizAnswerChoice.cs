using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quizportal.Models
{
    public class QuizAnswerChoice
    {
        [Key]
        public int QuizAnswerChoiceId { get; set; }

        [Required]
        public int QuizAnswerId { get; set; }

        [ForeignKey(nameof(QuizAnswerId))]
        public QuizAnswer QuizAnswer { get; set; } = null!;

        [Required]
        public int ChoiceId { get; set; }

        [ForeignKey(nameof(ChoiceId))]
        public Choice Choice { get; set; } = null!;
    }
}
