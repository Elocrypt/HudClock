using System;
using System.Reflection;
using HudClock.Core;
using HudClock.Infrastructure.Reflection;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace HudClock.Domain.Rifts;

/// <summary>
/// Production <see cref="IRiftService"/>. Reads <c>ModSystemRiftWeather.curPattern</c>
/// via <see cref="FieldAccessor{TTarget,TField}"/>, then pulls the <c>Code</c>
/// member (field or property, whichever VS declares) with cached reflection.
/// </summary>
internal sealed class RiftService : IRiftService
{
    private const string CurPatternFieldName = "curPattern";
    private const string CodeMemberName = "Code";
    private const string RiftsConfigKey = "temporalRifts";
    private const string RiftsDisabledValue = "off";

    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;

    private readonly ModSystemRiftWeather? _riftSystem;
    private readonly FieldAccessor<ModSystemRiftWeather, object>? _curPatternAccessor;

    // Resolved lazily from the returned pattern's runtime type. Either field or
    // property is populated on first successful read; never both.
    private FieldInfo? _codeField;
    private PropertyInfo? _codeProperty;

    private bool _degraded;
    private string? _lastCode;

    public RiftService(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _riftSystem = api.ModLoader.GetModSystem<ModSystemRiftWeather>();
        _curPatternAccessor = FieldAccessor<ModSystemRiftWeather, object>.TryCreate(CurPatternFieldName);

        if (_riftSystem is null || _curPatternAccessor is null)
        {
            _log.Notification("Rift weather system not available; rift HUD line will be hidden.");
            _degraded = true;
        }
    }

    /// <inheritdoc />
    public bool IsAvailable => !_degraded && _api.World.Config.GetString(RiftsConfigKey) != RiftsDisabledValue;

    /// <inheritdoc />
    public string? GetCurrentActivityCode()
    {
        if (_degraded || _riftSystem is null || _curPatternAccessor is null) return null;

        try
        {
            object pattern = _curPatternAccessor.Get(_riftSystem);
            EnsureCodeMember(pattern.GetType());

            string? code = _codeField is not null
                ? (string?)_codeField.GetValue(pattern)
                : (string?)_codeProperty!.GetValue(pattern);

            if (code is not null && code != _lastCode)
            {
                _log.Debug("Rift pattern changed: {0}", code);
                _lastCode = code;
            }
            return code;
        }
        catch (Exception ex)
        {
            _log.Error("Rift data read failed ({0}); degrading.", ex.Message);
            _degraded = true;
            return null;
        }
    }

    private void EnsureCodeMember(Type patternType)
    {
        if (_codeField is not null || _codeProperty is not null) return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        _codeField = patternType.GetField(CodeMemberName, flags);
        if (_codeField is null)
        {
            _codeProperty = patternType.GetProperty(CodeMemberName, flags);
        }
        if (_codeField is null && _codeProperty is null)
        {
            throw new InvalidOperationException($"Member '{CodeMemberName}' missing on {patternType.FullName}.");
        }
    }
}
