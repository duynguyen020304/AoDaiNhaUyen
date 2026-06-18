namespace AoDaiNhaUyen.Application.DTOs.Admin;

/// <summary>Live Hermes monitor feed snapshot.</summary>
public sealed record HermesFeedSnapshotResponse(
  IReadOnlyList<HermesFeedItemResponse> Items,
  HermesFeedHeartbeatResponse? Heartbeat,
  DateTimeOffset GeneratedAt);

/// <summary>One store event with related Hermes messages.</summary>
public sealed record HermesFeedItemResponse(
  Guid EventId,
  string StoreMessage,
  DateTimeOffset StoreTime,
  string EventType,
  string EventStatus,
  IReadOnlyList<HermesFeedHermesMessageResponse> HermesMessages,
  string? RunStatus);

/// <summary>One safe Hermes agent feed message.</summary>
public sealed record HermesFeedHermesMessageResponse(
  string Kind,
  string? Title,
  string Summary,
  DateTimeOffset Time,
  string? Status,
  string? Severity);

/// <summary>Latest Hermes worker heartbeat for the live monitor.</summary>
public sealed record HermesFeedHeartbeatResponse(
  string RunnerName,
  string Status,
  int ActiveJobs,
  DateTimeOffset RecordedAt);
