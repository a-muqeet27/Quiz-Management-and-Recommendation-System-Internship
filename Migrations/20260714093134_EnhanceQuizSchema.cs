using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quizportal.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceQuizSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Subjects_SubjectId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAnswers_Choices_SelectedChoiceId",
                table: "QuizAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Subjects_SubjectId",
                table: "Quizzes");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Subjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DifficultyFilter",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuestionTypeFilter",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuizTemplateId",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedQuestionCount",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SavedAt",
                table: "Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Quizzes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuizInfos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "QuizInfos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DifficultyFilter",
                table: "QuizInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionTypeFilter",
                table: "QuizInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "QuizInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Questions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuizAnswerChoices",
                columns: table => new
                {
                    QuizAnswerChoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizAnswerId = table.Column<int>(type: "int", nullable: false),
                    ChoiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswerChoices", x => x.QuizAnswerChoiceId);
                    table.ForeignKey(
                        name: "FK_QuizAnswerChoices_Choices_ChoiceId",
                        column: x => x.ChoiceId,
                        principalTable: "Choices",
                        principalColumn: "ChoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizAnswerChoices_QuizAnswers_QuizAnswerId",
                        column: x => x.QuizAnswerId,
                        principalTable: "QuizAnswers",
                        principalColumn: "QuizAnswerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    QuizQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.QuizQuestionId);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "QuizId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_QuizTemplateId",
                table: "Quizzes",
                column: "QuizTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizInfos_SubjectId",
                table: "QuizInfos",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswerChoices_ChoiceId",
                table: "QuizAnswerChoices",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswerChoices_QuizAnswerId_ChoiceId",
                table: "QuizAnswerChoices",
                columns: new[] { "QuizAnswerId", "ChoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuestionId",
                table: "QuizQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_QuestionId",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "QuestionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Subjects_SubjectId",
                table: "Questions",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAnswers_Choices_SelectedChoiceId",
                table: "QuizAnswers",
                column: "SelectedChoiceId",
                principalTable: "Choices",
                principalColumn: "ChoiceId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizInfos_Subjects_SubjectId",
                table: "QuizInfos",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_QuizInfos_QuizTemplateId",
                table: "Quizzes",
                column: "QuizTemplateId",
                principalTable: "QuizInfos",
                principalColumn: "QuizID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Subjects_SubjectId",
                table: "Quizzes",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Subjects_SubjectId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAnswers_Choices_SelectedChoiceId",
                table: "QuizAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizInfos_Subjects_SubjectId",
                table: "QuizInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_QuizInfos_QuizTemplateId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Subjects_SubjectId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "QuizAnswerChoices");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_QuizTemplateId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizInfos_SubjectId",
                table: "QuizInfos");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DifficultyFilter",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "QuestionTypeFilter",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "QuizTemplateId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "RequestedQuestionCount",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SavedAt",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "QuizInfos");

            migrationBuilder.DropColumn(
                name: "DifficultyFilter",
                table: "QuizInfos");

            migrationBuilder.DropColumn(
                name: "QuestionTypeFilter",
                table: "QuizInfos");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "QuizInfos");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Questions");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuizInfos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Subjects_SubjectId",
                table: "Questions",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAnswers_Choices_SelectedChoiceId",
                table: "QuizAnswers",
                column: "SelectedChoiceId",
                principalTable: "Choices",
                principalColumn: "ChoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Subjects_SubjectId",
                table: "Quizzes",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "SubjectId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
