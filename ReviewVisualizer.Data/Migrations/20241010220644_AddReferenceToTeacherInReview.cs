using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceToTeacherInReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EvaluationObjectivness",
                table: "Reviews",
                newName: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TeacherId",
                table: "Reviews",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Teachers_TeacherId",
                table: "Reviews",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Teachers_TeacherId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TeacherId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "Reviews",
                newName: "EvaluationObjectivness");
        }
    }
}