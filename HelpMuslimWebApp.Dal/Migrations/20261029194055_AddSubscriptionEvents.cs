using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_event",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    audience = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    subscription_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    plan = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    granted_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    backfilled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_event", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_event_audience_occurred_at",
                table: "subscription_event",
                columns: new[] { "audience", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_event_audience_profile_id",
                table: "subscription_event",
                columns: new[] { "audience", "profile_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_event");
        }
    }
}
