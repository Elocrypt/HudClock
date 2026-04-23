namespace HudClock.Domain.Claims;

/// <summary>Immutable information about the land claim covering a block position.</summary>
/// <param name="OwnerName">Display name of the claim owner.</param>
internal readonly record struct ClaimInfo(string OwnerName);
