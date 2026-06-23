using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHermesFanOutSubBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hermes_fanout_sub_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_batch_index = table.Column<int>(type: "integer", nullable: false),
                    event_count = table.Column<int>(type: "integer", nullable: false),
                    event_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "pending"),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    report_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "mixed"),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "info"),
                    report_preview = table.Column<string>(type: "text", nullable: true),
                    report_text_for_compression = table.Column<string>(type: "text", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_fanout_sub_batches", x => x.id);
                    table.CheckConstraint("ck_hermes_fanout_sub_batches_severity", "severity IN ('info', 'warning', 'high', 'critical')");
                    table.CheckConstraint("ck_hermes_fanout_sub_batches_status", "status IN ('pending','success','failed')");
                    table.ForeignKey(
                        name: "FK_hermes_fanout_sub_batches_hermes_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "hermes_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_trace_steps_run_kind",
                table: "hermes_agent_trace_steps",
                columns: new[] { "run_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_fanout_sub_batches_run_status",
                table: "hermes_fanout_sub_batches",
                columns: new[] { "run_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_hermes_fanout_sub_batches_run_sub_batch",
                table: "hermes_fanout_sub_batches",
                columns: new[] { "run_id", "sub_batch_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hermes_fanout_sub_batches");

            migrationBuilder.DropIndex(
                name: "idx_hermes_trace_steps_run_kind",
                table: "hermes_agent_trace_steps");
        }
    }
}
