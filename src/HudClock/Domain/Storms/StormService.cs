using System;
using System.Reflection;
using HudClock.Core;
using HudClock.Infrastructure.Reflection;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace HudClock.Domain.Storms;

/// <summary>
/// Production <see cref="IStormService"/>. Reads private fields on
/// <see cref="SystemTemporalStability"/> via <see cref="FieldAccessor{TTarget,TField}"/>
/// for the outer hop and cached <see cref="FieldInfo"/> for the inner fields on
/// the returned data object (whose type is VS-internal and not referenced here
/// by name, so a rename inside VS doesn't break compilation).
/// </summary>
/// <remarks>
/// Reflection setup happens once at construction. If any field is missing we
/// log once and return <see cref="StormStatus.Unavailable"/> forever after,
/// rather than spamming errors on every tick.
/// </remarks>
internal sealed class StormService : IStormService
{
    // VS internal field names. These are the risk vector for a 1.22 API change.
    private const string DataFieldName = "data";
    private const string NextStormTotalDaysFieldName = "nextStormTotalDays";
    private const string NowStormActiveFieldName = "nowStormActive";

    // Config key for the world-level storm toggle.
    private const string StormsConfigKey = "temporalStorms";
    private const string StormsDisabledValue = "off";

    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;

    private readonly SystemTemporalStability? _stormSystem;
    private readonly FieldAccessor<SystemTemporalStability, object>? _dataAccessor;

    // Cached on first successful data read; the data object's concrete type is
    // VS-internal, so we discover its field layout reflectively at runtime.
    private FieldInfo? _nextStormTotalDaysField;
    private FieldInfo? _nowStormActiveField;

    private bool _degraded;

    public StormService(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _stormSystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
        _dataAccessor = FieldAccessor<SystemTemporalStability, object>.TryCreate(DataFieldName);

        if (_stormSystem is null || _dataAccessor is null)
        {
            _log.Warning("Storm system unavailable; storm HUD will be disabled this session.");
            _degraded = true;
        }
    }

    /// <inheritdoc />
    public StormStatus GetStatus()
    {
        if (_degraded || _stormSystem is null || _dataAccessor is null)
        {
            return StormStatus.Unavailable;
        }

        bool enabled = _api.World.Config.GetString(StormsConfigKey) != StormsDisabledValue;
        if (!enabled)
        {
            return new StormStatus(Enabled: false, NowActive: false, DaysUntilNext: 0);
        }

        try
        {
            object data = _dataAccessor.Get(_stormSystem);
            EnsureNestedFields(data.GetType());

            double nextStormTotalDays = (double)_nextStormTotalDaysField!.GetValue(data)!;
            bool nowActive = (bool)_nowStormActiveField!.GetValue(data)!;
            double daysLeft = nextStormTotalDays - _api.World.Calendar.TotalDays;

            return new StormStatus(Enabled: true, NowActive: nowActive, DaysUntilNext: daysLeft);
        }
        catch (Exception ex)
        {
            // One-shot failure mode: log then degrade for the rest of the session.
            _log.Error("Storm data read failed ({0}); degrading.", ex.Message);
            _degraded = true;
            return StormStatus.Unavailable;
        }
    }

    private void EnsureNestedFields(Type dataType)
    {
        if (_nextStormTotalDaysField is not null && _nowStormActiveField is not null) return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        _nextStormTotalDaysField ??= dataType.GetField(NextStormTotalDaysFieldName, flags)
            ?? throw new InvalidOperationException($"Field '{NextStormTotalDaysFieldName}' missing on {dataType.FullName}.");
        _nowStormActiveField ??= dataType.GetField(NowStormActiveFieldName, flags)
            ?? throw new InvalidOperationException($"Field '{NowStormActiveFieldName}' missing on {dataType.FullName}.");
    }
}
