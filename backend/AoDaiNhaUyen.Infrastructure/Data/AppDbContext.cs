using AoDaiNhaUyen.Domain.Common;
using AoDaiNhaUyen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net;

namespace AoDaiNhaUyen.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Role> Roles => Set<Role>();
  public DbSet<User> Users => Set<User>();
  public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
  public DbSet<UserRole> UserRoles => Set<UserRole>();
  public DbSet<UserSession> UserSessions => Set<UserSession>();
  public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
  public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
  public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
  public DbSet<MeasurementProfile> MeasurementProfiles => Set<MeasurementProfile>();
  public DbSet<Category> Categories => Set<Category>();
  public DbSet<Product> Products => Set<Product>();
  public DbSet<Collection> Collections => Set<Collection>();
  public DbSet<CollectionProduct> CollectionProducts => Set<CollectionProduct>();
  public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
  public DbSet<ProductImage> ProductImages => Set<ProductImage>();
  public DbSet<StyleScenario> StyleScenarios => Set<StyleScenario>();
  public DbSet<ProductStyleProfile> ProductStyleProfiles => Set<ProductStyleProfile>();
  public DbSet<ProductScenario> ProductScenarios => Set<ProductScenario>();
  public DbSet<ProductPairing> ProductPairings => Set<ProductPairing>();
  public DbSet<ProductAiAsset> ProductAiAssets => Set<ProductAiAsset>();
  public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
  public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
  public DbSet<ChatAttachment> ChatAttachments => Set<ChatAttachment>();
  public DbSet<ChatThreadMemory> ChatThreadMemories => Set<ChatThreadMemory>();
  public DbSet<Cart> Carts => Set<Cart>();
  public DbSet<CartItem> CartItems => Set<CartItem>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderItem> OrderItems => Set<OrderItem>();
  public DbSet<Payment> Payments => Set<Payment>();
  public DbSet<Shipment> Shipments => Set<Shipment>();
  public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
  public DbSet<OrderPromoCode> OrderPromoCodes => Set<OrderPromoCode>();
  public DbSet<Review> Reviews => Set<Review>();
  public DbSet<Comment> Comments => Set<Comment>();
  public DbSet<ImageValidationCacheEntry> ImageValidationCacheEntries => Set<ImageValidationCacheEntry>();
  public DbSet<UserGeneratedImage> UserGeneratedImages => Set<UserGeneratedImage>();
  public DbSet<AiTryOnFeedback> AiTryOnFeedbacks => Set<AiTryOnFeedback>();
  public DbSet<AdminAiAction> AdminAiActions => Set<AdminAiAction>();
  public DbSet<ToolRiskConfig> ToolRiskConfigs => Set<ToolRiskConfig>();
  public DbSet<LlmAuditLog> LlmAuditLogs => Set<LlmAuditLog>();
  public DbSet<OrderAttribution> OrderAttributions => Set<OrderAttribution>();
  public DbSet<CustomerEvent> CustomerEvents => Set<CustomerEvent>();
  public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
  public DbSet<EmailJob> EmailJobs => Set<EmailJob>();
  public DbSet<EmailSendLog> EmailSendLogs => Set<EmailSendLog>();
  public DbSet<Subscriber> Subscribers => Set<Subscriber>();
  public DbSet<MarketingConsent> MarketingConsents => Set<MarketingConsent>();
  public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
  public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
  public DbSet<BlogImage> BlogImages => Set<BlogImage>();
  public DbSet<HermesReport> HermesReports => Set<HermesReport>();
  public DbSet<HermesRun> HermesRuns => Set<HermesRun>();
  public DbSet<HermesHeartbeat> HermesHeartbeats => Set<HermesHeartbeat>();
  public DbSet<HermesEventOutbox> HermesEventOutbox => Set<HermesEventOutbox>();
  public DbSet<HermesMonitorLink> HermesMonitorLinks => Set<HermesMonitorLink>();
  public DbSet<HermesAgentTraceStep> HermesAgentTraceSteps => Set<HermesAgentTraceStep>();
  public DbSet<HermesActionAudit> HermesActionAudits => Set<HermesActionAudit>();
  public DbSet<SocialAccountConnection> SocialAccountConnections => Set<SocialAccountConnection>();
  public DbSet<SocialInboxConversation> SocialInboxConversations => Set<SocialInboxConversation>();
  public DbSet<SocialInboxMessage> SocialInboxMessages => Set<SocialInboxMessage>();
  public DbSet<SocialInboxComment> SocialInboxComments => Set<SocialInboxComment>();
  public DbSet<SocialInboxSyncCursor> SocialInboxSyncCursors => Set<SocialInboxSyncCursor>();
  public DbSet<FacebookPageConnection> FacebookPageConnections => Set<FacebookPageConnection>();
  public DbSet<OrderPromoCostSnapshot> OrderPromoCostSnapshots => Set<OrderPromoCostSnapshot>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasPostgresEnum("public", "order_status", new[]
    {
      "pending", "confirmed", "processing", "shipping", "completed", "cancelled", "returned"
    });

    modelBuilder.HasPostgresEnum("public", "shipping_status", new[]
    {
      "pending", "packed", "shipped", "delivered", "failed", "returned"
    });

    modelBuilder.Entity<Role>(builder =>
    {
      builder.ToTable("roles");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Name).HasMaxLength(30).IsRequired();
      builder.HasIndex(x => x.Name).IsUnique();
    });

    modelBuilder.Entity<User>(builder =>
    {
      builder.ToTable("users");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Email).HasMaxLength(150);
      builder.Property(x => x.Phone).HasMaxLength(20);
      builder.Property(x => x.Gender).HasMaxLength(10);
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active").IsRequired();
      builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
      builder.Property(x => x.IsDeleted).HasDefaultValue(false);
      builder.Property(x => x.IsActive).HasDefaultValue(true);
      builder.Property(x => x.DeletedAt);
      builder.HasIndex(x => x.Email).IsUnique().HasFilter("NOT is_deleted");
      builder.HasIndex(x => x.Phone).IsUnique().HasFilter("NOT is_deleted");
      builder.ToTable(t => t.HasCheckConstraint("ck_users_status", "status IN ('active', 'inactive', 'blocked')"));
    });

    modelBuilder.Entity<UserAccount>(builder =>
    {
      builder.ToTable("user_accounts");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
      builder.Property(x => x.ProviderAccountId).HasMaxLength(255).IsRequired();
      builder.Property(x => x.PasswordHash);
      builder.Property(x => x.IsVerified).HasDefaultValue(false).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Provider, x.ProviderAccountId }).IsUnique();
      builder.HasIndex(x => x.UserId);
      builder.HasOne(x => x.User).WithMany(x => x.UserAccounts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<UserRole>(builder =>
    {
      builder.ToTable("user_roles");
      builder.HasKey(x => x.Id);
      builder.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
      builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<UserSession>(builder =>
    {
      builder.ToTable("user_sessions");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.RefreshTokenHash).IsRequired();
      builder.Property(x => x.IpAddress)
        .HasColumnType("inet")
        .HasConversion(
          value => string.IsNullOrWhiteSpace(value) ? null : IPAddress.Parse(value),
          value => value == null ? null : value.ToString());
      builder.Property(x => x.ExpiresAt).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasOne(x => x.User).WithMany(x => x.Sessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<EmailVerificationToken>(builder =>
    {
      builder.ToTable("email_verification_tokens");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Token).HasMaxLength(255).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Token).IsUnique();
      builder.HasOne(x => x.User).WithMany(x => x.EmailVerificationTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<PasswordResetToken>(builder =>
    {
      builder.ToTable("password_reset_tokens");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Token).HasMaxLength(255).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Token).IsUnique();
      builder.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<UserAddress>(builder =>
    {
      builder.ToTable("user_addresses");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.RecipientName).HasMaxLength(120).IsRequired();
      builder.Property(x => x.RecipientPhone).HasMaxLength(20).IsRequired();
      builder.Property(x => x.Province).HasMaxLength(100).IsRequired();
      builder.Property(x => x.District).HasMaxLength(100).IsRequired();
      builder.Property(x => x.Ward).HasMaxLength(100);
      builder.Property(x => x.AddressLine).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasOne(x => x.User).WithMany(x => x.Addresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<MeasurementProfile>(builder =>
    {
      builder.ToTable("measurement_profiles");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ProfileName).HasMaxLength(100).IsRequired();
      builder.Property(x => x.HeightCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.WeightKg).HasColumnType("numeric(5,2)");
      builder.Property(x => x.BustCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.WaistCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.HipCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.ShoulderCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.SleeveLengthCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.DressLengthCm).HasColumnType("numeric(5,2)");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasOne(x => x.User).WithMany(x => x.MeasurementProfiles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<Category>(builder =>
    {
      builder.ToTable("categories");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Slug).HasMaxLength(150).IsRequired();
      builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();
      builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique();
      builder.HasIndex(x => x.Parent).HasDatabaseName("idx_categories_parent");
      builder.HasOne(x => x.ParentCategory)
        .WithMany(x => x.Children)
        .HasForeignKey(x => x.Parent)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<Product>(builder =>
    {
      builder.ToTable("products");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
      builder.Property(x => x.ProductType).HasMaxLength(30).IsRequired();
      builder.Property(x => x.Material).HasMaxLength(120);
      builder.Property(x => x.Brand).HasMaxLength(120);
      builder.Property(x => x.Origin).HasMaxLength(120);
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("draft").IsRequired();
      builder.Property(x => x.IsPublic).HasDefaultValue(false).IsRequired().HasColumnName("is_public");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique();
      builder.HasIndex(x => x.CategoryId).HasDatabaseName("idx_products_category_id");
      builder.HasIndex(x => x.Status).HasDatabaseName("idx_products_status");
      builder.HasIndex(x => x.ProductType).HasDatabaseName("idx_products_product_type");
      builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
      builder.ToTable(t => t.HasCheckConstraint("ck_products_type", "product_type IN ('ao_dai', 'phu_kien')"));
      builder.ToTable(t => t.HasCheckConstraint("ck_products_status", "status IN ('draft', 'active', 'inactive', 'out_of_stock')"));
    });

    modelBuilder.Entity<Collection>(builder =>
    {
      builder.ToTable("collections");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
      builder.Property(x => x.Description).HasMaxLength(2000);
      builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique();
      builder.HasIndex(x => x.IsPublished).HasDatabaseName("idx_collections_is_published");
      builder.HasIndex(x => x.SortOrder).HasDatabaseName("idx_collections_sort_order");
    });

    modelBuilder.Entity<CollectionProduct>(builder =>
    {
      builder.ToTable("collection_products");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.CollectionId, x.ProductId }).IsUnique();
      builder.HasOne(x => x.Collection).WithMany(x => x.Products).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ProductVariant>(builder =>
    {
      builder.ToTable("product_variants");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
      builder.Property(x => x.VariantName).HasMaxLength(150);
      builder.Property(x => x.Size).HasMaxLength(30);
      builder.Property(x => x.Color).HasMaxLength(50);
      builder.Property(x => x.Price).HasColumnType("numeric(12,2)");
      builder.Property(x => x.SalePrice).HasColumnType("numeric(12,2)");
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active").IsRequired();
      builder.Property(x => x.CostPrice).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.StockQty).HasDefaultValue(0).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Sku).IsUnique();
      builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_product_variants_product_id");
      builder.HasIndex(x => x.StockQty).HasDatabaseName("idx_product_variants_stock_qty");
      builder.HasIndex(x => new { x.ProductId, x.Size, x.Color }).IsUnique();
      builder.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
      builder.ToTable(t => t.HasCheckConstraint("ck_variants_price", "price >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_variants_sale_price", "sale_price IS NULL OR sale_price >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_variants_stock", "stock_qty >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_variants_status", "status IN ('active', 'inactive')"));
    });

    modelBuilder.Entity<ProductImage>(builder =>
    {
      builder.ToTable("product_images");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.AltText).HasMaxLength(255);
      builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();
      builder.Property(x => x.IsPublic).HasDefaultValue(false).IsRequired();
      builder.Property(x => x.PublicObjectKey).HasMaxLength(500);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_product_images_product_id");
      builder.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Variant).WithMany(x => x.Images).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<StyleScenario>(builder =>
    {
      builder.ToTable("style_scenarios");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Description).HasMaxLength(500);
      builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique();
    });

    modelBuilder.Entity<ProductStyleProfile>(builder =>
    {
      builder.ToTable("product_style_profiles");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.StyleKeywordsJsonb).HasColumnType("jsonb");
      builder.Property(x => x.Formality).HasMaxLength(40);
      builder.Property(x => x.Silhouette).HasMaxLength(80);
      builder.Property(x => x.Notes).HasMaxLength(500);
      builder.Property(x => x.PrimaryColorFamily).HasMaxLength(50);
      builder.Property(x => x.SecondaryColorFamily).HasMaxLength(50);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ProductId).IsUnique();
      builder.HasOne(x => x.Product)
        .WithMany(x => x.StyleProfiles)
        .HasForeignKey(x => x.ProductId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ProductScenario>(builder =>
    {
      builder.ToTable("product_scenarios");
      builder.HasKey(x => x.Id);
      builder.HasIndex(x => new { x.ProductId, x.ScenarioId }).IsUnique();
      builder.Property(x => x.Score).HasColumnType("numeric(5,2)");
      builder.Property(x => x.Notes).HasMaxLength(500);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasOne(x => x.Product)
        .WithMany(x => x.Scenarios)
        .HasForeignKey(x => x.ProductId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Scenario)
        .WithMany(x => x.ProductScenarios)
        .HasForeignKey(x => x.ScenarioId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ProductPairing>(builder =>
    {
      builder.ToTable("product_pairings");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Score).HasColumnType("numeric(5,2)");
      builder.Property(x => x.Notes).HasMaxLength(500);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.BaseProductId, x.PairedProductId, x.ScenarioId }).IsUnique();
      builder.HasOne(x => x.BaseProduct)
        .WithMany(x => x.BasePairings)
        .HasForeignKey(x => x.BaseProductId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.PairedProduct)
        .WithMany(x => x.PairedWith)
        .HasForeignKey(x => x.PairedProductId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Scenario)
        .WithMany(x => x.ProductPairings)
        .HasForeignKey(x => x.ScenarioId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<ProductAiAsset>(builder =>
    {
      builder.ToTable("product_ai_assets");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.AssetKind).HasMaxLength(40).IsRequired();
      builder.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
      builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
      builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_product_ai_assets_product_id");
      builder.HasIndex(x => new { x.ProductId, x.AssetKind, x.IsActive }).HasDatabaseName("idx_product_ai_assets_lookup");
      builder.HasOne(x => x.Product)
        .WithMany(x => x.AiAssets)
        .HasForeignKey(x => x.ProductId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Variant)
        .WithMany(x => x.AiAssets)
        .HasForeignKey(x => x.VariantId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<ChatThread>(builder =>
    {
      builder.ToTable("chat_threads");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.GuestKeyHash).HasMaxLength(128);
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active").IsRequired();
      builder.Property(x => x.Source).HasMaxLength(20).HasDefaultValue("web").IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.UserId).HasDatabaseName("idx_chat_threads_user_id");
      builder.HasIndex(x => x.GuestKeyHash).HasDatabaseName("idx_chat_threads_guest_key_hash");
      builder.HasOne(x => x.User)
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<ChatMessage>(builder =>
    {
      builder.ToTable("chat_messages");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Role).HasMaxLength(20).HasDefaultValue("user").IsRequired();
      builder.Property(x => x.Content).IsRequired();
      builder.Property(x => x.Intent).HasMaxLength(50);
      builder.Property(x => x.ClientMessageId).HasMaxLength(80);
      builder.Property(x => x.PromptVersion).HasMaxLength(40);
      builder.Property(x => x.UsageJsonb).HasColumnType("jsonb");
      builder.Property(x => x.FinishReason).HasMaxLength(40);
      builder.Property(x => x.ToolCallsJsonb).HasColumnType("jsonb");
      builder.Property(x => x.StructuredPayloadJsonb).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ThreadId).HasDatabaseName("idx_chat_messages_thread_id");
      builder.HasOne(x => x.Thread)
        .WithMany(x => x.Messages)
        .HasForeignKey(x => x.ThreadId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ChatAttachment>(builder =>
    {
      builder.ToTable("chat_attachments");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Kind).HasMaxLength(40).HasDefaultValue("user_image").IsRequired();
      builder.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
      builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
      builder.Property(x => x.OriginalFileName).HasMaxLength(255);
      builder.Property(x => x.MetadataJsonb).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ThreadId).HasDatabaseName("idx_chat_attachments_thread_id");
      builder.HasOne(x => x.Thread)
        .WithMany(x => x.Attachments)
        .HasForeignKey(x => x.ThreadId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Message)
        .WithMany(x => x.Attachments)
        .HasForeignKey(x => x.MessageId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<ChatThreadMemory>(builder =>
    {
      builder.ToTable("chat_thread_memory");
      builder.HasKey(x => x.Id);
      builder.HasIndex(x => x.ThreadId).IsUnique();
      builder.Property(x => x.Summary).HasMaxLength(2000);
      builder.Property(x => x.FactsJsonb).HasColumnType("jsonb");
      builder.Property(x => x.ResolvedRefsJsonb).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasOne(x => x.Thread)
        .WithOne(x => x.Memory)
        .HasForeignKey<ChatThreadMemory>(x => x.ThreadId)
        .OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.LastMessage)
        .WithMany()
        .HasForeignKey(x => x.LastMessageId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<Cart>(builder =>
    {
      builder.ToTable("carts");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.UserId).IsUnique();
      builder.HasOne(x => x.User).WithMany(x => x.Carts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<CartItem>(builder =>
    {
      builder.ToTable("cart_items");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();
      builder.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Variant).WithMany(x => x.CartItems).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
      builder.ToTable(t => t.HasCheckConstraint("ck_cart_items_qty", "quantity > 0"));
    });

    modelBuilder.Entity<Order>(builder =>
    {
      builder.ToTable("orders");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.OrderCode).HasMaxLength(30).IsRequired();
      builder.Property(x => x.RecipientName).HasMaxLength(120).IsRequired();
      builder.Property(x => x.RecipientPhone).HasMaxLength(20).IsRequired();
      builder.Property(x => x.Province).HasMaxLength(100).IsRequired();
      builder.Property(x => x.District).HasMaxLength(100).IsRequired();
      builder.Property(x => x.Ward).HasMaxLength(100);
      builder.Property(x => x.Subtotal).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.ShippingFee).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.TotalAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.OrderStatus).HasMaxLength(20).HasDefaultValue("pending").IsRequired();
      builder.Property(x => x.PlacedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.OrderCode).IsUnique();
      builder.HasIndex(x => x.UserId).HasDatabaseName("idx_orders_user_id");
      builder.HasIndex(x => x.OrderStatus).HasDatabaseName("idx_orders_status");
      builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_orders_created_at");
      builder.HasOne(x => x.User).WithMany(x => x.Orders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
      builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_orders_subtotal", "subtotal >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_orders_discount", "discount_amount >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_orders_shipping", "shipping_fee >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_orders_total", "total_amount >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_orders_status", "order_status IN ('pending','confirmed','processing','shipping','completed','cancelled','returned')"));
    });

    modelBuilder.Entity<OrderItem>(builder =>
    {
      builder.ToTable("order_items");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Sku).HasMaxLength(80);
      builder.Property(x => x.Size).HasMaxLength(30);
      builder.Property(x => x.Color).HasMaxLength(50);
      builder.Property(x => x.UnitPrice).HasColumnType("numeric(12,2)");
      builder.Property(x => x.LineTotal).HasColumnType("numeric(12,2)");
      builder.Property(x => x.CustomMeasurementsJson).HasColumnType("jsonb");
      builder.HasIndex(x => x.OrderId).HasDatabaseName("idx_order_items_order_id");
      builder.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.MeasurementProfile).WithMany().HasForeignKey(x => x.MeasurementProfileId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_order_items_unit_price", "unit_price >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_order_items_line_total", "line_total >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_order_items_quantity", "quantity > 0"));
    });

    modelBuilder.Entity<Payment>(builder =>
    {
      builder.ToTable("payments");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Amount).HasColumnType("numeric(12,2)");
      builder.Property(x => x.PaidAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("idx_payments_order_id");
      builder.HasOne(x => x.Order).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.ToTable(t => t.HasCheckConstraint("ck_payments_amount", "amount >= 0"));
    });

    modelBuilder.Entity<Shipment>(builder =>
    {
      builder.ToTable("shipments");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Carrier).HasMaxLength(100);
      builder.Property(x => x.TrackingNumber).HasMaxLength(120);
      builder.Property(x => x.ShippingStatus).HasMaxLength(20).HasDefaultValue("pending").IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.OrderId).HasDatabaseName("idx_shipments_order_id");
      builder.HasOne(x => x.Order).WithMany(x => x.Shipments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.ToTable(t => t.HasCheckConstraint("ck_shipments_status", "shipping_status IN ('pending','packed','shipped','delivered','failed','returned')"));
    });

    modelBuilder.Entity<PromoCode>(builder =>
    {
      builder.ToTable("promo_codes");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
      builder.Property(x => x.DiscountType).HasMaxLength(20).IsRequired();
      builder.Property(x => x.DiscountValue).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.MinOrderAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0);
      builder.Property(x => x.MaxUses).HasDefaultValue(0);
      builder.Property(x => x.CurrentUses).HasDefaultValue(0);
      builder.Property(x => x.FreeShipping).HasDefaultValue(false);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Code).IsUnique();
      builder.ToTable(t => t.HasCheckConstraint("ck_promo_discount_value", "discount_value >= 0"));
      builder.ToTable(t => t.HasCheckConstraint("ck_promo_uses", "current_uses >= 0 AND (max_uses = 0 OR current_uses <= max_uses)"));
      builder.ToTable(t => t.HasCheckConstraint("ck_promo_dates", "end_date > start_date"));
    });

    modelBuilder.Entity<OrderPromoCode>(builder =>
    {
      builder.ToTable("order_promo_codes");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.DiscountAmountApplied).HasColumnType("numeric(12,2)");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.OrderId, x.PromoCodeId }).IsUnique();
      builder.HasOne(x => x.Order).WithMany(x => x.OrderPromoCodes).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.PromoCode).WithMany(x => x.OrderPromoCodes).HasForeignKey(x => x.PromoCodeId).OnDelete(DeleteBehavior.Restrict);
      builder.ToTable(t => t.HasCheckConstraint("ck_order_promo_discount", "discount_amount_applied >= 0"));
    });

    modelBuilder.Entity<Review>(builder =>
    {
      builder.ToTable("reviews");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Rating).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_reviews_product_id");
      builder.HasIndex(x => new { x.UserId, x.ProductId, x.OrderItemId }).IsUnique();
      builder.HasOne(x => x.User).WithMany(x => x.Reviews).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5"));
    });

    modelBuilder.Entity<Comment>(builder =>
    {
      builder.ToTable("comments");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Content).IsRequired();
      builder.Property(x => x.Rating);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_comments_product_id");
      builder.HasIndex(x => x.ParentCommentId).HasDatabaseName("idx_comments_parent_comment_id");
      builder.HasOne(x => x.User).WithMany(x => x.Comments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Product).WithMany(x => x.Comments).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.ParentComment).WithMany(x => x.Replies).HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Cascade);
      builder.ToTable(t => t.HasCheckConstraint("ck_comments_rating", "rating IS NULL OR (rating >= 1 AND rating <= 5)"));
    });


    modelBuilder.Entity<ImageValidationCacheEntry>(builder =>
    {
      builder.ToTable("image_validation_cache_entries");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
      builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
      builder.Property(x => x.FileSizeBytes).IsRequired();
      builder.Property(x => x.Width).IsRequired();
      builder.Property(x => x.Height).IsRequired();
      builder.Property(x => x.IsValid).IsRequired();
      builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
      builder.Property(x => x.Category).HasMaxLength(80);
      builder.Property(x => x.Confidence).HasColumnType("numeric(5,4)");
      builder.Property(x => x.Provider).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Model).HasMaxLength(120).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.ExpiresAt).IsRequired();
      builder.Property(x => x.LastUsedAt);
      builder.HasIndex(x => x.Sha256Hash).IsUnique();
      builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("idx_image_validation_cache_entries_expires_at");
    });

    modelBuilder.Entity<UserGeneratedImage>(builder =>
    {
      builder.ToTable("user_generated_images");
      builder.Property(x => x.ObjectKey).HasMaxLength(500).IsRequired();
      builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
      builder.Property(x => x.Kind).HasMaxLength(40).IsRequired().HasDefaultValue("user_image");
      builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
      builder.Property(x => x.OriginalFileName).HasMaxLength(255);
      builder.Property(x => x.SourceType).HasMaxLength(20).IsRequired().HasDefaultValue("chat");
      builder.Property(x => x.GuestKeyHash).HasMaxLength(128);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.UserId).HasDatabaseName("idx_user_generated_images_user_id");
      builder.HasIndex(x => x.GuestKeyHash).HasDatabaseName("idx_user_generated_images_guest_key");
      builder.HasIndex(x => x.SourceType).HasDatabaseName("idx_user_generated_images_source_type");
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<AiTryOnFeedback>(builder =>
    {
      builder.ToTable("ai_tryon_feedbacks");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Rating).IsRequired();
      builder.Property(x => x.Comment).HasMaxLength(1000);
      builder.Property(x => x.AdminNote).HasMaxLength(1000);
      builder.Property(x => x.GuestKeyHash).HasMaxLength(128);
      builder.Property(x => x.IsResolved).HasDefaultValue(false).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.UserGeneratedImageId).HasDatabaseName("idx_ai_tryon_feedbacks_image_id");
      builder.HasIndex(x => x.UserId).HasDatabaseName("idx_ai_tryon_feedbacks_user_id");
      builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_ai_tryon_feedbacks_created_at");
      builder.HasOne(x => x.UserGeneratedImage).WithMany().HasForeignKey(x => x.UserGeneratedImageId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_ai_tryon_feedbacks_rating", "rating BETWEEN 1 AND 5"));
    });


    modelBuilder.Entity<AdminAiAction>(builder =>
    {
      builder.ToTable("admin_ai_actions");
      builder.Property(x => x.ToolName).HasMaxLength(100).IsRequired();
      builder.Property(x => x.ToolInput).HasColumnType("text");
      builder.Property(x => x.ToolResult).HasColumnType("text");
      builder.Property(x => x.ConfirmedBy).HasMaxLength(100);
      builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(30).IsRequired();
      builder.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.AdminUserId).HasDatabaseName("idx_admin_ai_actions_admin_user_id");
      builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_admin_ai_actions_created_at");
      builder.HasOne(x => x.AdminUser).WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Cascade);
    });
    modelBuilder.Entity<LlmAuditLog>(builder =>
    {
      builder.ToTable("llm_audit_logs");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.RequestId).HasMaxLength(64).IsRequired();
      builder.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
      builder.Property(x => x.TraceId).HasMaxLength(128);
      builder.Property(x => x.ActorRole).HasMaxLength(40);
      builder.Property(x => x.Source).HasMaxLength(80).IsRequired();
      builder.Property(x => x.IpHash).HasMaxLength(128);
      builder.Property(x => x.UserAgentHash).HasMaxLength(128);
      builder.Property(x => x.Provider).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Model).HasMaxLength(160);
      builder.Property(x => x.Operation).HasMaxLength(120).IsRequired();
      builder.Property(x => x.ActionType).HasMaxLength(80);
      builder.Property(x => x.ToolName).HasMaxLength(120);
      builder.Property(x => x.RiskLevel).HasMaxLength(30);
      builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
      builder.Property(x => x.ErrorCode).HasMaxLength(80);
      builder.Property(x => x.EstimatedCost).HasColumnType("numeric(12,6)");
      builder.Property(x => x.PromptPreviewRedacted).HasColumnType("text");
      builder.Property(x => x.CompletionPreviewRedacted).HasColumnType("text");
      builder.Property(x => x.InputMetadataJson).HasColumnType("jsonb");
      builder.Property(x => x.OutputMetadataJson).HasColumnType("jsonb");
      builder.Property(x => x.SafetyFlagsJson).HasColumnType("jsonb");
      builder.Property(x => x.RedactionVersion).HasMaxLength(20).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_llm_audit_logs_created_at");
      builder.HasIndex(x => new { x.Source, x.CreatedAt }).HasDatabaseName("idx_llm_audit_logs_source_created_at");
      builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("idx_llm_audit_logs_status_created_at");
      builder.HasIndex(x => new { x.ActorUserId, x.CreatedAt }).HasDatabaseName("idx_llm_audit_logs_actor_created_at");
      builder.HasIndex(x => x.ThreadId).HasDatabaseName("idx_llm_audit_logs_thread_id");
      builder.HasIndex(x => x.ConversationId).HasDatabaseName("idx_llm_audit_logs_conversation_id");
      builder.HasIndex(x => x.RequestId).HasDatabaseName("idx_llm_audit_logs_request_id");
      builder.HasIndex(x => new { x.Provider, x.Model }).HasDatabaseName("idx_llm_audit_logs_provider_model");
    });

    modelBuilder.Entity<HermesRun>(builder =>
    {
      builder.ToTable("hermes_runs");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
      builder.Property(x => x.Trigger).HasMaxLength(80).IsRequired();
      builder.Property(x => x.ConversationId).HasMaxLength(160);
      builder.Property(x => x.PromptPreview).HasColumnType("text").IsRequired();
      builder.Property(x => x.ResultPreview).HasColumnType("text");
      builder.Property(x => x.Error).HasColumnType("text");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Status, x.StartedAt }).HasDatabaseName("idx_hermes_runs_status_started_at");
      builder.HasIndex(x => new { x.Trigger, x.StartedAt }).HasDatabaseName("idx_hermes_runs_trigger_started_at");
      builder.HasIndex(x => x.AdminUserId).HasDatabaseName("idx_hermes_runs_admin_user_id");
      builder.HasIndex(x => x.ConversationId).HasDatabaseName("idx_hermes_runs_conversation_id");
    });

    modelBuilder.Entity<HermesReport>(builder =>
    {
      builder.ToTable("hermes_reports");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ReportType).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Severity).HasMaxLength(30).HasDefaultValue("info").IsRequired();
      builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Summary).HasColumnType("text").IsRequired();
      builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
      builder.Property(x => x.Source).HasMaxLength(80).HasDefaultValue("hermes_agent").IsRequired();
      builder.Property(x => x.CorrelationId).HasMaxLength(128);
      builder.Property(x => x.Status).HasMaxLength(40).HasDefaultValue("open").IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Severity, x.CreatedAt }).HasDatabaseName("idx_hermes_reports_severity_created_at");
      builder.HasIndex(x => new { x.ReportType, x.CreatedAt }).HasDatabaseName("idx_hermes_reports_type_created_at");
      builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("idx_hermes_reports_status_created_at");
      builder.HasIndex(x => x.RunId).HasDatabaseName("idx_hermes_reports_run_id");
      builder.HasIndex(x => x.CorrelationId).HasDatabaseName("idx_hermes_reports_correlation_id");
      builder.HasOne(x => x.Run).WithMany(x => x.Reports).HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_hermes_reports_severity", "severity IN ('info', 'warning', 'high', 'critical')"));
    });

    modelBuilder.Entity<HermesHeartbeat>(builder =>
    {
      builder.ToTable("hermes_heartbeats");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.RunnerName).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Status).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Model).HasMaxLength(160);
      builder.Property(x => x.GatewayStatus).HasMaxLength(120);
      builder.Property(x => x.LastError).HasColumnType("text");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.RunnerName, x.RecordedAt }).HasDatabaseName("idx_hermes_heartbeats_runner_recorded_at");
    });

    modelBuilder.Entity<HermesEventOutbox>(builder =>
    {
      builder.ToTable("hermes_event_outbox");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
      builder.Property(x => x.AggregateType).HasMaxLength(80).IsRequired();
      builder.Property(x => x.AggregateId).HasMaxLength(128).IsRequired();
      builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
      builder.Property(x => x.Status).HasMaxLength(40).HasDefaultValue("pending").IsRequired();
      builder.Property(x => x.MaxAttempts).HasDefaultValue(5).IsRequired();
      builder.Property(x => x.LastError).HasColumnType("text");
      builder.Property(x => x.CorrelationId).HasMaxLength(128);
      builder.Property(x => x.IdempotencyKey).HasMaxLength(200);
      builder.Property(x => x.LockedBy).HasMaxLength(120);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Status, x.ScheduledAt, x.OccurredAt }).HasDatabaseName("idx_hermes_event_outbox_status_schedule");
      builder.HasIndex(x => new { x.EventType, x.OccurredAt }).HasDatabaseName("idx_hermes_event_outbox_type_occurred_at");
      builder.HasIndex(x => new { x.AggregateType, x.AggregateId }).HasDatabaseName("idx_hermes_event_outbox_aggregate");
      builder.HasIndex(x => x.CorrelationId).HasDatabaseName("idx_hermes_event_outbox_correlation_id");
      builder.HasIndex(x => x.IdempotencyKey)
        .IsUnique()
        .HasFilter("idempotency_key IS NOT NULL")
        .HasDatabaseName("ux_hermes_event_outbox_idempotency_key");
      builder.ToTable(t => t.HasCheckConstraint("ck_hermes_event_outbox_status", "status IN ('pending','processing','completed','failed','dead','cancelled')"));
    });

    modelBuilder.Entity<HermesMonitorLink>(builder =>
    {
      builder.ToTable("hermes_monitor_links");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
      builder.Property(x => x.ScopeType).HasMaxLength(40).HasDefaultValue("event").IsRequired();
      builder.Property(x => x.ScopeId).HasMaxLength(128).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ux_hermes_monitor_links_token_hash");
      builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("idx_hermes_monitor_links_expires_at");
      builder.HasIndex(x => new { x.ScopeType, x.ScopeId }).HasDatabaseName("idx_hermes_monitor_links_scope");
      builder.HasIndex(x => x.CreatedByAdminUserId).HasDatabaseName("idx_hermes_monitor_links_created_by_admin_user_id");
      builder.ToTable(t => t.HasCheckConstraint("ck_hermes_monitor_links_scope_type", "scope_type IN ('event')"));
    });

    modelBuilder.Entity<HermesAgentTraceStep>(builder =>
    {
      builder.ToTable("hermes_agent_trace_steps");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Kind).HasMaxLength(60).IsRequired();
      builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Summary).HasColumnType("text").IsRequired();
      builder.Property(x => x.Status).HasMaxLength(40).HasDefaultValue("success").IsRequired();
      builder.Property(x => x.SafePayloadJson).HasColumnType("jsonb");
      builder.Property(x => x.Error).HasColumnType("text");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.EventOutboxId, x.StartedAt }).HasDatabaseName("idx_hermes_trace_steps_event_started_at");
      builder.HasIndex(x => new { x.RunId, x.StartedAt }).HasDatabaseName("idx_hermes_trace_steps_run_started_at");
      builder.HasIndex(x => new { x.RunId, x.Kind }).HasDatabaseName("idx_hermes_trace_steps_run_kind");
      builder.HasIndex(x => new { x.Kind, x.StartedAt }).HasDatabaseName("idx_hermes_trace_steps_kind_started_at");
      builder.HasOne(x => x.Run).WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.EventOutbox).WithMany().HasForeignKey(x => x.EventOutboxId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_hermes_agent_trace_steps_status", "status IN ('success','failed','running','skipped')"));
    });

    modelBuilder.Entity<HermesActionAudit>(builder =>
    {
      builder.ToTable("hermes_action_audit");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Method).HasMaxLength(10).IsRequired();
      builder.Property(x => x.Path).HasMaxLength(400).IsRequired();
      builder.Property(x => x.BodyHash).HasMaxLength(128).IsRequired();
      builder.Property(x => x.BodyPreview).HasColumnType("text");
      builder.Property(x => x.RiskLevel).HasMaxLength(20);
      builder.Property(x => x.ResponseStatus).IsRequired();
      builder.Property(x => x.ResponsePreview).HasColumnType("text");
      builder.Property(x => x.Error).HasColumnType("text");
      builder.Property(x => x.ExecutedAt).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.RunId, x.ExecutedAt }).HasDatabaseName("idx_hermes_action_audit_run_executed_at");
      builder.HasIndex(x => new { x.Method, x.Path, x.ExecutedAt }).HasDatabaseName("idx_hermes_action_audit_method_path_executed_at");
      builder.HasIndex(x => x.EventOutboxId).HasDatabaseName("idx_hermes_action_audit_event_outbox_id");
      builder.HasIndex(x => new { x.ResponseStatus, x.ExecutedAt }).HasDatabaseName("idx_hermes_action_audit_status_executed_at");
    });

    modelBuilder.Entity<OrderAttribution>(builder =>
    {
      builder.ToTable("order_attributions");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.AnonymousSessionId).HasMaxLength(128);
      builder.Property(x => x.FirstTouchSource).HasMaxLength(80);
      builder.Property(x => x.FirstTouchMedium).HasMaxLength(80);
      builder.Property(x => x.FirstTouchCampaign).HasMaxLength(120);
      builder.Property(x => x.LastTouchSource).HasMaxLength(80);
      builder.Property(x => x.LastTouchMedium).HasMaxLength(80);
      builder.Property(x => x.LastTouchCampaign).HasMaxLength(120);
      builder.Property(x => x.PromoCode).HasMaxLength(50);
      builder.Property(x => x.AttributedRevenue).HasColumnType("numeric(12,2)");
      builder.Property(x => x.AttributedDiscount).HasColumnType("numeric(12,2)");
      builder.Property(x => x.AttributedShippingSubsidy).HasColumnType("numeric(12,2)");
      builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
      builder.HasIndex(x => x.OrderId).IsUnique();
      builder.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("idx_order_attributions_user_created_at");
      builder.HasIndex(x => new { x.LastTouchSource, x.LastTouchMedium, x.LastTouchCampaign }).HasDatabaseName("idx_order_attributions_last_touch");
      builder.HasIndex(x => x.PromoCodeId).HasDatabaseName("idx_order_attributions_promo_code_id");
      builder.HasOne(x => x.Order).WithOne().HasForeignKey<OrderAttribution>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.Promo).WithMany().HasForeignKey(x => x.PromoCodeId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<CustomerEvent>(builder =>
    {
      builder.ToTable("customer_events");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.AnonymousSessionId).HasMaxLength(128);
      builder.Property(x => x.EventType).HasMaxLength(60).IsRequired();
      builder.Property(x => x.Source).HasMaxLength(80);
      builder.Property(x => x.Medium).HasMaxLength(80);
      builder.Property(x => x.Campaign).HasMaxLength(120);
      builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
      builder.Property(x => x.IpHash).HasMaxLength(128);
      builder.Property(x => x.UserAgentHash).HasMaxLength(128);
      builder.HasIndex(x => new { x.UserId, x.OccurredAt }).HasDatabaseName("idx_customer_events_user_occurred_at");
      builder.HasIndex(x => new { x.AnonymousSessionId, x.OccurredAt }).HasDatabaseName("idx_customer_events_session_occurred_at");
      builder.HasIndex(x => new { x.EventType, x.OccurredAt }).HasDatabaseName("idx_customer_events_type_occurred_at");
      builder.HasIndex(x => new { x.CampaignId, x.OccurredAt }).HasDatabaseName("idx_customer_events_campaign_occurred_at");
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.PromoCode).WithMany().HasForeignKey(x => x.PromoCodeId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<EmailTemplate>(builder =>
    {
      builder.ToTable("email_templates");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Key).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
      builder.Property(x => x.Subject).HasMaxLength(255).IsRequired();
      builder.Property(x => x.Preheader).HasMaxLength(255);
      builder.Property(x => x.HtmlBody).HasColumnType("text").IsRequired();
      builder.Property(x => x.TextBody).HasColumnType("text");
      builder.Property(x => x.TemplateType).HasMaxLength(80).HasDefaultValue("legacy.html").IsRequired();
      builder.Property(x => x.ConfigJson).HasColumnType("jsonb");
      builder.Property(x => x.IsSystem).HasDefaultValue(false).IsRequired();
      builder.Property(x => x.Locale).HasMaxLength(20).HasDefaultValue("vi-VN").IsRequired();
      builder.Property(x => x.Version).HasDefaultValue(1).IsRequired();
      builder.HasIndex(x => new { x.Key, x.Locale, x.Version }).IsUnique();
    });

    modelBuilder.Entity<EmailJob>(builder =>
    {
      builder.ToTable("email_jobs");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ToEmail).HasMaxLength(150).IsRequired();
      builder.Property(x => x.TemplateKey).HasMaxLength(120).IsRequired();
      builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("queued").IsRequired();
      builder.Property(x => x.ProviderMessageId).HasMaxLength(255);
      builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
      builder.HasIndex(x => new { x.Status, x.ScheduledAt }).HasDatabaseName("idx_email_jobs_status_scheduled_at");
    });

    modelBuilder.Entity<EmailSendLog>(builder =>
    {
      builder.ToTable("email_send_logs");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ToEmail).HasMaxLength(150).IsRequired();
      builder.Property(x => x.TemplateKey).HasMaxLength(120).IsRequired();
      builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
      builder.Property(x => x.ProviderMessageId).HasMaxLength(255);
      builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
      builder.HasIndex(x => x.EmailJobId);
      builder.HasOne(x => x.EmailJob).WithMany(x => x.SendLogs).HasForeignKey(x => x.EmailJobId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<Subscriber>(builder =>
    {
      builder.ToTable("subscribers");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
      builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending").IsRequired();
      builder.Property(x => x.UnsubscribeToken).HasMaxLength(128).IsRequired();
      builder.Property(x => x.ConfirmationToken).HasMaxLength(128).IsRequired();
      builder.HasIndex(x => x.Email).IsUnique();
      builder.HasIndex(x => x.UnsubscribeToken).IsUnique();
      builder.HasIndex(x => x.ConfirmationToken).IsUnique();
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<MarketingConsent>(builder =>
    {
      builder.ToTable("marketing_consents");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Channel).HasMaxLength(20).HasDefaultValue("email").IsRequired();
      builder.Property(x => x.Source).HasMaxLength(80).IsRequired();
      builder.Property(x => x.ConsentVersion).HasMaxLength(30).HasDefaultValue("2026-01").IsRequired();
      builder.Property(x => x.IpHash).HasMaxLength(128);
      builder.Property(x => x.UserAgentHash).HasMaxLength(128);
      builder.HasIndex(x => new { x.SubscriberId, x.Channel, x.IsOptIn });
      builder.HasOne(x => x.Subscriber).WithMany(x => x.Consents).HasForeignKey(x => x.SubscriberId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<SocialAccountConnection>(builder =>
    {
      builder.ToTable("social_account_connections");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Provider).HasMaxLength(50).HasDefaultValue("zernio").IsRequired();
      builder.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("facebook").IsRequired();
      builder.Property(x => x.ZernioProfileId).HasMaxLength(120).IsRequired();
      builder.Property(x => x.ZernioAccountId).HasMaxLength(120).IsRequired();
      builder.Property(x => x.DisplayName).HasMaxLength(255);
      builder.Property(x => x.Username).HasMaxLength(255);
      builder.Property(x => x.AvatarUrl).HasMaxLength(1000);
      builder.Property(x => x.LastSyncedAt);
      builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Provider, x.ZernioAccountId }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_social_accounts_provider_account_unique");
      builder.HasIndex(x => new { x.Platform, x.IsActive, x.DisplayName }).HasDatabaseName("idx_social_accounts_platform_active_name");
      builder.HasIndex(x => x.ZernioProfileId).HasDatabaseName("idx_social_accounts_zernio_profile_id");
    });

    modelBuilder.Entity<SocialInboxConversation>(builder =>
    {
      builder.ToTable("social_inbox_conversations");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("facebook").IsRequired();
      builder.Property(x => x.AccountId).HasMaxLength(120).IsRequired();
      builder.Property(x => x.AccountUsername).HasMaxLength(255);
      builder.Property(x => x.ProfileId).HasMaxLength(120);
      builder.Property(x => x.ConversationId).HasMaxLength(200).IsRequired();
      builder.Property(x => x.ParticipantId).HasMaxLength(200);
      builder.Property(x => x.ParticipantName).HasMaxLength(255);
      builder.Property(x => x.ParticipantPicture).HasMaxLength(1000);
      builder.Property(x => x.LastMessage).HasColumnType("text");
      builder.Property(x => x.Status).HasMaxLength(50);
      builder.Property(x => x.Url).HasMaxLength(1000);
      builder.Property(x => x.RawJson).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.ConversationId }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_social_inbox_conversations_unique");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.UpdatedTime }).HasDatabaseName("idx_social_inbox_conversations_account_updated");
      builder.HasIndex(x => new { x.Status, x.UpdatedTime }).HasDatabaseName("idx_social_inbox_conversations_status_updated");
    });

    modelBuilder.Entity<SocialInboxMessage>(builder =>
    {
      builder.ToTable("social_inbox_messages");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("facebook").IsRequired();
      builder.Property(x => x.AccountId).HasMaxLength(120).IsRequired();
      builder.Property(x => x.ConversationId).HasMaxLength(200).IsRequired();
      builder.Property(x => x.MessageId).HasMaxLength(200).IsRequired();
      builder.Property(x => x.SenderId).HasMaxLength(200);
      builder.Property(x => x.SenderName).HasMaxLength(255);
      builder.Property(x => x.Direction).HasMaxLength(30).HasDefaultValue("incoming").IsRequired();
      builder.Property(x => x.Text).HasColumnType("text");
      builder.Property(x => x.AttachmentsJson).HasColumnType("jsonb");
      builder.Property(x => x.DeliveryStatus).HasMaxLength(50);
      builder.Property(x => x.RawJson).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.MessageId }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_social_inbox_messages_unique");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.ConversationId, x.CreatedAt }).HasDatabaseName("idx_social_inbox_messages_thread_created");
    });

    modelBuilder.Entity<SocialInboxComment>(builder =>
    {
      builder.ToTable("social_inbox_comments");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("facebook").IsRequired();
      builder.Property(x => x.AccountId).HasMaxLength(120).IsRequired();
      builder.Property(x => x.PostId).HasMaxLength(200).IsRequired();
      builder.Property(x => x.CommentId).HasMaxLength(200).IsRequired();
      builder.Property(x => x.ParentCommentId).HasMaxLength(200);
      builder.Property(x => x.AuthorId).HasMaxLength(200);
      builder.Property(x => x.AuthorName).HasMaxLength(255);
      builder.Property(x => x.AuthorUsername).HasMaxLength(255);
      builder.Property(x => x.AuthorPicture).HasMaxLength(1000);
      builder.Property(x => x.Message).HasColumnType("text");
      builder.Property(x => x.Url).HasMaxLength(1000);
      builder.Property(x => x.RawJson).HasColumnType("jsonb");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.CommentId }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_social_inbox_comments_unique");
      builder.HasIndex(x => new { x.Platform, x.AccountId, x.PostId, x.CreatedTime }).HasDatabaseName("idx_social_inbox_comments_post_created");
      builder.HasIndex(x => x.ParentCommentId).HasDatabaseName("idx_social_inbox_comments_parent");
    });

    modelBuilder.Entity<SocialInboxSyncCursor>(builder =>
    {
      builder.ToTable("social_inbox_sync_cursors");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Resource).HasMaxLength(80).IsRequired();
      builder.Property(x => x.Platform).HasMaxLength(50).HasDefaultValue("facebook").IsRequired();
      builder.Property(x => x.AccountId).HasMaxLength(120).HasDefaultValue(string.Empty).IsRequired();
      builder.Property(x => x.ProfileId).HasMaxLength(120).HasDefaultValue(string.Empty).IsRequired();
      builder.Property(x => x.Cursor).HasColumnType("text");
      builder.Property(x => x.LastError).HasColumnType("text");
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => new { x.Resource, x.Platform, x.AccountId, x.ProfileId }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_social_inbox_sync_cursors_unique");
    });

    modelBuilder.Entity<FacebookPageConnection>(builder =>
    {
      builder.ToTable("facebook_page_connections");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.PageId).HasMaxLength(100).IsRequired();
      builder.Property(x => x.PageName).HasMaxLength(255);
      builder.Property(x => x.EncryptedPageAccessToken).HasColumnType("text").IsRequired();
      builder.Property(x => x.TokenLast4).HasMaxLength(8).IsRequired();
      builder.Property(x => x.ExpiresAt);
      builder.Property(x => x.LastValidatedAt);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.PageId).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_facebook_page_connections_page_id_unique");
      builder.HasIndex(x => new { x.IsActive, x.PageName }).HasDatabaseName("idx_facebook_page_connections_active_name");
    });

    modelBuilder.Entity<OrderPromoCostSnapshot>(builder =>
    {
      builder.ToTable("order_promo_cost_snapshots");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Code).HasMaxLength(50);
      builder.Property(x => x.DiscountType).HasMaxLength(20);
      builder.Property(x => x.DiscountValue).HasColumnType("numeric(12,2)");
      builder.Property(x => x.SubtotalBeforeDiscount).HasColumnType("numeric(12,2)");
      builder.Property(x => x.DiscountAmount).HasColumnType("numeric(12,2)");
      builder.Property(x => x.ShippingFeeBeforePromo).HasColumnType("numeric(12,2)");
      builder.Property(x => x.ShippingFeeCharged).HasColumnType("numeric(12,2)");
      builder.Property(x => x.ShippingSubsidy).HasColumnType("numeric(12,2)");
      builder.Property(x => x.TotalAfterDiscount).HasColumnType("numeric(12,2)");
      builder.Property(x => x.EstimatedCostOfGoods).HasColumnType("numeric(12,2)");
      builder.Property(x => x.EstimatedGrossProfitBeforePromo).HasColumnType("numeric(12,2)");
      builder.Property(x => x.EstimatedGrossProfitAfterPromo).HasColumnType("numeric(12,2)");
      builder.Property(x => x.MarginLoss).HasColumnType("numeric(12,2)");
      builder.HasIndex(x => new { x.PromoCodeId, x.CreatedAt }).HasDatabaseName("idx_order_promo_cost_snapshots_promo_created_at");
      builder.HasIndex(x => x.OrderId).IsUnique();
      builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.PromoCode).WithMany().HasForeignKey(x => x.PromoCodeId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<BlogCategory>(builder =>
    {
      builder.ToTable("blog_categories");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
      builder.Property(x => x.Description).HasMaxLength(500);
      builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_blog_categories_slug_unique");
      builder.HasIndex(x => new { x.IsActive, x.SortOrder }).HasDatabaseName("idx_blog_categories_active_sort_order");
    });

    modelBuilder.Entity<BlogPost>(builder =>
    {
      builder.ToTable("blog_posts");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
      builder.Property(x => x.Slug).HasMaxLength(500).IsRequired();
      builder.Property(x => x.Excerpt).IsRequired();
      builder.Property(x => x.FeaturedImage).HasMaxLength(1000);
      builder.Property(x => x.Template).HasConversion<string>().HasMaxLength(50).IsRequired();
      builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
      builder.Property(x => x.Content).HasColumnType("jsonb").IsRequired();
      builder.Property(x => x.Tags).HasColumnType("jsonb").IsRequired();
      builder.Property(x => x.AuthorNameOverride).HasMaxLength(200);
      builder.Property(x => x.ReviewedBy).HasMaxLength(200);
      builder.Property(x => x.MetaTitle).HasMaxLength(200);
      builder.Property(x => x.MetaDescription).HasMaxLength(500);
      builder.Property(x => x.CanonicalUrl).HasMaxLength(2000);
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.Slug).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("idx_blog_posts_slug_unique");
      builder.HasIndex(x => new { x.Status, x.PublishedAt }).HasDatabaseName("idx_blog_posts_status_published_at");
      builder.HasIndex(x => x.AuthorId).HasDatabaseName("idx_blog_posts_author_id");
      builder.HasIndex(x => x.BlogCategoryId).HasDatabaseName("idx_blog_posts_blog_category_id");
      builder.HasIndex(x => x.Tags).HasMethod("gin").HasDatabaseName("idx_blog_posts_tags_gin");
      builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
      builder.HasOne(x => x.BlogCategory).WithMany(x => x.Posts).HasForeignKey(x => x.BlogCategoryId).OnDelete(DeleteBehavior.SetNull);
    });


    modelBuilder.Entity<BlogImage>(builder =>
    {
      builder.ToTable("blog_images");
      builder.HasKey(x => x.Id);
      builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
      builder.Property(x => x.PublicObjectKey).HasMaxLength(1000);
      builder.Property(x => x.AltText).HasMaxLength(255);
      builder.Property(x => x.SortOrder).HasDefaultValue(0).IsRequired();
      builder.Property(x => x.IsPublic).HasDefaultValue(false).IsRequired();
      builder.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
      builder.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW()");
      builder.HasIndex(x => x.BlogPostId).HasDatabaseName("idx_blog_images_blog_post_id");
      builder.HasOne(x => x.BlogPost).WithMany().HasForeignKey(x => x.BlogPostId).OnDelete(DeleteBehavior.SetNull);
    });
    ApplySnakeCaseColumnNames(modelBuilder);
    ApplyGlobalSoftDeleteQueryFilters(modelBuilder);
  }

  private static void ApplySnakeCaseColumnNames(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      foreach (var property in entityType.GetProperties())
      {
        property.SetColumnName(ToSnakeCase(property.Name));
      }
    }
  }

  private static void ApplyGlobalSoftDeleteQueryFilters(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
      {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var isDeleted = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);
        entityType.SetQueryFilter(filter);

        modelBuilder.Entity(entityType.ClrType).Property(nameof(ISoftDeletable.IsDeleted)).HasDefaultValue(false);
      }

      if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
      {
        modelBuilder.Entity(entityType.ClrType).Property(nameof(BaseEntity.IsActive)).HasDefaultValue(true);
      }
    }
  }

  private static string ToSnakeCase(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return value;
    }

    var builder = new System.Text.StringBuilder(value.Length + 8);
    for (var index = 0; index < value.Length; index++)
    {
      var current = value[index];
      if (char.IsUpper(current))
      {
        if (index > 0)
        {
          builder.Append('_');
        }

        builder.Append(char.ToLowerInvariant(current));
        continue;
      }

      builder.Append(current);
    }

    return builder.ToString();
  }
}
