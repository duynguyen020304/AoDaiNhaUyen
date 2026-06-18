using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260618094000_EnsureAiTryOnFeedbackTable")]
    public partial class EnsureAiTryOnFeedbackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS ai_tryon_feedbacks (
                    id uuid NOT NULL,
                    user_generated_image_id uuid NOT NULL,
                    user_id uuid NULL,
                    guest_key_hash character varying(128) NULL,
                    rating integer NOT NULL,
                    comment character varying(1000) NULL,
                    admin_note character varying(1000) NULL,
                    is_resolved boolean NOT NULL DEFAULT false,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    is_deleted boolean NOT NULL DEFAULT false,
                    is_active boolean NOT NULL DEFAULT true,
                    deleted_at timestamp with time zone NULL,
                    CONSTRAINT "PK_ai_tryon_feedbacks" PRIMARY KEY (id)
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'ck_ai_tryon_feedbacks_rating'
                    ) THEN
                        ALTER TABLE ai_tryon_feedbacks
                        ADD CONSTRAINT ck_ai_tryon_feedbacks_rating CHECK (rating BETWEEN 1 AND 5);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ai_tryon_feedbacks_user_generated_images_user_generated_image_id'
                    ) THEN
                        ALTER TABLE ai_tryon_feedbacks
                        ADD CONSTRAINT "FK_ai_tryon_feedbacks_user_generated_images_user_generated_image_id"
                        FOREIGN KEY (user_generated_image_id) REFERENCES user_generated_images (id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ai_tryon_feedbacks_users_user_id'
                    ) THEN
                        ALTER TABLE ai_tryon_feedbacks
                        ADD CONSTRAINT "FK_ai_tryon_feedbacks_users_user_id"
                        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS idx_ai_tryon_feedbacks_created_at ON ai_tryon_feedbacks (created_at);
                CREATE INDEX IF NOT EXISTS idx_ai_tryon_feedbacks_image_id ON ai_tryon_feedbacks (user_generated_image_id);
                CREATE INDEX IF NOT EXISTS idx_ai_tryon_feedbacks_user_id ON ai_tryon_feedbacks (user_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS ai_tryon_feedbacks;");
        }
    }
}
