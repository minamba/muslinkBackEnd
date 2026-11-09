using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddVimeoVideos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vidéos déjà ajoutées : l'identifiant YouTube est gardé (colonne renommée, plus longue pour « numéro/clé » de Vimeo)
            migrationBuilder.RenameColumn(name: "youtube_id", table: "company_media", newName: "video_id");
            migrationBuilder.AlterColumn<string>(
                name: "video_id",
                table: "company_media",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "video_provider",
                table: "company_media",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "company_media",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql("UPDATE company_media SET video_provider = 'youtube' WHERE kind = 'video' AND video_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM company_media WHERE video_provider = 'vimeo';");
            migrationBuilder.DropColumn(name: "thumbnail_url", table: "company_media");
            migrationBuilder.DropColumn(name: "video_provider", table: "company_media");
            migrationBuilder.AlterColumn<string>(
                name: "video_id",
                table: "company_media",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);
            migrationBuilder.RenameColumn(name: "video_id", table: "company_media", newName: "youtube_id");
        }
    }
}
