using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quizportal.Migrations
{
    /// <inheritdoc />
    public partial class FixQuizInfoPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NumberOfQuestions",
                table: "QuizInfos",
                newName: "NoOfQuestions");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "QuizInfos",
                newName: "QuizID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NoOfQuestions",
                table: "QuizInfos",
                newName: "NumberOfQuestions");

            migrationBuilder.RenameColumn(
                name: "QuizID",
                table: "QuizInfos",
                newName: "Id");
        }
    }
}
