using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHermesEventOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hermes_event_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "pending"),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    locked_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hermes_event_outbox", x => x.id);
                    table.CheckConstraint("ck_hermes_event_outbox_status", "status IN ('pending','processing','completed','failed','dead','cancelled')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_event_outbox_aggregate",
                table: "hermes_event_outbox",
                columns: new[] { "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_event_outbox_correlation_id",
                table: "hermes_event_outbox",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_hermes_event_outbox_status_schedule",
                table: "hermes_event_outbox",
                columns: new[] { "status", "scheduled_at", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_hermes_event_outbox_type_occurred_at",
                table: "hermes_event_outbox",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_hermes_event_outbox_idempotency_key",
                table: "hermes_event_outbox",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hermes_event_outbox");
        }
    }
}
