using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGenerationFrequencyFromReviewer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewGenerationFrequensyMiliseconds",
                table: "Reviewers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewGenerationFrequensyMiliseconds",
                table: "Reviewers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}