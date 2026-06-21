using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHermesActionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hermes_action_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    path = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    body_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    body_preview = table.Column<string>(type: "text", nullable: true),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    response_preview = table.Column<string>(type: "text", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_action_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_action_audit_event_outbox_id",
                table: "hermes_action_audit",
                column: "event_outbox_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_action_audit_method_path_executed_at",
                table: "hermes_action_audit",
                columns: new[] { "method", "path", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_action_audit_run_executed_at",
                table: "hermes_action_audit",
                columns: new[] { "run_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_action_audit_status_executed_at",
                table: "hermes_action_audit",
                columns: new[] { "response_status", "executed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hermes_action_audit");
        }
    }
}
