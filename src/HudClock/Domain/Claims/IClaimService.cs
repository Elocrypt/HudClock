using Vintagestory.API.MathTools;

namespace HudClock.Domain.Claims;

/// <summary>Read access to land-claim data at a block position.</summary>
internal interface IClaimService
{
    /// <summary>Look up the claim covering <paramref name="pos"/>; null if unclaimed.</summary>
    ClaimInfo? GetClaimAt(BlockPos pos);
}
