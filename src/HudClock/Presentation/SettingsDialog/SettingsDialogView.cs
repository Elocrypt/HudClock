using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock.Presentation.SettingsDialog;

/// <summary>
/// In-game mod settings dialog. Redesigned in 4.0 with section headers
/// grouping related options (Display, Time &amp; Date, Weather, World Events,
/// Multiplayer) instead of 3.x's undifferentiated two-column grid.
/// </summary>
/// <remarks>
/// Fires <see cref="SettingsChanged"/> on close so other dialogs can rebuild
/// their layouts with the updated settings. The dialog mutates
/// <see cref="HudClockSettings"/> directly via each control's change
/// callback — no intermediate working copy.
/// </remarks>
internal sealed class SettingsDialogView : GuiDialog
{
    // Horizontal layout: [ margin | label column | gap | control column | margin ]
    private const int OuterMargin     = 20;
    private const int LabelX          = OuterMargin + 10; // indent label under the section header
    private const int LabelWidth      = 240;
    private const int ColumnGap       = 20;
    private const int ControlX        = LabelX + LabelWidth + ColumnGap;
    private const int ControlWidth    = 240;

    // Vertical layout: sections contain rows; rows are consistent height.
    private const int SectionHeaderHeight = 28;
    private const int RowHeight           = 32;   // switch + dropdown both fit comfortably
    private const int RowGap              = 4;    // gap between rows within a section
    private const int SectionGap          = 14;   // gap between sections
    private const int TopY                = 40;

    private readonly HudClockSettings _settings;
    private readonly ModLog _log;

    public SettingsDialogView(ICoreClientAPI capi, HudClockSettings settings, ModLog log) : base(capi)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public override string ToggleKeyCombinationCode => "hudclock:settingsdialog";

    /// <summary>Raised when the user closes the dialog; listeners rebuild in response.</summary>
    public event EventHandler? SettingsChanged;

    /// <inheritdoc />
    public override bool TryOpen()
    {
        try
        {
            Build();
            return base.TryOpen();
        }
        catch (Exception ex)
        {
            // VS 1.22 + .NET 10 can produce intermittent fatal-state errors from inside
            // the native OpenAL audio path when a GUI dialog's open-sound plays. The
            // failure originates in Vintagestory.Client.NoObf.OggDecoder / AudioOpenAL,
            // not in user code. Catching broadly here keeps the game alive; the dialog
            // may not have fully opened, but the user can retry. Intentionally catching
            // System.Exception rather than a specific type because the runtime label
            // can vary across .NET versions.
            _log.Error("Settings dialog failed to open: {0}. "
                + "If this is an audio subsystem failure, retry the hotkey.", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public override bool TryClose()
    {
        bool result = base.TryClose();
        if (result) SettingsChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    private void Build()
    {
        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterFixed);

        // Background sizes itself to fit all children. We collect child bounds in
        // _buildChildBounds as the helpers register them, then wire them into
        // bgBounds before Compose(). Without this wiring, VS's buildBoundsFromChildren
        // throws "Couldn't build bounds from children", which on 1.22 manifests as a
        // native AccessViolationException.
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(10.0, 10.0);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var childBounds = new System.Collections.Generic.List<ElementBounds>();
        _buildChildBounds = childBounds;

        GuiComposer composer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get(LangKeys.Settings.Title), OnClose);

        int y = TopY;

        y = AddSection(composer, y, LangKeys.Settings.SectionDisplay, (c, yy) =>
        {
            yy = AddDropDown(c, yy, LangKeys.Settings.HudPosition,
                Enum.GetNames(typeof(HudAnchor)),
                HudAnchorLocales(),
                (int)_settings.Display.Anchor,
                "anchor",
                (code, sel) => _settings.Display.Anchor = ParseEnum<HudAnchor>(code));
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowRoom, "room",
                _settings.Room.ShowRoomIndicator,
                v => _settings.Room.ShowRoomIndicator = v);
            return yy;
        });

        y = AddSection(composer, y, LangKeys.Settings.SectionAppearance, (c, yy) =>
        {
            yy = AddDropDown(c, yy, LangKeys.Settings.IconTheme,
                Enum.GetNames(typeof(IconTheme)),
                IconThemeLocales(),
                (int)_settings.Appearance.IconTheme,
                "icon-theme",
                (code, sel) => _settings.Appearance.IconTheme = ParseEnum<IconTheme>(code));
            return yy;
        });

        y = AddSection(composer, y, LangKeys.Settings.SectionTime, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowDate, "date",
                _settings.Time.ShowDate, v => _settings.Time.ShowDate = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowTime, "time",
                _settings.Time.ShowTime, v => _settings.Time.ShowTime = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowRealtime, "realtime",
                _settings.Time.ShowRealtime, v => _settings.Time.ShowRealtime = v);
            yy = AddDropDown(c, yy, LangKeys.Settings.TimeFormat,
                Enum.GetNames(typeof(TimeFormat)),
                TimeFormatLocales(),
                (int)_settings.Time.Format,
                "timeformat",
                (code, sel) => _settings.Time.Format = ParseEnum<TimeFormat>(code));
            return yy;
        });

