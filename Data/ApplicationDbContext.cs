using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using quizportal.Models;

namespace quizportal.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Choice> Choices { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<CommentReaction> CommentReactions { get; set; }

    public virtual DbSet<CommentShare> CommentShares { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<QuizAttempt> QuizAttempts { get; set; }

    public virtual DbSet<QuizChoice> QuizChoices { get; set; }

    public virtual DbSet<QuizQuestion> QuizQuestions { get; set; }

    public virtual DbSet<QuizScore> QuizScores { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFavouriteQuestion> UserFavouriteQuestions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Choice>(entity =>
        {
            entity.HasKey(e => e.ChoiceId).HasName("PK__Choices__76F516A6A745BECA");

            entity.HasIndex(e => e.QuestionId, "IX_Choices_QuestionId");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Question).WithMany(p => p.Choices)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_Choices_Questions");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__Comment__C3B4DFCAD532734F");

            entity.ToTable("Comment");

            entity.HasIndex(e => e.ParentCommentId, "IX_Comment_ParentCommentId");

            entity.HasIndex(e => e.QuestionId, "IX_Comment_QuestionId");

            entity.Property(e => e.CommentText).HasMaxLength(1000);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .HasConstraintName("FK_Comment_Parent");

            entity.HasOne(d => d.Question).WithMany(p => p.Comments)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_Question");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comment_User");
        });

        modelBuilder.Entity<CommentReaction>(entity =>
        {
            entity.HasKey(e => e.CommentReactionId).HasName("PK__CommentR__609B106A27DD6185");

            entity.ToTable("CommentReaction");

            entity.HasIndex(e => e.CommentId, "IX_CommentReaction_CommentId");

            entity.HasIndex(e => new { e.CommentId, e.UserId }, "UQ_CommentReaction_UserComment").IsUnique();

            entity.Property(e => e.ReactedDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentReactions)
                .HasForeignKey(d => d.CommentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentReaction_Comment");

            entity.HasOne(d => d.User).WithMany(p => p.CommentReactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentReaction_User");
        });

        modelBuilder.Entity<CommentShare>(entity =>
        {
            entity.HasKey(e => e.CommentShareId).HasName("PK__CommentS__3E51096227A0AD03");

            entity.ToTable("CommentShare");

            entity.HasIndex(e => e.CommentId, "IX_CommentShare_CommentId");

            entity.Property(e => e.SharedDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentShares)
                .HasForeignKey(d => d.CommentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentShare_Comment");

            entity.HasOne(d => d.User).WithMany(p => p.CommentShares)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CommentShare_User");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FAC53CDBD20");

            entity.ToTable("Question");

            entity.HasIndex(e => e.SubjectId, "IX_Question_SubjectId");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Questions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Question_CreatedBy");

            entity.HasOne(d => d.Subject).WithMany(p => p.Questions)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Question_Subject");

            entity.HasOne(d => d.Topic).WithMany(p => p.Questions)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_Question_Topic");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("PK__Quiz__8B42AE8E11604FC6");

            entity.ToTable("Quiz");

            entity.HasIndex(e => e.SubjectId, "IX_Quiz_SubjectId");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Quiz_CreatedBy");

            entity.HasOne(d => d.Subject).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_Subject");

            entity.HasOne(d => d.Topic).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.TopicId)
                .HasConstraintName("FK_Quiz_Topic");

            entity.HasIndex(e => e.ParentQuizId, "IX_Quiz_ParentQuizId");

            entity.HasOne(d => d.ParentQuiz).WithMany(p => p.ArrangementQuizzes)
                .HasForeignKey(d => d.ParentQuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quiz_ParentQuiz");
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.QuizAttemptId).HasName("PK__QuizAtte__F39FDCED336BD230");

            entity.ToTable("QuizAttempt");

            entity.HasIndex(e => e.QuizId, "IX_QuizAttempt_QuizId");

            entity.HasIndex(e => e.UserId, "IX_QuizAttempt_UserId");

            entity.Property(e => e.AttemptDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempt_Quiz");

            entity.HasOne(d => d.User).WithMany(p => p.QuizAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizAttempt_User");
        });

        modelBuilder.Entity<QuizChoice>(entity =>
        {
            entity.HasKey(e => e.QuizChoiceId).HasName("PK__QuizChoi__C1486A37C2366F0A");

            entity.ToTable("QuizChoice");

            entity.HasIndex(e => e.ChoiceId, "IX_QuizChoice_ChoiceId");

            entity.HasIndex(e => e.QuestionId, "IX_QuizChoice_QuestionId");

            entity.HasIndex(e => e.QuizAttemptId, "IX_QuizChoice_QuizAttemptId");

            entity.HasOne(d => d.Choice).WithMany(p => p.QuizChoices)
                .HasForeignKey(d => d.ChoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizChoice_Choice");

            entity.HasOne(d => d.Question).WithMany(p => p.QuizChoices)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizChoice_Question");

            entity.HasOne(d => d.QuizAttempt).WithMany(p => p.QuizChoices)
                .HasForeignKey(d => d.QuizAttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizChoice_Attempt");
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(e => e.QuizQuestionId).HasName("PK__QuizQues__45E34D3E5C9C3779");

            entity.ToTable("QuizQuestion");

            entity.HasIndex(e => e.QuizId, "IX_QuizQuestion_QuizId");

            entity.HasOne(d => d.Question).WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizQuestion_Question");

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizQuestion_Quiz");
        });

        modelBuilder.Entity<QuizScore>(entity =>
        {
            entity.HasKey(e => e.QuizScoreId).HasName("PK__QuizScor__3BC1FA7F042CEEDD");

            entity.ToTable("QuizScore");

            entity.HasIndex(e => e.QuizAttemptId, "IX_QuizScore_QuizAttemptId");

            entity.Property(e => e.QuizPercentage).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.QuizAttempt).WithMany(p => p.QuizScores)
                .HasForeignKey(d => d.QuizAttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuizScore_Attempt");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__Subjects__AC1BA3A8798C1AEA");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Subject_IsActive");
            entity.Property(e => e.SubjectDescription).HasMaxLength(500);
            entity.Property(e => e.SubjectName).HasMaxLength(100);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Subjects_CreatedBy");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__Topic__022E0F5D29CDCC0E");

            entity.ToTable("Topic");

            entity.HasIndex(e => e.SubjectId, "IX_Topic_SubjectId");

            entity.HasIndex(e => new { e.SubjectId, e.TopicName }, "UQ_Subject_Topic").IsUnique();

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TopicDescription).HasMaxLength(500);
            entity.Property(e => e.TopicName).HasMaxLength(100);

            entity.HasOne(d => d.Subject).WithMany(p => p.Topics)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Topic_Subject");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C87541760");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E488F3B083").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105345E34C9E6").IsUnique();

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserFavouriteQuestion>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.QuestionId });

            entity.ToTable("UserFavouriteQuestion");

            entity.HasIndex(e => e.QuestionId, "IX_UserFavourite_QuestionId");

            entity.Property(e => e.MarkedDate).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Question).WithMany(p => p.UserFavouriteQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavouriteQuestion_Question");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavouriteQuestions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavouriteQuestion_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
