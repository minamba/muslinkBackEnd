using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateWishes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "wish_anywhere",
                table: "candidate_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "wish_cities",
                table: "candidate_profile",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "wish_contract_types",
                table: "candidate_profile",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "wish_profession_ids",
                table: "candidate_profile",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "wish_remote",
                table: "candidate_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "wish_salary_min",
                table: "candidate_profile",
                type: "integer",
                nullable: true);

            // Le métier recherché du profil devient le premier métier de « Ce que je cherche »
            migrationBuilder.Sql("update candidate_profile set wish_profession_ids = array[desired_profession_id] where desired_profession_id is not null;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "wish_anywhere",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "wish_cities",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "wish_contract_types",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "wish_profession_ids",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "wish_remote",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "wish_salary_min",
                table: "candidate_profile");
        }
    }
}
