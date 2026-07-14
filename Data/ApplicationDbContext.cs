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
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Choice> Choices => Set<Choice>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
        public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
        public DbSet<QuizAnswerChoice> QuizAnswerChoices => Set<QuizAnswerChoice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<QuizQuestion>()
                .HasIndex(qq => new { qq.QuizId, qq.QuestionId })
                .IsUnique();

            modelBuilder.Entity<QuizAnswerChoice>()
                .HasIndex(qac => new { qac.QuizAnswerId, qac.ChoiceId })
                .IsUnique();

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Subject)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Choice>()
                .HasOne(c => c.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizInfo>()
                .HasOne(q => q.Subject)
                .WithMany(s => s.QuizTemplates)
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Subject)
                .WithMany()
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.QuizTemplate)
                .WithMany(t => t.QuizAttempts)
                .HasForeignKey(q => q.QuizTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Question)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.QuizAnswers)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.Question)
                .WithMany(q => q.QuizAnswers)
                .HasForeignKey(qa => qa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.SelectedChoice)
                .WithMany()
                .HasForeignKey(qa => qa.SelectedChoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QuizAnswerChoice>()
                .HasOne(qac => qac.QuizAnswer)
                .WithMany(qa => qa.SelectedChoices)
                .HasForeignKey(qac => qac.QuizAnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswerChoice>()
                .HasOne(qac => qac.Choice)
                .WithMany(c => c.QuizAnswerChoices)
                .HasForeignKey(qac => qac.ChoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
