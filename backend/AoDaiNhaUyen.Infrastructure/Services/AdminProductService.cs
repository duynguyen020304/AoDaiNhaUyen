using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin product management service implementation.</summary>
public sealed class AdminProductService(AppDbContext dbContext) : IAdminProductService
{
    public async Task<(IReadOnlyList<AdminProductListItemResponse> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Where(p => p.Status != "deleted")
            .AsQueryable();

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
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != "deleted", cancellationToken);

        if (product is null) return null;

        return MapToDetail(product);
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
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<AdminProductDetailResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != "deleted", cancellationToken);

        if (product is null) return null;

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

        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        return (await GetByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<bool> ToggleStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != "deleted", cancellationToken);

        if (product is null) return false;

        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null) return false;

        product.Status = "deleted";
        product.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static AdminProductDetailResponse MapToDetail(Domain.Entities.Product p) => new(
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
        p.Images.Select(i => new AdminImageResponse(
            i.Id, i.ImageUrl, i.AltText, i.SortOrder, i.IsPrimary)).ToList());
}
