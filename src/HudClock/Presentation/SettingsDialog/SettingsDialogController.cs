using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Infrastructure.Input;
using HudClock.Resources;
using Vintagestory.API.Client;

namespace HudClock.Presentation.SettingsDialog;

/// <summary>
/// Owns the in-game settings dialog and its open/close keybind. Exposes
/// <see cref="SettingsChanged"/> for sibling controllers to rebuild on.
/// </summary>
internal sealed class SettingsDialogController : IDisposable
{
    private readonly ICoreClientAPI _api;
    private readonly SettingsDialogView _view;
    private readonly ModLog _log;
    private bool _disposed;

    public SettingsDialogController(
        ICoreClientAPI api,
        HudClockSettings settings,
        KeybindRegistry keybinds,
        ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _view = new SettingsDialogView(api, settings, log);
        _view.SettingsChanged += (_, e) => SettingsChanged?.Invoke(this, e);

        keybinds.Register(
            code: _view.ToggleKeyCombinationCode,
            translationKey: LangKeys.Keybind.SettingsDialog,
            binding: settings.Keybinds.OpenSettings,
            handler: Toggle);
    }

    /// <summary>Raised when the user closes the dialog.</summary>
    public event EventHandler? SettingsChanged;

    private bool Toggle(Vintagestory.API.Client.KeyCombination _)
    {
        if (_view.IsOpened()) _view.TryClose();
        else _view.TryOpen();
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view.TryClose();
        _view.Dispose();
    }
}
