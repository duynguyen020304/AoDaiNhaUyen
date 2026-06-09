using System.Text.Json;

namespace AoDaiNhaUyen.Application.DTOs.BlogPost;

public sealed record BlogBlockDto(string Type, JsonElement Data);
