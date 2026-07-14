using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quizportal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubjectQuestionDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Questions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Subjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Subjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Questions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Questions",
                type: "datetime2",
                nullable: true);
        }
    }
}
