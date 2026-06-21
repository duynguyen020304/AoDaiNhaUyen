<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/BlogPost

## Purpose
Blog content DTOs for the editorial system: blog post list items and full detail, structured block content, categories, image uploads, and create/update request payloads. Used by `IBlogPostService` and `IBlogCategoryService`.

## Key Files
| File | Description |
|------|-------------|
| `BlogPostListItemDto.cs` | List item: Id, Slug, Title, Excerpt, CoverImageUrl, CategoryName, PublishedAt, Status |
| `BlogPostDto.cs` | Full post detail: all list fields + Blocks list + Tags + Author info |
| `BlogBlockDto.cs` | A single content block: Type (text/image/quote/etc.), Content, metadata |
| `BlogCategoryDto.cs` | Blog category: Id, Name, Slug, PostCount |
| `BlogImageUploadResponse.cs` | Result of a blog image upload: ImageUrl, FileKey |
| `BlogImageVisibilityDto.cs` | Image visibility state for blog images: ImageId, IsPublic |
| `CreateBlogPostRequest.cs` | Create request: Title, Slug, CategoryId, Excerpt, CoverImageUrl, Blocks, Tags, Status |
| `UpdateBlogPostRequest.cs` | Update request: same shape as create with all fields updatable |
| `GenerateBlogDraftRequest.cs` | AI draft generation request: Topic, Keywords, Tone, TargetLength |

## For AI Agents

### Working In This Directory
- Blog posts use a block-based content model (`BlogBlockDto`) rather than raw HTML — each block has a `Type` and typed `Content`
- `GenerateBlogDraftRequest` feeds into `IBlogAiDraftService` which calls the LLM to produce a draft post
- Image URLs in blog DTOs may be private S3 keys resolved via `IImageVisibilityService` — do not hardcode S3 paths
- Status values follow the `BlogPostStatus` domain enum (draft, published, archived)

### Common Patterns
```csharp
// Block-based post content:
var blocks = new List<BlogBlockDto>
{
    new("heading", "Áo Dài Nhã Uyên"),
    new("paragraph", "Giới thiệu về bộ sưu tập...")
};
```

## Dependencies
### Internal
- `IBlogPostService`, `IBlogCategoryService`, `IBlogAiDraftService` — consume these DTOs
- `IBlogImageVisibilityService` — resolves image URLs in post detail responses

<!-- MANUAL: -->
