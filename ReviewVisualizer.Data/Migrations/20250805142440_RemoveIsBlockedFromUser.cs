using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsBlockedFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blocked",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Blocked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}