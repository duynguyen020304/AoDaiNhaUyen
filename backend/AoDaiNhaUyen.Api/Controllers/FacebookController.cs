using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Facebook;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/admin/facebook")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class FacebookController(IFacebookService facebookService) : ControllerBase
{
  [HttpGet("connections")]
  public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
  {
    var connections = await facebookService.GetConnectionsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(connections));
  }

  [HttpPost("connections")]
  public async Task<IActionResult> ConnectPage([FromBody] ConnectFacebookPageRequest request, CancellationToken cancellationToken)
  {
    var result = await facebookService.ConnectPageAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Kết nối Facebook Page thành công."));
  }

  [HttpGet("oauth-url")]
  public async Task<IActionResult> GetOAuthUrl(
    [FromQuery] string redirectUri,
    [FromQuery] string state,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.GetOAuthUrlAsync(redirectUri, state, cancellationToken);
    return Ok(ApiResponseFactory.Success(result));
  }

  [HttpPost("oauth/pages")]
  public async Task<IActionResult> GetOAuthPages([FromBody] FacebookOAuthPagesRequest request, CancellationToken cancellationToken)
  {
    var result = await facebookService.GetOAuthPagesAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result));
  }

  [HttpPost("connections/oauth")]
  public async Task<IActionResult> ConnectOAuthPage([FromBody] ConnectFacebookOAuthPageRequest request, CancellationToken cancellationToken)
  {
    var result = await facebookService.ConnectOAuthPageAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Kết nối Facebook Page thành công."));
  }

  [HttpDelete("connections/{pageId}")]
  public async Task<IActionResult> DisconnectPage(string pageId, CancellationToken cancellationToken)
  {
    await facebookService.DisconnectPageAsync(pageId, cancellationToken);
    return NoContent();
  }

  [HttpGet("{pageId}/info")]
  public async Task<IActionResult> GetPageInfo(string pageId, CancellationToken cancellationToken)
  {
    var page = await facebookService.GetPageInfoAsync(pageId, cancellationToken);
    return Ok(ApiResponseFactory.Success(page));
  }

  [HttpGet("{pageId}/posts")]
  public async Task<IActionResult> GetPosts(
    string pageId,
    [FromQuery] string? cursor,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var posts = await facebookService.GetPostsAsync(pageId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(posts));
  }

  [HttpPost("{pageId}/posts")]
  public async Task<IActionResult> PublishPost(
    string pageId,
    [FromBody] CreateFacebookPostRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.PublishPostAsync(pageId, request, cancellationToken);
    return Created($"/api/admin/facebook/posts/{result.Id}", ApiResponseFactory.Success(result, "Đăng bài Facebook thành công."));
  }

  [HttpPost("{pageId}/photos")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> PublishPhoto(
    string pageId,
    [FromForm] IFormFile file,
    [FromForm] string? caption,
    [FromForm] DateTimeOffset? scheduledPublishTime,
    [FromForm] bool published = true,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateUpload(file, ["image/jpeg", "image/png", "image/gif", "image/webp"], 10 * 1024 * 1024);
    if (validation is not null) return validation;

    await using var stream = file.OpenReadStream();
    var result = await facebookService.PublishPhotoAsync(
      pageId,
      stream,
      file.FileName,
      file.ContentType,
      caption,
      scheduledPublishTime,
      published,
      cancellationToken);

    return Created($"/api/admin/facebook/posts/{result.PostId ?? result.Id}", ApiResponseFactory.Success(result, "Đăng ảnh Facebook thành công."));
  }

  [HttpPost("{pageId}/videos")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> PublishVideo(
    string pageId,
    [FromForm] IFormFile file,
    [FromForm] string? description,
    [FromForm] DateTimeOffset? scheduledPublishTime,
    [FromForm] bool published = true,
    CancellationToken cancellationToken = default)
  {
    var validation = ValidateUpload(file, ["video/mp4", "video/quicktime", "video/webm"], 200 * 1024 * 1024);
    if (validation is not null) return validation;

    await using var stream = file.OpenReadStream();
    var result = await facebookService.PublishVideoAsync(
      pageId,
      stream,
      file.FileName,
      file.ContentType,
      description,
      scheduledPublishTime,
      published,
      cancellationToken);

    return Created($"/api/admin/facebook/posts/{result.PostId ?? result.Id}", ApiResponseFactory.Success(result, "Đăng video Facebook thành công."));
  }

  [HttpGet("posts/{postId}")]
  public async Task<IActionResult> GetPost(string postId, CancellationToken cancellationToken)
  {
    var post = await facebookService.GetPostAsync(postId, cancellationToken);
    return Ok(ApiResponseFactory.Success(post));
  }

  [HttpPut("posts/{postId}")]
  public async Task<IActionResult> UpdatePost(
    string postId,
    [FromBody] UpdateFacebookPostRequest request,
    CancellationToken cancellationToken)
  {
    var post = await facebookService.UpdatePostAsync(postId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(post, "Cập nhật bài viết Facebook thành công."));
  }

  [HttpDelete("posts/{postId}")]
  public async Task<IActionResult> DeletePost(string postId, CancellationToken cancellationToken)
  {
    await facebookService.DeletePostAsync(postId, cancellationToken);
    return NoContent();
  }

  [HttpGet("{pageId}/posts/{postId}/comments")]
  public async Task<IActionResult> GetPostComments(
    string pageId,
    string postId,
    [FromQuery] string? after,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var comments = await facebookService.GetPostCommentsAsync(pageId, postId, after, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(comments));
  }

  [HttpPost("{pageId}/posts/{postId}/comments")]
  public async Task<IActionResult> CommentOnPost(
    string pageId,
    string postId,
    [FromBody] CreateFacebookCommentRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.CommentOnPostAsync(pageId, postId, request, cancellationToken);
    return Created($"/api/admin/facebook/{pageId}/comments/{result.Id}", ApiResponseFactory.Success(result, "Đã bình luận bài viết."));
  }

  [HttpPost("{pageId}/comments/{commentId}/replies")]
  public async Task<IActionResult> ReplyToComment(
    string pageId,
    string commentId,
    [FromBody] ReplyFacebookCommentRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.ReplyToCommentAsync(pageId, commentId, request, cancellationToken);
    return Created($"/api/admin/facebook/{pageId}/comments/{result.Id}", ApiResponseFactory.Success(result, "Đã trả lời bình luận."));
  }

  [HttpPatch("{pageId}/comments/{commentId}/visibility")]
  public async Task<IActionResult> ToggleCommentVisibility(
    string pageId,
    string commentId,
    [FromBody] ToggleFacebookCommentHiddenRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.ToggleCommentHiddenAsync(pageId, commentId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, request.IsHidden ? "Đã ẩn bình luận." : "Đã hiện bình luận."));
  }

  [HttpDelete("{pageId}/comments/{commentId}")]
  public async Task<IActionResult> DeleteComment(
    string pageId,
    string commentId,
    CancellationToken cancellationToken)
  {
    await facebookService.DeleteCommentAsync(pageId, commentId, cancellationToken);
    return NoContent();
  }

  [HttpGet("{pageId}/conversations")]
  public async Task<IActionResult> GetConversations(
    string pageId,
    [FromQuery] string? after,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var conversations = await facebookService.GetConversationsAsync(pageId, after, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(conversations));
  }

  [HttpGet("{pageId}/conversations/{conversationId}/messages")]
  public async Task<IActionResult> GetConversationMessages(
    string pageId,
    string conversationId,
    [FromQuery] string? before,
    [FromQuery] int limit = 50,
    CancellationToken cancellationToken = default)
  {
    var messages = await facebookService.GetConversationMessagesAsync(pageId, conversationId, before, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(messages));
  }

  [HttpPost("{pageId}/conversations/{conversationId}/messages")]
  public async Task<IActionResult> SendMessage(
    string pageId,
    string conversationId,
    [FromBody] SendFacebookMessageRequest request,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.SendMessageAsync(pageId, conversationId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã gửi tin nhắn."));
  }

  [HttpPost("{pageId}/conversations/{conversationId}/read")]
  public async Task<IActionResult> MarkConversationRead(
    string pageId,
    string conversationId,
    CancellationToken cancellationToken)
  {
    var result = await facebookService.MarkConversationReadAsync(pageId, conversationId, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã đánh dấu đã đọc."));
  }

  private static IActionResult? ValidateUpload(IFormFile? file, IReadOnlyCollection<string> allowedContentTypes, long maxBytes)
  {
    if (file is null || file.Length == 0)
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File không hợp lệ", "bad_request", "Vui lòng chọn file hợp lệ."));
    }

    if (file.Length > maxBytes)
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File quá lớn", "file_too_large", "Vui lòng chọn file nhỏ hơn giới hạn cho phép."));
    }

    if (!allowedContentTypes.Contains(file.ContentType))
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("Định dạng file không hỗ trợ", "unsupported_media_type", "Vui lòng chọn đúng định dạng file."));
    }

    return null;
  }
}
