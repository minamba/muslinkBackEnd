using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateContactVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_email_visible",
                table: "candidate_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_phone_visible",
                table: "candidate_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_email_visible",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "is_phone_visible",
                table: "candidate_profile");
        }
    }
}
