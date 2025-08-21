using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerTypeProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStopped",
                table: "Reviewers");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Reviewers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Reviewers");

            migrationBuilder.AddColumn<bool>(
                name: "IsStopped",
                table: "Reviewers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}