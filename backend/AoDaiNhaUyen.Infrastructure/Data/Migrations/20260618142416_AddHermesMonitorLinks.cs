using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHermesMonitorLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hermes_agent_trace_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_outbox_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "success"),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    safe_payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_agent_trace_steps", x => x.id);
                    table.CheckConstraint("ck_hermes_agent_trace_steps_status", "status IN ('success','failed','running','skipped')");
                    table.ForeignKey(
                        name: "FK_hermes_agent_trace_steps_hermes_event_outbox_event_outbox_id",
                        column: x => x.event_outbox_id,
                        principalTable: "hermes_event_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_hermes_agent_trace_steps_hermes_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "hermes_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "hermes_monitor_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scope_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "event"),
                    scope_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_by_admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    access_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_monitor_links", x => x.id);
                    table.CheckConstraint("ck_hermes_monitor_links_scope_type", "scope_type IN ('event')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_trace_steps_event_started_at",
                table: "hermes_agent_trace_steps",
                columns: new[] { "event_outbox_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_trace_steps_kind_started_at",
                table: "hermes_agent_trace_steps",
                columns: new[] { "kind", "started_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_trace_steps_run_started_at",
                table: "hermes_agent_trace_steps",
                columns: new[] { "run_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_monitor_links_created_by_admin_user_id",
                table: "hermes_monitor_links",
                column: "created_by_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_monitor_links_expires_at",
                table: "hermes_monitor_links",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_monitor_links_scope",
                table: "hermes_monitor_links",
                columns: new[] { "scope_type", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ux_hermes_monitor_links_token_hash",
                table: "hermes_monitor_links",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hermes_agent_trace_steps");

            migrationBuilder.DropTable(
                name: "hermes_monitor_links");
        }
    }
}
