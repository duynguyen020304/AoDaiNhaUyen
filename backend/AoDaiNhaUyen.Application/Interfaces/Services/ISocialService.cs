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

  Task<SocialPostDto> GetPostAsync(
    string postId,
    CancellationToken cancellationToken = default);

  Task<SocialPostDto> UpdatePostAsync(
    string postId,
    UpdateSocialPostRequest request,
    CancellationToken cancellationToken = default);

  Task DeletePostAsync(
    string postId,
    CancellationToken cancellationToken = default);

  Task<SocialPostActionResultDto> UnpublishPostAsync(
    string postId,
    UnpublishSocialPostRequest request,
    CancellationToken cancellationToken = default);

  Task<SocialAnalyticsDto> GetAnalyticsAsync(
    string platform,
    DateOnly fromDate,
    DateOnly toDate,
    CancellationToken cancellationToken = default);

  Task<SocialMediaPresignDto> GetMediaPresignAsync(
    SocialMediaPresignRequest request,
    CancellationToken cancellationToken = default);

  Task<SocialCommentedPostListDto> GetCommentedPostsAsync(
    string? platform = "facebook",
    string? accountId = null,
    string? profileId = null,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<SocialCommentListDto> GetCommentsAsync(
    string postId,
    string accountId,
    string? cursor = null,
    int limit = 50,
    CancellationToken cancellationToken = default);

  Task<SocialActionResultDto> ReplyToCommentAsync(
    string postId,
    CreateSocialCommentReplyRequest request,
    CancellationToken cancellationToken = default);

  Task<SocialActionResultDto> DeleteCommentAsync(
    string postId,
    string accountId,
    string commentId,
    CancellationToken cancellationToken = default);

  Task<SocialActionResultDto> ToggleCommentHiddenAsync(
    string postId,
    string accountId,
    string commentId,
    bool isHidden,
    CancellationToken cancellationToken = default);

  Task<SocialConversationListDto> GetConversationsAsync(
    string? platform = "facebook",
    string? accountId = null,
    string? profileId = null,
    string? cursor = null,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<SocialMessageListDto> GetConversationMessagesAsync(
    string conversationId,
    string accountId,
    string? cursor = null,
    int limit = 50,
    CancellationToken cancellationToken = default);

  Task<SocialActionResultDto> SendMessageAsync(
    string conversationId,
    SendSocialMessageRequest request,
    CancellationToken cancellationToken = default);

  Task<SocialActionResultDto> MarkConversationReadAsync(
    string conversationId,
    string accountId,
    CancellationToken cancellationToken = default);
}
