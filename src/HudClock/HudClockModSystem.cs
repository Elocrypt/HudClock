using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HudClock;

/// <summary>
/// Composition root and entry point for the HUD Clock mod.
/// </summary>
/// <remarks>
/// All service wiring will happen in <see cref="StartClientSide"/> once the infrastructure and
/// domain layers are in place. Until then this class exists as a scaffolding placeholder that
/// verifies the toolchain (build, deploy, launch, mod discovery) works end-to-end.
/// </remarks>
public sealed class HudClockModSystem : ModSystem
{
    /// <inheritdoc />
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    /// <inheritdoc />
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Logger.Notification("[HudClock] {0} scaffolding loaded.", Mod.Info.Version);
    }
}
