using AoDaiNhaUyen.Application.DTOs.Social;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ISocialService
{
  Task<IReadOnlyList<SocialAccountConnectionDto>> GetAccountsAsync(
    string? platform = null,
    bool sync = false,
    string? profileId = null,
    CancellationToken cancellationToken = default);

  Task<SocialConnectUrlDto> GetConnectUrlAsync(
    CreateSocialConnectUrlRequest request,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<SocialAccountConnectionDto>> SelectFacebookPageAsync(
    SelectFacebookPageRequest request,
    CancellationToken cancellationToken = default);

  Task DisconnectAccountAsync(
    Guid id,
    CancellationToken cancellationToken = default);

  Task<SocialPostDto> CreatePostAsync(
    CreateSocialPostRequest request,
    CancellationToken cancellationToken = default);

  Task<SocialPostListDto> GetPostsAsync(
    string? platform = null,
    Guid? accountId = null,
    string? profileId = null,
    int page = 1,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<SocialMediaPresignDto> GetMediaPresignAsync(
    SocialMediaPresignRequest request,
    CancellationToken cancellationToken = default);
}
