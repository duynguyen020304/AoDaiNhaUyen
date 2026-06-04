namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record OrderItemDto(
    Guid Id,
    Guid? ProductId,
    Guid? VariantId,
    string ProductName,
    string? Sku,
    string? Size,
    string? Color,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ImageUrl,
    bool IsCustomTailoring,
    Guid? MeasurementProfileId,
    string? CustomMeasurementsJson,
    string? Note);
