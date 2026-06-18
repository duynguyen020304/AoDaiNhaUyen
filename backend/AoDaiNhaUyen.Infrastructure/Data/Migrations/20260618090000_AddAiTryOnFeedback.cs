using System;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260618090000_AddAiTryOnFeedback")]
    public partial class AddAiTryOnFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_tryon_feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_generated_image_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    admin_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_tryon_feedbacks", x => x.id);
                    table.CheckConstraint("ck_ai_tryon_feedbacks_rating", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ai_tryon_feedbacks_user_generated_images_user_generated_image_id",
                        column: x => x.user_generated_image_id,
                        principalTable: "user_generated_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_tryon_feedbacks_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ai_tryon_feedbacks_created_at",
                table: "ai_tryon_feedbacks",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_ai_tryon_feedbacks_image_id",
                table: "ai_tryon_feedbacks",
                column: "user_generated_image_id");

            migrationBuilder.CreateIndex(
                name: "idx_ai_tryon_feedbacks_user_id",
                table: "ai_tryon_feedbacks",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_tryon_feedbacks");
        }
    }
}
