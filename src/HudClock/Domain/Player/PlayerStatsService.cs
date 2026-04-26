using System;
using HudClock.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace HudClock.Domain.Player;

/// <summary>
/// Production <see cref="IPlayerStatsService"/>. Reads directly from the
/// client player's <c>WatchedAttributes</c>.
/// </summary>
/// <remarks>
/// <para>
/// Body temperature reads from the <c>bodyTemp.bodytemp</c> watched
/// attribute — the canonical raw temperature the survival system
/// maintains internally. This value sits at <c>NormalBodyTemperature
/// + 4</c> (=41 by default) under comfortable conditions and drops as
/// the player gets cold; the survival mod's <c>EntityBehaviorBodyTemperature</c>
/// triggers freezing damage when this value falls more than 4 °C
/// below normal.
/// </para>
/// <para>
/// The character GUI's "Body Temperature" line shows a
/// <i>display-shifted</i> version computed client-side from this raw
/// value plus clothing, wetness, and environmental inputs. There is no
/// single watched attribute that holds the displayed value — it's
/// assembled in the GUI render path. We deliberately don't replicate
/// that formula. Instead the HUD shows a comfort signal derived from
/// the raw value, which tracks live with all the survival-system
/// inputs and gives the player actionable info ("are you cold?")
/// rather than a number that disagrees with vanilla.
/// </para>
/// <para>
/// Intoxication is a top-level <c>WatchedAttribute</c> float — confirmed
/// in <c>Collectible.cs</c> and <c>BehaviorHunger.cs</c>.
/// </para>
/// </remarks>
internal sealed class PlayerStatsService : IPlayerStatsService
{
    private const string BodyTempTreeKey = "bodyTemp";
    private const string BodyTempValueKey = "bodytemp";
    private const string IntoxicationKey = "intoxication";

    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;

    // One-shot warning latches: complain once per session about missing
    // attributes, then stay quiet.
    private bool _warnedBodyTempMissing;
    private bool _warnedIntoxMissing;

    public PlayerStatsService(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public float? BodyTemperatureCelsius
    {
        get
        {
            Entity? entity = _api.World?.Player?.Entity;
            if (entity is null) return null;

            // Re-fetch the tree on every call. Don't cache it — VS may
            // replace the subtree reference on resync, and a held
            // reference would point at a stale snapshot.
            ITreeAttribute? bodyTemp = entity.WatchedAttributes?.GetTreeAttribute(BodyTempTreeKey);
            if (bodyTemp is null)
            {
                if (!_warnedBodyTempMissing)
                {
                    _log.Notification("Body temperature attribute not present on player entity; line will stay hidden.");
                    _warnedBodyTempMissing = true;
                }
                return null;
            }

            float value = bodyTemp.GetFloat(BodyTempValueKey, float.NaN);
            return float.IsNaN(value) ? null : value;
        }
    }

    /// <inheritdoc />
    public float? Intoxication
    {
        get
        {
            ITreeAttribute? watched = _api.World?.Player?.Entity?.WatchedAttributes;
            if (watched is null) return null;

            if (!watched.HasAttribute(IntoxicationKey))
            {
                if (!_warnedIntoxMissing)
                {
                    _log.Notification("Intoxication attribute not present on player entity; line will stay hidden.");
                    _warnedIntoxMissing = true;
                }
                return null;
            }

            return watched.GetFloat(IntoxicationKey);
        }
    }
}
