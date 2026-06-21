using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin product management service implementation.</summary>
public sealed class AdminProductService(
    AppDbContext dbContext,
    IImageVisibilityService imageVisibilityService,
    IStorageService storageService,
    IHermesEventOutboxPublisher hermesEvents,
    ILogger<AdminProductService> logger) : IAdminProductService
{
    public async Task<(IReadOnlyList<AdminProductListItemResponse> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Slug.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status.Trim());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminProductListItemResponse(
                p.Id,
                p.Name,
                p.Slug,
                p.ProductType,
                p.Category.Name,
                p.Status,
                p.IsFeatured,
                p.Variants.Count,
                p.Variants.Sum(v => v.StockQty),
                p.IsDeleted,
                new DateTimeOffset(p.CreatedAt, TimeSpan.Zero)))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AdminProductDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null) return null;

        return await MapToDetailAsync(product, cancellationToken);
    }

    public async Task<AdminProductDetailResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Domain.Entities.Product
        {
            Name = request.Name,
            Slug = request.Slug,
            ProductType = request.ProductType,
            CategoryId = request.CategoryId,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            Material = request.Material,
            Brand = request.Brand,
            Origin = request.Origin,
            CareInstruction = request.CareInstruction,
            Status = request.Status,
            IsFeatured = request.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        dbContext.Products.Add(product);
        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_created",
            product.Id,
            new { productId = product.Id, product.Name, product.Slug, product.ProductType, product.CategoryId, product.Status, product.IsFeatured },
            $"product_created:Product:{product.Id:N}:{product.CreatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin created product {ProductId} ({Name})", product.Id, product.Name);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<AdminProductDetailResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null) return null;

        var oldStatus = product.Status;
        product.Name = request.Name;
        product.Slug = request.Slug;
        product.ProductType = request.ProductType;
        product.CategoryId = request.CategoryId;
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.Material = request.Material;
        product.Brand = request.Brand;
        product.Origin = request.Origin;
        product.CareInstruction = request.CareInstruction;
        product.Status = request.Status;
        product.IsFeatured = request.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;

        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_updated",
            product.Id,
            new { productId = product.Id, product.Name, product.Slug, product.ProductType, product.CategoryId, oldStatus, newStatus = product.Status, product.IsFeatured },
            $"product_updated:Product:{product.Id:N}:{product.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await ProcessStatusChangeVisibilityAsync(product, oldStatus, product.Status, cancellationToken);

        logger.LogInformation("Admin updated product {ProductId}", product.Id);
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<AdminProductDetailResponse?> CreateVariantAsync(Guid productId, CreateVariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null) return null;

        var now = DateTime.UtcNow;
        var isDefault = request.IsDefault || product.Variants.Count == 0;
        var variant = new Domain.Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            Sku = request.Sku.Trim(),
            VariantName = string.IsNullOrWhiteSpace(request.VariantName) ? null : request.VariantName.Trim(),
            Size = string.IsNullOrWhiteSpace(request.Size) ? null : request.Size.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            Price = request.Price,
            SalePrice = request.SalePrice,
            StockQty = request.StockQty,
            IsDefault = isDefault,
            Status = request.Status.Trim().ToLowerInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (isDefault)
        {
            await dbContext.ProductVariants
                .Where(v => v.ProductId == productId && v.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsDefault, false), cancellationToken);
        }

        product.Variants.Add(variant);
        product.UpdatedAt = now;

        await hermesEvents.EnqueueAdminInventoryEventAsync(
            "product_variant_created",
            variant.Id,
            new { productId, variantId = variant.Id, variant.Sku, variant.Size, variant.Color, variant.Price, variant.SalePrice, variant.StockQty, variant.Status, variant.IsDefault },
            $"product_variant_created:Inventory:{variant.Id:N}:{variant.CreatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin created variant {VariantId} for product {ProductId}", variant.Id, productId);
        return await GetByIdAsync(productId, cancellationToken);
    }

    public async Task<AdminProductDetailResponse?> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken);

        if (variant is null) return null;

        variant.Sku = request.Sku.Trim();
        variant.VariantName = string.IsNullOrWhiteSpace(request.VariantName) ? null : request.VariantName.Trim();
        variant.Size = string.IsNullOrWhiteSpace(request.Size) ? null : request.Size.Trim();
        variant.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        variant.Price = request.Price;
        variant.SalePrice = request.SalePrice;
        variant.StockQty = request.StockQty;
        variant.IsDefault = request.IsDefault;
        variant.Status = request.Status.Trim().ToLowerInvariant();
        variant.UpdatedAt = DateTime.UtcNow;
        variant.Product.UpdatedAt = DateTime.UtcNow;

        if (variant.IsDefault)
        {
            await dbContext.ProductVariants
                .Where(v => v.ProductId == productId && v.Id != variantId && v.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsDefault, false), cancellationToken);
        }

        await hermesEvents.EnqueueAdminInventoryEventAsync(
            "product_variant_updated",
            variant.Id,
            new { productId, variantId, variant.Sku, variant.Size, variant.Color, variant.Price, variant.SalePrice, variant.StockQty, variant.Status, variant.IsDefault },
            $"product_variant_updated:Inventory:{variant.Id:N}:{variant.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin updated variant {VariantId} for product {ProductId}", variantId, productId);
        return await GetByIdAsync(productId, cancellationToken);
    }

    public async Task<AdminProductDetailResponse?> UpdateVariantStockAsync(Guid productId, Guid variantId, int stockQty, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken);

        if (variant is null) return null;

        var oldStockQty = variant.StockQty;
        variant.StockQty = stockQty;
        variant.UpdatedAt = DateTime.UtcNow;
        variant.Product.UpdatedAt = DateTime.UtcNow;

        await hermesEvents.EnqueueAdminInventoryEventAsync(
            "product_stock_changed",
            variant.Id,
            new { productId, variantId, variant.Sku, variant.Size, variant.Color, productName = variant.Product.Name, oldStockQty, newStockQty = stockQty, delta = stockQty - oldStockQty },
            $"product_stock_changed:Inventory:{variant.Id:N}:{oldStockQty}:{stockQty}:{variant.UpdatedAt.Ticks}",
            cancellationToken);

        if (oldStockQty == 0 && stockQty > 0)
        {
            await hermesEvents.EnqueueAdminInventoryEventAsync(
                "stock_replenished",
                variant.Id,
                new { productId, variantId, variant.Sku, variant.Size, variant.Color, productName = variant.Product.Name, oldStockQty, newStockQty = stockQty, replenishedAt = DateTimeOffset.UtcNow },
                $"stock_replenished:Inventory:{variant.Id:N}:{stockQty}:{DateTime.UtcNow.Date.Ticks}",
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin updated stock for product {ProductId}, variant {VariantId} to {StockQty}", productId, variantId, stockQty);
        return await GetByIdAsync(productId, cancellationToken);
    }

    public async Task<bool> ToggleStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null) return false;

        var oldStatus = product.Status;
        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;
        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_visibility_changed",
            product.Id,
            new { productId = product.Id, product.Name, oldStatus, newStatus },
            $"product_visibility_changed:Product:{product.Id:N}:{oldStatus}:{newStatus}:{product.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await ProcessStatusChangeVisibilityAsync(product, oldStatus, newStatus, cancellationToken);

        logger.LogInformation("Admin toggled product {ProductId} status to {Status}", product.Id, newStatus);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null || product.IsDeleted) return false;

        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_deleted",
            product.Id,
            new { productId = product.Id, product.Name, product.Slug, deletedAt = product.DeletedAt },
            $"product_deleted:Product:{product.Id:N}:{product.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin soft-deleted product {ProductId} ({Name})", product.Id, product.Name);
        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null || !product.IsDeleted) return false;

        product.IsDeleted = false;
        product.DeletedAt = null;
        product.UpdatedAt = DateTime.UtcNow;

        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_updated",
            product.Id,
            new { productId = product.Id, product.Name, action = "restored" },
            $"product_restored:Product:{product.Id:N}:{product.UpdatedAt.Ticks}",
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admin restored product {ProductId} ({Name})", product.Id, product.Name);
        return true;
    }

    private async Task ProcessStatusChangeVisibilityAsync(Domain.Entities.Product product, string oldStatus, string newStatus, CancellationToken cancellationToken)
    {
        if (oldStatus == newStatus) return;

        var images = await dbContext.ProductImages.Where(i => i.ProductId == product.Id).ToListAsync(cancellationToken);

        if (newStatus == "active" && oldStatus != "active")
        {
            foreach (var img in images)
            {
                if (!img.IsPublic)
                {
                    await imageVisibilityService.MakePublicAsync(img.Id, product.Id, cancellationToken);
                }
            }
        }
        else if (newStatus != "active" && oldStatus == "active")
        {
            foreach (var img in images)
            {
                if (img.IsPublic)
                {
                    await imageVisibilityService.MakePrivateAsync(img.Id, product.Id, cancellationToken);
                }
            }
        }
    }

    public async Task<AdminImageResponse?> UploadImageAsync(Guid productId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null) return null;

        var uploadResult = await storageService.UploadAsync(stream, fileName, contentType, "private/products", cancellationToken);

        var isPrimary = product.Images.Count == 0;
        var sortOrder = product.Images.Count > 0 ? product.Images.Max(i => i.SortOrder) + 1 : 0;

        var image = new Domain.Entities.ProductImage
        {
            ProductId = productId,
            ImageUrl = uploadResult.ObjectKey,
            AltText = fileName,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            IsPublic = false
        };

        dbContext.ProductImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (product.Status == "active")
        {
            await imageVisibilityService.MakePublicAsync(image.Id, productId, cancellationToken);
            // Refresh from DB after make public
            image = await dbContext.ProductImages.FirstAsync(i => i.Id == image.Id, cancellationToken);
        }

        var resolvedUrl = await imageVisibilityService.ResolveUrlAsync(image.ImageUrl, image.IsPublic, image.PublicObjectKey, cancellationToken);

        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_media_changed",
            productId,
            new { productId, imageId = image.Id, action = "uploaded", isPublic = image.IsPublic, isPrimary = image.IsPrimary },
            $"product_media_changed:Product:{productId:N}:{image.Id:N}:uploaded:{image.CreatedAt.Ticks}",
            cancellationToken);

        return new AdminImageResponse(image.Id, resolvedUrl, image.AltText, image.SortOrder, image.IsPrimary, image.IsPublic);
    }

    public async Task<bool> DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);
        if (image is null) return false;

        if (image.IsPublic && !string.IsNullOrWhiteSpace(image.PublicObjectKey))
        {
            await storageService.DeleteAsync(image.PublicObjectKey, cancellationToken);
        }

        await storageService.DeleteAsync(image.ImageUrl, cancellationToken);

        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_media_changed",
            productId,
            new { productId, imageId = imageId, action = "deleted" },
            $"product_media_changed:Product:{productId:N}:{imageId:N}:deleted:{DateTime.UtcNow.Ticks}",
            cancellationToken);

        return true;
    }

    public async Task<bool> SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var images = await dbContext.ProductImages.Where(i => i.ProductId == productId).ToListAsync(cancellationToken);
        if (images.Count == 0 || !images.Any(i => i.Id == imageId)) return false;

        foreach (var img in images)
        {
            img.IsPrimary = img.Id == imageId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await hermesEvents.EnqueueAdminProductEventAsync(
            "product_media_changed",
            productId,
            new { productId, imageId, action = "set_primary" },
            $"product_media_changed:Product:{productId:N}:{imageId:N}:set_primary:{DateTime.UtcNow.Ticks}",
            cancellationToken);

        return true;
    }

    private async Task<AdminProductDetailResponse> MapToDetailAsync(Domain.Entities.Product p, CancellationToken cancellationToken)
    {
        var resolvedImages = new List<AdminImageResponse>();
        foreach (var i in p.Images)
        {
            var resolvedUrl = await imageVisibilityService.ResolveUrlAsync(i.ImageUrl, i.IsPublic, i.PublicObjectKey, cancellationToken);
            resolvedImages.Add(new AdminImageResponse(i.Id, resolvedUrl, i.AltText, i.SortOrder, i.IsPrimary, i.IsPublic));
        }

        return new AdminProductDetailResponse(
            p.Id,
            p.Name,
            p.Slug,
            p.ProductType,
            p.CategoryId,
            p.Category.Name,
            p.ShortDescription,
            p.Description,
            p.Material,
            p.Brand,
            p.Origin,
            p.CareInstruction,
            p.Status,
            p.IsFeatured,
            new DateTimeOffset(p.CreatedAt, TimeSpan.Zero),
            new DateTimeOffset(p.UpdatedAt, TimeSpan.Zero),
            p.Variants.Select(v => new AdminVariantResponse(
                v.Id, v.Sku, v.VariantName, v.Size, v.Color,
                v.Price, v.SalePrice, v.StockQty, v.IsDefault, v.Status)).ToList(),
            resolvedImages);
    }
}
