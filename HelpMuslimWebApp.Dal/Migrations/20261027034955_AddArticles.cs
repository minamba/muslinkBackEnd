using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    cover_image_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    thumbnail_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reading_minutes = table.Column<int>(type: "integer", nullable: true),
                    author_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    updated_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_category",
                table: "article",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_article_published_at",
                table: "article",
                column: "published_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article");
        }
    }
}
