using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHermesPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hermes_heartbeats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    runner_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    gateway_status = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    active_jobs = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_heartbeats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hermes_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    trigger = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    prompt_preview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    result_preview = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hermes_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "info"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "hermes_agent"),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "open"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_reports", x => x.id);
                    table.CheckConstraint("ck_hermes_reports_severity", "severity IN ('info', 'warning', 'high', 'critical')");
                    table.ForeignKey(
                        name: "FK_hermes_reports_hermes_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "hermes_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_heartbeats_runner_recorded_at",
                table: "hermes_heartbeats",
                columns: new[] { "runner_name", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_reports_correlation_id",
                table: "hermes_reports",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_reports_run_id",
                table: "hermes_reports",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_reports_severity_created_at",
                table: "hermes_reports",
                columns: new[] { "severity", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_reports_status_created_at",
                table: "hermes_reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_reports_type_created_at",
                table: "hermes_reports",
                columns: new[] { "report_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_runs_admin_user_id",
                table: "hermes_runs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_runs_conversation_id",
                table: "hermes_runs",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_runs_status_started_at",
                table: "hermes_runs",
                columns: new[] { "status", "started_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_runs_trigger_started_at",
                table: "hermes_runs",
                columns: new[] { "trigger", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hermes_heartbeats");

            migrationBuilder.DropTable(
                name: "hermes_reports");

            migrationBuilder.DropTable(
                name: "hermes_runs");
        }
    }
}