        y = AddSection(composer, y, LangKeys.Settings.SectionWeather, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowSeason, "season",
                _settings.Weather.ShowSeason, v => _settings.Weather.ShowSeason = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowTemperature, "temp",
                _settings.Weather.ShowTemperature, v => _settings.Weather.ShowTemperature = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.Fahrenheit, "fahrenheit",
                _settings.Weather.Fahrenheit, v => _settings.Weather.Fahrenheit = v);
            yy = AddDropDown(c, yy, LangKeys.Settings.Wind,
                Enum.GetNames(typeof(WindDisplay)),
                WindLocales(),
                (int)_settings.Weather.Wind,
                "wind",
                (code, sel) => _settings.Weather.Wind = ParseEnum<WindDisplay>(code));
            return yy;
        });

        y = AddSection(composer, y, LangKeys.Settings.SectionEvents, (c, yy) =>
        {
            yy = AddDropDown(c, yy, LangKeys.Settings.StormDialog,
                Enum.GetNames(typeof(StormDisplay)),
                StormLocales(),
                (int)_settings.Storm.Display,
                "storm",
                (code, sel) => _settings.Storm.Display = ParseEnum<StormDisplay>(code));
            yy = AddDropDown(c, yy, LangKeys.Settings.StormAnchor,
                Enum.GetNames(typeof(HudAnchor)),
                HudAnchorLocales(),
                (int)_settings.Storm.Anchor,
                "storm-anchor",
                (code, sel) => _settings.Storm.Anchor = ParseEnum<HudAnchor>(code));
            yy = AddDropDown(c, yy, LangKeys.Settings.RiftDialog,
                Enum.GetNames(typeof(RiftDisplay)),
                RiftLocales(),
                (int)_settings.Rift.Display,
                "rift",
                (code, sel) => _settings.Rift.Display = ParseEnum<RiftDisplay>(code));
            return yy;
        });

        _ = AddSection(composer, y, LangKeys.Settings.SectionMultiplayer, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowOnlinePlayers, "players",
                _settings.Multiplayer.ShowOnlinePlayerCount,
                v => _settings.Multiplayer.ShowOnlinePlayerCount = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowClaim, "claim",
                _settings.Claim.ShowClaimedArea,
                v => _settings.Claim.ShowClaimedArea = v);
            return yy;
        });

        // Wire every collected child bounds into bgBounds so FitToChildren can measure.
        foreach (ElementBounds child in childBounds)
        {
            bgBounds.WithChild(child);
        }

        SingleComposer = composer.Compose();
        _buildChildBounds = null;
    }

    // --- Layout helpers ---

    // Total content width = label column + gap + control column. Used for section headers
    // so they span both columns without introducing a layout element wider than any child.
    private const int SectionHeaderWidth = LabelWidth + ColumnGap + ControlWidth;

    private int AddSection(GuiComposer composer, int startY, string titleKey,
        System.Func<GuiComposer, int, int> build)
    {
        ElementBounds titleBounds = ElementBounds.Fixed(LabelX, startY, SectionHeaderWidth, SectionHeaderHeight);
        composer.AddStaticText(
            Lang.Get(titleKey),
            CairoFont.WhiteSmallishText().WithWeight(Cairo.FontWeight.Bold),
            titleBounds);
        _buildChildBounds?.Add(titleBounds);

        int y = startY + SectionHeaderHeight + RowGap;
        y = build(composer, y);
        return y + SectionGap;
    }

    private int AddSwitch(GuiComposer composer, int y, string labelKey, string nameSuffix,
        bool initial, System.Action<bool> onChange)
    {
        ElementBounds labelBounds = ElementBounds.Fixed(LabelX, y, LabelWidth, RowHeight);
        ElementBounds controlBounds = ElementBounds.Fixed(ControlX, y + 4, ControlWidth, RowHeight - 8);

        composer.AddStaticText(Lang.Get(labelKey), CairoFont.WhiteSmallText(), labelBounds);
        composer.AddSwitch(onChange, controlBounds, "switch-" + nameSuffix);

        _buildChildBounds?.Add(labelBounds);
        _buildChildBounds?.Add(controlBounds);

        _pendingSwitchStates[nameSuffix] = initial;

        return y + RowHeight + RowGap;
    }

    private int AddDropDown(GuiComposer composer, int y, string labelKey,
        string[] codes, string[] labels, int initialIndex, string nameSuffix,
        SelectionChangedDelegate onChange)
    {
        ElementBounds labelBounds = ElementBounds.Fixed(LabelX, y, LabelWidth, RowHeight);
        ElementBounds controlBounds = ElementBounds.Fixed(ControlX, y + 2, ControlWidth, RowHeight - 4);

        composer.AddStaticText(Lang.Get(labelKey), CairoFont.WhiteSmallText(), labelBounds);
        composer.AddDropDown(codes, labels, initialIndex, onChange, controlBounds, "dropdown-" + nameSuffix);

        _buildChildBounds?.Add(labelBounds);
        _buildChildBounds?.Add(controlBounds);

        return y + RowHeight + RowGap;
    }

    // Collected during Build; applied to the composed UI once the controls exist.
    private readonly System.Collections.Generic.Dictionary<string, bool> _pendingSwitchStates = new();

    // Collected during Build; wired into bgBounds' WithChildren before Compose().
    // Using an instance field lets the helper methods append to it without changing
    // their signatures (they're already called via nested closures from Build).
    private System.Collections.Generic.List<ElementBounds>? _buildChildBounds;

    /// <inheritdoc />
    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        // Apply initial switch states once the composer has instantiated the controls.
        if (SingleComposer is null) return;
        foreach (var kvp in _pendingSwitchStates)
        {
            var sw = SingleComposer.GetSwitch("switch-" + kvp.Key);
            if (sw is not null) sw.On = kvp.Value;
        }
    }

    private void OnClose() => TryClose();

    // --- Enum helpers ---

    private static T ParseEnum<T>(string code) where T : struct, Enum
        => (T)Enum.Parse(typeof(T), code);

    private static string[] HudAnchorLocales() => LocalizeEnumNames<HudAnchor>(LangKeys.Settings.HudPositionStem);
    private static string[] IconThemeLocales() => LocalizeEnumNames<IconTheme>(LangKeys.Settings.IconThemeStem);
    private static string[] TimeFormatLocales() => LocalizeEnumNames<TimeFormat>(LangKeys.Settings.TimeFormatStem);
    private static string[] WindLocales() => LocalizeEnumNames<WindDisplay>(LangKeys.Settings.WindStem);
    private static string[] StormLocales() => LocalizeEnumNames<StormDisplay>(LangKeys.Settings.StormStem);
    private static string[] RiftLocales() => LocalizeEnumNames<RiftDisplay>(LangKeys.Settings.RiftStem);

    /// <summary>
    /// Build a localized display array for a dropdown whose codes come from
    /// <c>Enum.GetNames&lt;T&gt;()</c>. The suffix is obtained via the enum's
    /// <c>ToKeySuffix()</c> extension so a future enum rename doesn't break
    /// lang files.
    /// </summary>
    private static string[] LocalizeEnumNames<T>(string stem) where T : struct, Enum
    {
        string[] names = Enum.GetNames(typeof(T));
        string[] result = new string[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var value = (T)Enum.Parse(typeof(T), names[i]);
            string suffix = SuffixFor(value);
            result[i] = Lang.Get(stem + suffix);
        }
        return result;
    }

    private static string SuffixFor<T>(T value) where T : struct, Enum
    {
        // Dispatch by runtime type to the correct ToKeySuffix extension.
        return value switch
        {
            HudAnchor a      => a.ToKeySuffix(),
            IconTheme it     => it.ToKeySuffix(),
            TimeFormat t     => t.ToKeySuffix(),
            WindDisplay w    => w.ToKeySuffix(),
            StormDisplay s   => s.ToKeySuffix(),
            RiftDisplay r    => r.ToKeySuffix(),
            _ => value.ToString().ToLowerInvariant(),
        };
    }
}
