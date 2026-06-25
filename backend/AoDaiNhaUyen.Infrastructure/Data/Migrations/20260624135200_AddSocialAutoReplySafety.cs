using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAutoReplySafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_reply_ignore_before",
                table: "social_account_connections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "social_auto_reply_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    window_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_message_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    message_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    message_count = table.Column<int>(type: "integer", nullable: false),
                    reply_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_auto_reply_batches", x => x.id);
                    table.CheckConstraint("ck_social_auto_reply_batches_status", "status IN ('pending','processing','queued','replied','cancelled','failed')");
                });

            migrationBuilder.CreateTable(
                name: "social_automation_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    initialized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_automation_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "social_webhook_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "zernio"),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    thread_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "incoming"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reply_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    reply_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    raw_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_webhook_receipts", x => x.id);
                    table.CheckConstraint("ck_social_webhook_receipts_reply_status", "reply_status IN ('pending','skipped','batched','replied','failed')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_social_auto_reply_batches_active_unique",
                table: "social_auto_reply_batches",
                columns: new[] { "platform", "account_id", "conversation_id" },
                unique: true,
                filter: "status IN ('pending','processing','queued') AND NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_auto_reply_batches_status_window",
                table: "social_auto_reply_batches",
                columns: new[] { "status", "window_ends_at" });

            migrationBuilder.CreateIndex(
                name: "idx_social_automation_states_key_unique",
                table: "social_automation_states",
                column: "key",
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_webhook_receipts_event_unique",
                table: "social_webhook_receipts",
                columns: new[] { "provider", "event_type", "external_event_id" },
                unique: true,
                filter: "external_event_id IS NOT NULL AND NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_webhook_receipts_message_unique",
                table: "social_webhook_receipts",
                columns: new[] { "platform", "account_id", "message_id" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_webhook_receipts_status_received",
                table: "social_webhook_receipts",
                columns: new[] { "reply_status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "idx_social_webhook_receipts_thread_received",
                table: "social_webhook_receipts",
                columns: new[] { "platform", "account_id", "thread_id", "received_at" });

            migrationBuilder.Sql("""
                INSERT INTO social_automation_states (id, key, initialized_at, created_at, updated_at, is_deleted, is_active)
                VALUES ('00000000-0000-0000-0000-000000000001', 'global', NOW(), NOW(), NOW(), false, true)
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_auto_reply_batches");

            migrationBuilder.DropTable(
                name: "social_automation_states");

            migrationBuilder.DropTable(
                name: "social_webhook_receipts");

            migrationBuilder.DropColumn(
                name: "auto_reply_ignore_before",
                table: "social_account_connections");
        }
    }
}
