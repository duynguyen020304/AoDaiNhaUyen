using System.ComponentModel.DataAnnotations;

namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Category item returned in the admin category list.</summary>
public sealed record AdminCategoryListItemResponse(
    Guid Id,
    Guid? Parent,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int SortOrder,
    int ProductCount,
    bool IsDeleted,
    DateTimeOffset CreatedAt);

/// <summary>Full category detail returned for admin edit forms.</summary>
public sealed record AdminCategoryDetailResponse(
    Guid Id,
    Guid? Parent,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Payload for creating a new category.</summary>
public sealed record CreateCategoryRequest
{
    [Required, MaxLength(120)]
    public required string Name { get; init; }

    [Required, MaxLength(150)]
    public required string Slug { get; init; }

    public Guid? Parent { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public int SortOrder { get; init; }
}

/// <summary>Payload for updating an existing category.</summary>
public sealed record UpdateCategoryRequest
{
    [Required, MaxLength(120)]
    public required string Name { get; init; }

    [Required, MaxLength(150)]
    public required string Slug { get; init; }

    public Guid? Parent { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    public string? ImageUrl { get; init; }

    public int SortOrder { get; init; }
}
