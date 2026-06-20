using AoDaiNhaUyen.Application.DTOs.Facebook;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IFacebookService
{
  Task<IReadOnlyList<FacebookConnectionDto>> GetConnectionsAsync(CancellationToken cancellationToken = default);

  Task<FacebookConnectionDto> ConnectPageAsync(
    ConnectFacebookPageRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookOAuthUrlDto> GetOAuthUrlAsync(
    string redirectUri,
    string state,
    CancellationToken cancellationToken = default);

  Task<IReadOnlyList<FacebookOAuthPageDto>> GetOAuthPagesAsync(
    FacebookOAuthPagesRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookConnectionDto> ConnectOAuthPageAsync(
    ConnectFacebookOAuthPageRequest request,
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

  Task<FacebookPostCommentListDto> GetPostCommentsAsync(
    string pageId,
    string postId,
    string? after = null,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<FacebookCommentActionResultDto> CommentOnPostAsync(
    string pageId,
    string postId,
    CreateFacebookCommentRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookCommentActionResultDto> ReplyToCommentAsync(
    string pageId,
    string commentId,
    ReplyFacebookCommentRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookCommentActionResultDto> ToggleCommentHiddenAsync(
    string pageId,
    string commentId,
    ToggleFacebookCommentHiddenRequest request,
    CancellationToken cancellationToken = default);

  Task<FacebookDeleteResultDto> DeleteCommentAsync(
    string pageId,
    string commentId,
    CancellationToken cancellationToken = default);

  Task<FacebookConversationListDto> GetConversationsAsync(
    string pageId,
    string? after = null,
    int limit = 25,
    CancellationToken cancellationToken = default);

  Task<FacebookMessageListDto> GetConversationMessagesAsync(
    string pageId,
    string conversationId,
    string? before = null,
    int limit = 50,
    CancellationToken cancellationToken = default);

  Task<FacebookMessageSendResultDto> SendMessageAsync(
    string pageId,
    string conversationId,
    SendFacebookMessageRequest request,
    CancellationToken cancellationToken = default);

  Task<MarkConversationReadResultDto> MarkConversationReadAsync(
    string pageId,
    string conversationId,
    CancellationToken cancellationToken = default);
}
