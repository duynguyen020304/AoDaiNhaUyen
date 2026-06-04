using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Product item returned in the admin product list.</summary>
public sealed record AdminProductListItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string ProductType,
    string CategoryName,
    string Status,
    bool IsFeatured,
    int VariantCount,
    bool IsDeleted,
    DateTimeOffset CreatedAt);

/// <summary>Full product detail returned for admin edit forms.</summary>
public sealed record AdminProductDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    string ProductType,
    Guid CategoryId,
    string CategoryName,
    string? ShortDescription,
    string? Description,
    string? Material,
    string? Brand,
    string? Origin,
    string? CareInstruction,
    string Status,
    bool IsFeatured,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminVariantResponse> Variants,
    IReadOnlyList<AdminImageResponse> Images);

/// <summary>Product variant in admin responses.</summary>
public sealed record AdminVariantResponse(
    Guid Id,
    string Sku,
    string? VariantName,
    string? Size,
    string? Color,
    decimal Price,
    decimal? SalePrice,
    int StockQty,
    bool IsDefault,
    string Status);

/// <summary>Product image in admin responses.</summary>
public sealed record AdminImageResponse(
    Guid Id,
    string ImageUrl,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

/// <summary>Payload for creating a new product.</summary>
public sealed record CreateProductRequest
{
    [Required, MaxLength(300)]
    public required string Name { get; init; }

    [Required, MaxLength(350)]
    public required string Slug { get; init; }

    [Required]
    public required string ProductType { get; init; }

    [Required]
    public required Guid CategoryId { get; init; }

    [MaxLength(500)]
    public string? ShortDescription { get; init; }

    public string? Description { get; init; }

    [MaxLength(200)]
    public string? Material { get; init; }

    [MaxLength(200)]
    public string? Brand { get; init; }

    [MaxLength(200)]
    public string? Origin { get; init; }

    public string? CareInstruction { get; init; }

    [Required]
    public required string Status { get; init; } = "draft";

    public bool IsFeatured { get; init; }
}

/// <summary>Payload for updating an existing product.</summary>
public sealed record UpdateProductRequest
{
    [Required, MaxLength(300)]
    public required string Name { get; init; }

    [Required, MaxLength(350)]
    public required string Slug { get; init; }

    [Required]
    public required string ProductType { get; init; }

    [Required]
    public required Guid CategoryId { get; init; }

    [MaxLength(500)]
    public string? ShortDescription { get; init; }

    public string? Description { get; init; }

    [MaxLength(200)]
    public string? Material { get; init; }

    [MaxLength(200)]
    public string? Brand { get; init; }

    [MaxLength(200)]
    public string? Origin { get; init; }

    public string? CareInstruction { get; init; }

    [Required]
    public required string Status { get; init; }

    public bool IsFeatured { get; init; }
}

/// <summary>Payload for toggling product status.</summary>
public sealed record ToggleProductStatusRequest
{
    [Required]
    public required string Status { get; init; }
}
