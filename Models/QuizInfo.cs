using System.ComponentModel.DataAnnotations;

namespace quizportal.Models
{
    public class QuizInfo
    {
        [Key]
        public int QuizID { get; set; }
        public string Title { get; set; } 
        public int NoOfQuestions { get; set; }
        public int TimeLimit { get; set; }
        public int TotalMarks { get; set; }
    }
}
