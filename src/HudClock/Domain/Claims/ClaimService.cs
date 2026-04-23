using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Domain.Claims;

/// <summary>Production <see cref="IClaimService"/> wrapping <c>IWorldAccessor.Claims</c>.</summary>
internal sealed class ClaimService : IClaimService
{
    private readonly ICoreClientAPI _api;

    public ClaimService(ICoreClientAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public ClaimInfo? GetClaimAt(BlockPos pos)
    {
        if (pos is null) throw new ArgumentNullException(nameof(pos));

        LandClaim[]? claims = _api.World.Claims.Get(pos);
        if (claims is null || claims.Length == 0) return null;

        return new ClaimInfo(claims[0].LastKnownOwnerName ?? string.Empty);
    }
}
