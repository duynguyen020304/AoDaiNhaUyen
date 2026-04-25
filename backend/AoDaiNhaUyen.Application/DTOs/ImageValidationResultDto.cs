namespace AoDaiNhaUyen.Application.DTOs;

public sealed record ImageValidationResultDto(
  bool IsValid,
  string Reason,
  string? Category = null,
  decimal? Confidence = null);
