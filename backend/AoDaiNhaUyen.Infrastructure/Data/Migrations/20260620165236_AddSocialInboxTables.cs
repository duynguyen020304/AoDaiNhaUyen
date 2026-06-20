using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialInboxTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "social_inbox_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    post_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    comment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_comment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    author_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    author_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    author_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    author_picture = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    author_is_owner = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    created_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    like_count = table.Column<int>(type: "integer", nullable: false),
                    reply_count = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    can_reply = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false),
                    can_hide = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_inbox_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "social_inbox_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    account_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    profile_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    conversation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    participant_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    participant_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    participant_picture = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_message = table.Column<string>(type: "text", nullable: true),
                    updated_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unread_count = table.Column<int>(type: "integer", nullable: true),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_inbox_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "social_inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sender_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sender_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    direction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "incoming"),
                    text = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attachments_json = table.Column<string>(type: "jsonb", nullable: true),
                    delivery_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "social_inbox_sync_cursors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "facebook"),
                    account_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: ""),
                    profile_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: ""),
                    cursor = table.Column<string>(type: "text", nullable: true),
                    last_success_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_inbox_sync_cursors", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_comments_parent",
                table: "social_inbox_comments",
                column: "parent_comment_id");

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_comments_post_created",
                table: "social_inbox_comments",
                columns: new[] { "platform", "account_id", "post_id", "created_time" });

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_comments_unique",
                table: "social_inbox_comments",
                columns: new[] { "platform", "account_id", "comment_id" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_conversations_account_updated",
                table: "social_inbox_conversations",
                columns: new[] { "platform", "account_id", "updated_time" });

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_conversations_status_updated",
                table: "social_inbox_conversations",
                columns: new[] { "status", "updated_time" });

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_conversations_unique",
                table: "social_inbox_conversations",
                columns: new[] { "platform", "account_id", "conversation_id" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_messages_thread_created",
                table: "social_inbox_messages",
                columns: new[] { "platform", "account_id", "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_messages_unique",
                table: "social_inbox_messages",
                columns: new[] { "platform", "account_id", "message_id" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "idx_social_inbox_sync_cursors_unique",
                table: "social_inbox_sync_cursors",
                columns: new[] { "resource", "platform", "account_id", "profile_id" },
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_inbox_comments");

            migrationBuilder.DropTable(
                name: "social_inbox_conversations");

            migrationBuilder.DropTable(
                name: "social_inbox_messages");

            migrationBuilder.DropTable(
                name: "social_inbox_sync_cursors");
        }
    }
}
