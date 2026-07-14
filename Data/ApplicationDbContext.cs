using Microsoft.EntityFrameworkCore;
using quizportal.Models;

namespace quizportal.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<QuizInfo> QuizInfos => Set<QuizInfo>();
    }
}
