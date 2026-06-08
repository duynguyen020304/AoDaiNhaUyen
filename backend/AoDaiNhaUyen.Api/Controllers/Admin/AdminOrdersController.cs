using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Order;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminOrdersController(
  IOrderService orderService,
  ICacheInvalidationService cacheInvalidation) : ControllerBase
{
  /// <summary>
  /// Cập nhật trạng thái đơn hàng.
  /// </summary>
  [HttpPatch("{orderId:guid}/status")]
  public async Task<IActionResult> UpdateStatus(
    Guid orderId,
    [FromBody] UpdateOrderStatusRequest request,
    CancellationToken cancellationToken)
  {
    var result = await orderService.UpdateStatusAsync(orderId, request.Status, cancellationToken);
    if (!result.Success)
    {
      return BadRequest(ApiResponseFactory.Failure(
        result.ErrorMessage ?? "Không thể cập nhật trạng thái.",
        result.ErrorCode ?? "update_failed",
        result.ErrorMessage ?? "Lỗi cập nhật trạng thái đơn hàng."));
    }

    await cacheInvalidation.InvalidateOrderRelatedCacheAsync(CancellationToken.None);
    return Ok(ApiResponseFactory.Success(result, "Cập nhật trạng thái thành công."));
  }

  /// <summary>
  /// Tạo shipment cho đơn hàng (chuyển sang trạng thái shipping).
  /// </summary>
  [HttpPost("{orderId:guid}/ship")]
  public async Task<IActionResult> CreateShipment(
    Guid orderId,
    [FromBody] CreateShipmentRequest request,
    CancellationToken cancellationToken)
  {
    var result = await orderService.CreateShipmentAsync(orderId, request.Carrier, request.TrackingNumber, cancellationToken);
    if (!result.Success)
    {
      return BadRequest(ApiResponseFactory.Failure(
        result.ErrorMessage ?? "Không thể tạo shipment.",
        result.ErrorCode ?? "shipment_failed",
        result.ErrorMessage ?? "Lỗi tạo shipment."));
    }

    await cacheInvalidation.InvalidateOrderRelatedCacheAsync(CancellationToken.None);
    return Ok(ApiResponseFactory.Success(result, "Tạo shipment thành công."));
  }

  /// <summary>
  /// Cập nhật trạng thái shipment.
  /// </summary>
  [HttpPatch("shipments/{shipmentId:guid}/status")]
  public async Task<IActionResult> UpdateShipmentStatus(
    Guid shipmentId,
    [FromBody] UpdateShipmentStatusRequest request,
    CancellationToken cancellationToken)
  {
    var result = await orderService.UpdateShipmentStatusAsync(shipmentId, request.Status, cancellationToken);
    if (!result.Success)
    {
      return BadRequest(ApiResponseFactory.Failure(
        result.ErrorMessage ?? "Không thể cập nhật trạng thái shipment.",
        result.ErrorCode ?? "update_failed",
        result.ErrorMessage ?? "Lỗi cập nhật trạng thái shipment."));
    }

    await cacheInvalidation.InvalidateOrderRelatedCacheAsync(CancellationToken.None);
    return Ok(ApiResponseFactory.Success(result, "Cập nhật trạng thái shipment thành công."));
  }
}
