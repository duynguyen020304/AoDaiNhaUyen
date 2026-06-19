using AoDaiNhaUyen.Application.DTOs.Facebook;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IFacebookService
{
  Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken = default);

  Task<FacebookConnectionDto> ConnectPageAsync(
    ConnectFacebookPageRequest request,
    CancellationToken cancellationToken = default);

  Task DisconnectPageAsync(
    string pageId,
    CancellationToken cancellationToken = default);

  Task<FacebookPageInfoDto> GetPageInfoAsync(
    string pageId,
    CancellationToken cancellationToken = default);

  Task<FacebookPublishResultDto> PublishPostAsync(
    string pageId,
    CreateFacebookPostRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookPublishResultDto> PublishPhotoAsync(
    string pageId,
    Stream imageStream,
    string fileName,
    string contentType,
    string? caption,
    DateTimeOffset? scheduledPublishTime = null,
    bool published = true,
    CancellationToken cancellationToken = default);

  Task<FacebookPublishResultDto> PublishVideoAsync(
    string pageId,
    Stream videoStream,
    string fileName,
    string contentType,
    string? description,
    DateTimeOffset? scheduledPublishTime = null,
    bool published = true,
    CancellationToken cancellationToken = default);

  Task<FacebookPostListDto> GetPostsAsync(
    string pageId,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<FacebookPostDto> GetPostAsync(
    string postId,
    CancellationToken cancellationToken = default);

  Task<FacebookPostDto> UpdatePostAsync(
    string postId,
    UpdateFacebookPostRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookDeleteResultDto> DeletePostAsync(
    string postId,
    CancellationToken cancellationToken = default);
}
