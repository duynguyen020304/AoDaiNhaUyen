using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPromoAndLlmAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "llm_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admin_action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_generated_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ip_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_agent_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    model = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    operation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    action_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    tool_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    risk_level = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    requires_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    latency_ms = table.Column<long>(type: "bigint", nullable: true),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                    completion_tokens = table.Column<int>(type: "integer", nullable: true),
                    total_tokens = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    prompt_preview_redacted = table.Column<string>(type: "text", nullable: true),
                    completion_preview_redacted = table.Column<string>(type: "text", nullable: true),
                    input_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    output_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    safety_flags_json = table.Column<string>(type: "jsonb", nullable: true),
                    redaction_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    retain_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_llm_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_actor_created_at",
                table: "llm_audit_logs",
                columns: new[] { "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_conversation_id",
                table: "llm_audit_logs",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_created_at",
                table: "llm_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_provider_model",
                table: "llm_audit_logs",
                columns: new[] { "provider", "model" });

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_request_id",
                table: "llm_audit_logs",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_source_created_at",
                table: "llm_audit_logs",
                columns: new[] { "source", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_status_created_at",
                table: "llm_audit_logs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_llm_audit_logs_thread_id",
                table: "llm_audit_logs",
                column: "thread_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_audit_logs");
        }
    }
}
