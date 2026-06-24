using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSocialAutoReplyPendingBatchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_social_auto_reply_batches_active_unique",
                table: "social_auto_reply_batches");

            migrationBuilder.CreateIndex(
                name: "idx_social_auto_reply_batches_active_unique",
                table: "social_auto_reply_batches",
                columns: new[] { "platform", "account_id", "conversation_id" },
                unique: true,
                filter: "status = 'pending' AND NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_social_auto_reply_batches_active_unique",
                table: "social_auto_reply_batches");

            migrationBuilder.CreateIndex(
                name: "idx_social_auto_reply_batches_active_unique",
                table: "social_auto_reply_batches",
                columns: new[] { "platform", "account_id", "conversation_id" },
                unique: true,
                filter: "status IN ('pending','processing','queued') AND NOT is_deleted");
        }
    }
}
