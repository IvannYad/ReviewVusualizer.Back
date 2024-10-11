using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewVisualizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewGenerationFrequensyMiliseconds = table.Column<int>(type: "int", nullable: false),
                    TeachingQualityMinGrage = table.Column<int>(type: "int", nullable: false),
                    TeachingQualityMaxGrage = table.Column<int>(type: "int", nullable: false),
                    StudentsSupportMinGrage = table.Column<int>(type: "int", nullable: false),
                    StudentsSupportMaxGrage = table.Column<int>(type: "int", nullable: false),
                    CommunicationMinGrage = table.Column<int>(type: "int", nullable: false),
                    CommunicationMaxGrage = table.Column<int>(type: "int", nullable: false),
                    IsStopped = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviewers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewerTeacher",
                columns: table => new
                {
                    ReviewersId = table.Column<int>(type: "int", nullable: false),
                    TeachersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewerTeacher", x => new { x.ReviewersId, x.TeachersId });
                    table.ForeignKey(
                        name: "FK_ReviewerTeacher_Reviewers_ReviewersId",
                        column: x => x.ReviewersId,
                        principalTable: "Reviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewerTeacher_Teachers_TeachersId",
                        column: x => x.TeachersId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewerTeacher_TeachersId",
                table: "ReviewerTeacher",
                column: "TeachersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewerTeacher");

            migrationBuilder.DropTable(
                name: "Reviewers");
        }
    }
}
