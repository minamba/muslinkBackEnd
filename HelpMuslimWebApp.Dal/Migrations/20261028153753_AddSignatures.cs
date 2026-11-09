using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "signature_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    closing = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    show_job_title = table.Column<bool>(type: "boolean", nullable: false),
                    show_email = table.Column<bool>(type: "boolean", nullable: false),
                    show_website = table.Column<bool>(type: "boolean", nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signature_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signature_assignment",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: true),
                    job_title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signature_assignment", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_signature_assignment_signature_template_template_id",
                        column: x => x.template_id,
                        principalTable: "signature_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_signature_assignment_template_id",
                table: "signature_assignment",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "signature_assignment");

            migrationBuilder.DropTable(
                name: "signature_template");
        }
    }
}
