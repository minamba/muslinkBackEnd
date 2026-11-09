using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_domain",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_domain", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recruiter_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    job_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company_sector = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company_size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    company_description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    logo_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruiter_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profession",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    domain_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profession", x => x.id);
                    table.ForeignKey(
                        name: "FK_profession_job_domain_domain_id",
                        column: x => x.domain_id,
                        principalTable: "job_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    headline = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    situation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    desired_job = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    desired_domain_id = table.Column<int>(type: "integer", nullable: true),
                    desired_profession_id = table.Column<int>(type: "integer", nullable: true),
                    summary = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    photo_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cv_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cv_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    linkedin_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_profile_job_domain_desired_domain_id",
                        column: x => x.desired_domain_id,
                        principalTable: "job_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_candidate_profile_profession_desired_profession_id",
                        column: x => x.desired_profession_id,
                        principalTable: "profession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "job_offer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    domain_id = table.Column<int>(type: "integer", nullable: true),
                    profession_id = table.Column<int>(type: "integer", nullable: true),
                    contract_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_remote = table.Column<bool>(type: "boolean", nullable: true),
                    salary = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_open = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_offer", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_offer_job_domain_domain_id",
                        column: x => x.domain_id,
                        principalTable: "job_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_offer_profession_profession_id",
                        column: x => x.profession_id,
                        principalTable: "profession",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_offer_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_education",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    school = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    degree = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    field_of_study = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_education", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_education_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_experience",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_experience", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_experience_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_language",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    level = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_language", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_language_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skill",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_skill", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_skill_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation", x => x.id);
                    table.ForeignKey(
                        name: "FK_conversation_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversation_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorite",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite", x => x.id);
                    table.ForeignKey(
                        name: "FK_favorite_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_favorite_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_application",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_offer_id = table.Column<int>(type: "integer", nullable: false),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    cover_letter = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_application", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_application_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_application_job_offer_job_offer_id",
                        column: x => x.job_offer_id,
                        principalTable: "job_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conversation_id = table.Column<int>(type: "integer", nullable: false),
                    sender_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    job_application_id = table.Column<int>(type: "integer", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_message_job_application_job_application_id",
                        column: x => x.job_application_id,
                        principalTable: "job_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_education_candidate_profile_id",
                table: "candidate_education",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_experience_candidate_profile_id",
                table: "candidate_experience",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_language_candidate_profile_id",
                table: "candidate_language",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profile_desired_domain_id",
                table: "candidate_profile",
                column: "desired_domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profile_desired_profession_id",
                table: "candidate_profile",
                column: "desired_profession_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profile_user_id",
                table: "candidate_profile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_skill_candidate_profile_id",
                table: "candidate_skill",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_candidate_profile_id",
                table: "conversation",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_recruiter_profile_id_candidate_profile_id",
                table: "conversation",
                columns: new[] { "recruiter_profile_id", "candidate_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_favorite_candidate_profile_id",
                table: "favorite",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_recruiter_profile_id_candidate_profile_id",
                table: "favorite",
                columns: new[] { "recruiter_profile_id", "candidate_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_application_candidate_profile_id",
                table: "job_application",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_application_job_offer_id_candidate_profile_id",
                table: "job_application",
                columns: new[] { "job_offer_id", "candidate_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_domain_name",
                table: "job_domain",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_domain_id",
                table: "job_offer",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_is_open_created_at",
                table: "job_offer",
                columns: new[] { "is_open", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_profession_id",
                table: "job_offer",
                column: "profession_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_recruiter_profile_id",
                table: "job_offer",
                column: "recruiter_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_message_conversation_id_sent_at",
                table: "message",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_message_job_application_id",
                table: "message",
                column: "job_application_id");

            migrationBuilder.CreateIndex(
                name: "IX_profession_domain_id_name",
                table: "profession",
                columns: new[] { "domain_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profile_user_id",
                table: "recruiter_profile",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_education");

            migrationBuilder.DropTable(
                name: "candidate_experience");

            migrationBuilder.DropTable(
                name: "candidate_language");

            migrationBuilder.DropTable(
                name: "candidate_skill");

            migrationBuilder.DropTable(
                name: "favorite");

            migrationBuilder.DropTable(
                name: "message");

            migrationBuilder.DropTable(
                name: "conversation");

            migrationBuilder.DropTable(
                name: "job_application");

            migrationBuilder.DropTable(
                name: "candidate_profile");

            migrationBuilder.DropTable(
                name: "job_offer");

            migrationBuilder.DropTable(
                name: "profession");

            migrationBuilder.DropTable(
                name: "recruiter_profile");

            migrationBuilder.DropTable(
                name: "job_domain");
        }
    }
}
