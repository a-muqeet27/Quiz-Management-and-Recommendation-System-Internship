using System.ComponentModel.DataAnnotations;

namespace quizportal.Models
{
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Question> Questions { get; set; } = new List<Question>();

        public ICollection<QuizInfo> QuizTemplates { get; set; } = new List<QuizInfo>();
    }
}
