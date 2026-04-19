using AoDaiNhaUyen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
  public DbSet<Review> Reviews => Set<Review>();

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
      builder.Property(x => x.Id).UseIdentityAlwaysColumn();
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
      builder.HasIndex(x => x.Email).IsUnique();
      builder.HasIndex(x => x.Phone).IsUnique();
      builder.ToTable(t => t.HasCheckConstraint("ck_users_status", "status IN ('active', 'inactive', 'blocked')"));
      builder.ToTable(t => t.HasCheckConstraint("ck_users_contact", "email IS NOT NULL OR phone IS NOT NULL"));
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
      builder.HasKey(x => new { x.UserId, x.RoleId });
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
      builder.HasKey(x => new { x.ProductId, x.ScenarioId });
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
      builder.HasKey(x => x.ThreadId);
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
      builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
      builder.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.SetNull);
      builder.ToTable(t => t.HasCheckConstraint("ck_reviews_rating", "rating BETWEEN 1 AND 5"));
    });

    ApplySnakeCaseColumnNames(modelBuilder);
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
