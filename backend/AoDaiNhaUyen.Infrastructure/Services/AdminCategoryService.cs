using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>Admin category management service implementation.</summary>
public sealed class AdminCategoryService(AppDbContext dbContext, ILogger<AdminCategoryService> logger) : IAdminCategoryService
{
    public async Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCategoryListItemResponse(
                c.Id,
                c.Parent,
                c.Name,
                c.Slug,
                c.Description,
                c.ImageUrl,
                c.SortOrder,
                c.Products.Count,
                c.IsDeleted,
                new DateTimeOffset(c.CreatedAt, TimeSpan.Zero)))
            .ToListAsync(cancellationToken);

        return categories.AsReadOnly();
    }

    public async Task<AdminCategoryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return null;

        return MapToDetail(category);
    }

    public async Task<AdminCategoryDetailResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            Parent = request.Parent,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            SortOrder = request.SortOrder,
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin created category {CategoryId} ({Name})", category.Id, category.Name);

        return MapToDetail(category);
    }

    public async Task<AdminCategoryDetailResponse?> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            logger.LogWarning("Admin attempted to update non-existent category {CategoryId}", id);
            return null;
        }

        category.Parent = request.Parent;
        category.Name = request.Name;
        category.Slug = request.Slug;
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin updated category {CategoryId}", id);

        return MapToDetail(category);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null || category.IsDeleted)
        {
            logger.LogWarning("Admin attempted to delete non-existent or already-deleted category {CategoryId}", id);
            return false;
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin soft-deleted category {CategoryId} ({Name})", id, category.Name);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null || !category.IsDeleted)
        {
            logger.LogWarning("Admin attempted to restore non-existent or non-deleted category {CategoryId}", id);
            return false;
        }

        category.IsDeleted = false;
        category.DeletedAt = null;
        category.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin restored category {CategoryId} ({Name})", id, category.Name);

        return true;
    }

    private static AdminCategoryDetailResponse MapToDetail(Category category) =>
        new(
            category.Id,
            category.Parent,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.SortOrder,
            new DateTimeOffset(category.CreatedAt, TimeSpan.Zero),
            new DateTimeOffset(category.UpdatedAt, TimeSpan.Zero));
}
