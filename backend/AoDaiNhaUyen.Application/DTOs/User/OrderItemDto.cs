namespace AoDaiNhaUyen.Application.DTOs.User;

public sealed record OrderItemDto(
    long Id,
    long? ProductId,
    long? VariantId,
    string ProductName,
    string? Sku,
    string? Size,
    string? Color,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ImageUrl,
    bool IsCustomTailoring,
    long? MeasurementProfileId,
    string? CustomMeasurementsJson,
    string? Note);
