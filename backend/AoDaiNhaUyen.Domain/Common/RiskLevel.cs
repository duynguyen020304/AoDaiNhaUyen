namespace AoDaiNhaUyen.Domain.Common;

/// <summary>Safety risk classification for AI agent actions.</summary>
public enum RiskLevel
{
  /// <summary>Read-only operations — auto-approved.</summary>
  Read = 0,

  /// <summary>Low-risk writes — create drafts, upload images.</summary>
  Low = 1,

  /// <summary>Medium risk — updates, status toggles. Needs confirmation.</summary>
  Medium = 2,

  /// <summary>High risk — deletes, role changes. Needs explicit approval.</summary>
  High = 3,

  /// <summary>Critical — bulk deletes, seed data, config. Human-only.</summary>
  Critical = 4
}
