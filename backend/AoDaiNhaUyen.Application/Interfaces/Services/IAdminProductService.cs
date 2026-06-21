using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin product management service.</summary>
public interface IAdminProductService
{
    /// <summary>Get a paginated list of all products (including drafts) for admin.</summary>
    Task<(IReadOnlyList<AdminProductListItemResponse> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>Get a single product by ID for admin editing.</summary>
    Task<AdminProductDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Create a new product.</summary>
    Task<AdminProductDetailResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing product.</summary>
    Task<AdminProductDetailResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Create a product variant.</summary>
    Task<AdminProductDetailResponse?> CreateVariantAsync(Guid productId, CreateVariantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update product variant details.</summary>
    Task<AdminProductDetailResponse?> UpdateVariantAsync(Guid productId, Guid variantId, UpdateVariantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update stock quantity for one product variant.</summary>
    Task<AdminProductDetailResponse?> UpdateVariantStockAsync(Guid productId, Guid variantId, int stockQty, CancellationToken cancellationToken = default);

    /// <summary>Toggle product status (active/draft/inactive).</summary>
    Task<bool> ToggleStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete a product by setting IsDeleted flag.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Restore a soft-deleted product.</summary>
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);    /// <summary>Upload a product image.</summary>
    Task<AdminImageResponse?> UploadImageAsync(Guid productId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Delete a product image.</summary>
    Task<bool> DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>Set a product image as primary.</summary>
    Task<bool> SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
}
