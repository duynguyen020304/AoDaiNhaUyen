using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    actor_email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    actor_roles = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    query_string = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    controller_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    action_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    action_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    request_preview = table.Column<string>(type: "text", nullable: true),
                    response_preview = table.Column<string>(type: "text", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    ip_address_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_agent_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_logs_action_created_at",
                table: "admin_audit_logs",
                columns: new[] { "action_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_logs_actor_created_at",
                table: "admin_audit_logs",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_logs_created_success",
                table: "admin_audit_logs",
                columns: new[] { "created_at", "success" });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_logs_entity_created_at",
                table: "admin_audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_admin_audit_logs_status_created_at",
                table: "admin_audit_logs",
                columns: new[] { "status_code", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");
        }
    }
}
