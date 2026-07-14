using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quizportal.Models
{
    
    public class QuizQuestion
    {
        [Key]
        public int QuizQuestionId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz Quiz { get; set; } = null!;

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;

        public int DisplayOrder { get; set; }
    }
}
