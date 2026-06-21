using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Collections;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IAdminCollectionService
{
  Task<PagedResult<CollectionListItemDto>> GetListAsync(string? search, bool includeDeleted, int page, int pageSize, CancellationToken ct = default);
  Task<CollectionDetailDto?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
  Task<CollectionDetailDto> CreateAsync(CreateCollectionRequest request, CancellationToken ct = default);
  Task<CollectionDetailDto?> UpdateAsync(Guid id, UpdateCollectionRequest request, CancellationToken ct = default);
  Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
  Task<bool> RestoreAsync(Guid id, CancellationToken ct = default);
  Task<CollectionDetailDto?> AddProductAsync(Guid id, AddProductToCollectionRequest request, CancellationToken ct = default);
  Task<CollectionDetailDto?> RemoveProductAsync(Guid id, Guid productId, CancellationToken ct = default);
}
