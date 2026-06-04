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
        CancellationToken cancellationToken = default);

    /// <summary>Get a single product by ID for admin editing.</summary>
    Task<AdminProductDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Create a new product.</summary>
    Task<AdminProductDetailResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing product.</summary>
    Task<AdminProductDetailResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>Toggle product status (active/draft/inactive).</summary>
    Task<bool> ToggleStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete a product (set status to "deleted").</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
