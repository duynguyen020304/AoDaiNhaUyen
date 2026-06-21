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
    int TotalStock,
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
    bool IsPrimary,
    bool IsPublic);

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

/// <summary>Payload for creating a product variant.</summary>
public sealed record CreateVariantRequest
{
    [Required, MaxLength(120)]
    public required string Sku { get; init; }

    [MaxLength(120)]
    public string? VariantName { get; init; }

    [MaxLength(80)]
    public string? Size { get; init; }

    [MaxLength(80)]
    public string? Color { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal Price { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; init; }

    [Range(0, int.MaxValue)]
    public required int StockQty { get; init; }

    public bool IsDefault { get; init; }

    [Required]
    public required string Status { get; init; }
}

/// <summary>Payload for updating product variant details.</summary>
public sealed record UpdateVariantRequest
{
    [Required, MaxLength(120)]
    public required string Sku { get; init; }

    [MaxLength(120)]
    public string? VariantName { get; init; }

    [MaxLength(80)]
    public string? Size { get; init; }

    [MaxLength(80)]
    public string? Color { get; init; }

    [Range(0, double.MaxValue)]
    public required decimal Price { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; init; }

    [Range(0, int.MaxValue)]
    public required int StockQty { get; init; }

    public bool IsDefault { get; init; }

    [Required]
    public required string Status { get; init; }
}

/// <summary>Payload for updating product variant stock.</summary>
public sealed record UpdateVariantStockRequest
{
    [Range(0, int.MaxValue)]
    public required int StockQty { get; init; }
}

/// <summary>Payload for toggling product status.</summary>
public sealed record ToggleProductStatusRequest
{
    [Required]
    public required string Status { get; init; }
}
