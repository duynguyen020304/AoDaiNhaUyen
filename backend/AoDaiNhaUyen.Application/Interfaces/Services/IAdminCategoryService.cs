using AoDaiNhaUyen.Application.DTOs.Admin;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin category management service.</summary>
public interface IAdminCategoryService
{
    /// <summary>Get all categories (flat list) for admin.</summary>
    Task<IReadOnlyList<AdminCategoryListItemResponse>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>Get a single category by ID for admin editing.</summary>
    Task<AdminCategoryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Create a new category.</summary>
    Task<AdminCategoryDetailResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update an existing category.</summary>
    Task<AdminCategoryDetailResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete a category by setting IsDeleted flag.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Restore a soft-deleted category.</summary>
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
