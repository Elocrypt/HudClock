using System;
using System.Collections.Generic;
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
/// <para>
/// Fires <see cref="SettingsChanged"/> on close so other dialogs can rebuild
/// their layouts with the updated settings. The dialog mutates
/// <see cref="HudClockSettings"/> directly via each control's change
/// callback — no intermediate working copy.
/// </para>
/// <para>
/// The whole option stack lives inside a single scrollable
/// <see cref="GuiElementContainer"/> (clip + vertical scrollbar) so the dialog
/// never runs off a short display or at large GUI scale. This is the pattern
/// VS itself uses for scrolling GUIs: every row — headers, labels, switches,
/// dropdowns — is added to the container so they scroll together. Adding them
/// straight to the composer instead would bake the static text into the
/// background at compose time, leaving labels behind while only the live
/// controls scroll.
/// </para>
/// </remarks>
internal sealed class SettingsDialogView : GuiDialog
{
    // Horizontal layout, relative to the scroll container's content box:
    //   [ label column | gap | control column ]
    private const int LabelX       = 0;
    private const int LabelWidth   = 240;
    private const int ColumnGap    = 20;
    private const int ControlX     = LabelX + LabelWidth + ColumnGap;
    private const int ControlWidth = 240;
    private const int ContentWidth = ControlX + ControlWidth;
    // Breathing room between the widest row's right edge (a dropdown ends at
    // ContentWidth) and the clip's right edge, so the control's border isn't
    // shaved at fractional GUI scale. Added on top of the scroll container's
    // HalfPadding inset when sizing the inset width.
    private const int ContentGutter = 6;

    // Vertical layout: sections contain rows; rows are consistent height.
    private const int SectionHeaderHeight = 28;
    private const int RowHeight           = 32;   // switch + dropdown both fit comfortably
    private const int RowGap              = 4;    // gap between rows within a section
    private const int SectionGap          = 14;   // gap between sections

    // Never shrink the visible window below this (logical px); below it the
    // dialog is unusable even on tiny screens.
    private const int MinVisibleHeight = 200;
    // Logical-pixel headroom reserved for the title bar, window padding, and a
    // little breathing space when capping the visible height to the screen.
    private const int VisibleHeightMargin = 120;

    private const string ScrollContentKey = "hudclock-settings-scroll";
    private const string ScrollbarKey     = "hudclock-settings-scrollbar";

    private readonly HudClockSettings _settings;
    private readonly ModLog _log;

    // Switch elements plus their initial states. Container children aren't in
    // the composer's flat lookup, so we hold references and apply the initial
    // On state after composition (OnGuiOpened), mirroring the pre-scroll flow.
    private readonly Dictionary<string, GuiElementSwitch> _switches = new();
    private readonly Dictionary<string, bool> _switchInitial = new();

    // Movable scroll container bounds + its resting Y, used by OnScroll.
    private ElementBounds? _scrollContentBounds;
    private double _scrollContentRestY;

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
        _switches.Clear();
        _switchInitial.Clear();

        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        // Cap the visible window to the screen (logical px). GUIScale converts
        // physical framebuffer pixels to the logical units ElementBounds uses.
        float guiScale = RuntimeEnv.GUIScale;
        if (guiScale <= 0f) guiScale = 1f;
        int logicalScreenHeight = (int)(capi.Render.FrameHeight / guiScale);
        int maxVisibleHeight = Math.Max(MinVisibleHeight, logicalScreenHeight - VisibleHeightMargin);

        // ForkContainingChild trims HalfPadding off *all four* sides of the inset,
        // so the container's usable content box is (inset − 2·HalfPadding) wide.
        // Rows are laid out to ContentWidth and a dropdown's right edge sits at
        // exactly ContentWidth, so the inset must absorb that padding (plus a small
        // gutter) or the dropdown loses its right border to the clip. GuiStyle
        // paddings are runtime doubles, so compute the width here rather than as a
        // const.
        int pad = (int)GuiStyle.HalfPadding;
        int insetWidth = ContentWidth + 2 * pad + ContentGutter;

        // Inset = the visible scroll window, below the title bar. Heights are
        // finalized after the content height is known (before Compose()).
        ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, maxVisibleHeight);
        ElementBounds scrollbarBounds = insetBounds.RightCopy().WithFixedWidth(20);
        ElementBounds clipBounds = insetBounds.ForkContainingChild(pad, pad, pad, pad);
        ElementBounds containerBounds = insetBounds.ForkContainingChild(pad, pad, pad, pad);

        _scrollContentBounds = containerBounds;
        _scrollContentRestY = containerBounds.fixedY;

        ElementBounds bgBounds = ElementBounds.Fill
            .WithFixedPadding(GuiStyle.ElementToDialogPadding)
            .WithSizing(ElementSizing.FitToChildren)
            .WithChildren(insetBounds, scrollbarBounds);

        GuiComposer composer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get(LangKeys.Settings.Title), OnClose)
            .BeginChildElements()
                .AddInset(insetBounds, 3)
                .BeginClip(clipBounds)
                    .AddContainer(containerBounds, ScrollContentKey)
                .EndClip()
                .AddVerticalScrollbar(OnScroll, scrollbarBounds, ScrollbarKey)
            .EndChildElements();

        // GetContainer works once AddContainer has run; add all rows to it
        // before Compose().
        SingleComposer = composer;
        GuiElementContainer container = composer.GetContainer(ScrollContentKey);

        int y = 0;

        y = AddSection(container, y, LangKeys.Settings.SectionDisplay, (c, yy) =>
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

        y = AddSection(container, y, LangKeys.Settings.SectionAppearance, (c, yy) =>
        {
            yy = AddDropDown(c, yy, LangKeys.Settings.IconTheme,
                Enum.GetNames(typeof(IconTheme)),
                IconThemeLocales(),
                (int)_settings.Appearance.IconTheme,
                "icon-theme",
                (code, sel) => _settings.Appearance.IconTheme = ParseEnum<IconTheme>(code));
            yy = AddDropDown(c, yy, LangKeys.Settings.HudTextColor,
                Enum.GetNames(typeof(HudTextColor)),
                HudTextColorLocales(),
                (int)_settings.Appearance.HudTextColor,
                "hud-text-color",
                (code, sel) => _settings.Appearance.HudTextColor = ParseEnum<HudTextColor>(code));
            yy = AddSwitch(c, yy, LangKeys.Settings.HemisphereAwareSeason, "hemisphere-season",
                _settings.Appearance.HemisphereAwareSeason,
                v => _settings.Appearance.HemisphereAwareSeason = v);
            return yy;
        });

        y = AddSection(container, y, LangKeys.Settings.SectionTime, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowDate, "date",
                _settings.Time.ShowDate, v => _settings.Time.ShowDate = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowTime, "time",
                _settings.Time.ShowTime, v => _settings.Time.ShowTime = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowRealtime, "realtime",
                _settings.Time.ShowRealtime, v => _settings.Time.ShowRealtime = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowMoonPhase, "moon-phase",
                _settings.Time.ShowMoonPhase, v => _settings.Time.ShowMoonPhase = v);
            yy = AddDropDown(c, yy, LangKeys.Settings.TimeFormat,
                Enum.GetNames(typeof(TimeFormat)),
                TimeFormatLocales(),
                (int)_settings.Time.Format,
                "timeformat",
                (code, sel) => _settings.Time.Format = ParseEnum<TimeFormat>(code));
            return yy;
        });

        y = AddSection(container, y, LangKeys.Settings.SectionWeather, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowSeason, "season",
                _settings.Weather.ShowSeason, v => _settings.Weather.ShowSeason = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowTemperature, "temp",
                _settings.Weather.ShowTemperature, v => _settings.Weather.ShowTemperature = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.Fahrenheit, "fahrenheit",
                _settings.Weather.Fahrenheit, v => _settings.Weather.Fahrenheit = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowRainfall, "rainfall",
                _settings.Weather.ShowRainfall, v => _settings.Weather.ShowRainfall = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowAreaRainfall, "area-rainfall",
                _settings.Weather.ShowAreaRainfall, v => _settings.Weather.ShowAreaRainfall = v);
            yy = AddDropDown(c, yy, LangKeys.Settings.Wind,
                Enum.GetNames(typeof(WindDisplay)),
                WindLocales(),
                (int)_settings.Weather.Wind,
                "wind",
                (code, sel) => _settings.Weather.Wind = ParseEnum<WindDisplay>(code));
            return yy;
        });

        y = AddSection(container, y, LangKeys.Settings.SectionPlayerStats, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowBodyTemperature, "body-temp",
                _settings.PlayerStats.ShowBodyTemperature,
                v => _settings.PlayerStats.ShowBodyTemperature = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowIntoxication, "intox",
                _settings.PlayerStats.ShowIntoxication,
                v => _settings.PlayerStats.ShowIntoxication = v);
            // Apparent ("feels like") temperature line. Silently no-ops
            // unless an immersive-temperature mod is publishing the
            // bodyTemp.apparentTempC watched attribute (see
            // docs/integration.md). The setting itself is always shown so
            // users with the mod installed can find it.
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowApparentTemperature, "apparent-temp",
                _settings.PlayerStats.ShowApparentTemperature,
                v => _settings.PlayerStats.ShowApparentTemperature = v);
            return yy;
        });

        y = AddSection(container, y, LangKeys.Settings.SectionEvents, (c, yy) =>
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

        int contentHeight = AddSection(container, y, LangKeys.Settings.SectionMultiplayer, (c, yy) =>
        {
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowOnlinePlayers, "players",
                _settings.Multiplayer.ShowOnlinePlayerCount,
                v => _settings.Multiplayer.ShowOnlinePlayerCount = v);
            yy = AddSwitch(c, yy, LangKeys.Settings.ShowClaim, "claim",
                _settings.Claim.ShowClaimedArea,
                v => _settings.Claim.ShowClaimedArea = v);
            return yy;
        });

        // Size the inset (the bordered box) to the content plus the fork's
        // HalfPadding top+bottom, capped to the screen. The clip *viewport* is then
        // the inset minus that padding — because ForkContainingChild both trims the
        // clip height and offsets it down by pad, giving the clip the full inset
        // height (as the pre-4.4.1 code did) pushed the clipped content one
        // HalfPadding below the inset's bottom border.
        int insetHeight = Math.Min(contentHeight + 2 * pad, maxVisibleHeight);
        int viewportHeight = insetHeight - 2 * pad;

        insetBounds.fixedHeight = insetHeight;
        clipBounds.fixedHeight = viewportHeight;
        scrollbarBounds.fixedHeight = insetHeight;
        // containerBounds height is (re)computed from its children during Compose
        // by GuiElementContainer.CalcTotalHeight, so setting it here is a no-op.

        composer.Compose();

        // Canonical VS scrolling contract (see the wiki's DemoScrollingGui):
        // visible = the clip viewport, total = the container's *actual* composed
        // height. Reading the real total back (rather than our running
        // contentHeight, which omits the container's own cell spacing) lets the
        // scrollbar reach the very bottom row. A total <= visible disables the
        // handle (nothing to scroll).
        GuiElementContainer scrollContent = composer.GetContainer(ScrollContentKey);
        float totalHeight = (float)scrollContent.Bounds.fixedHeight;
        composer.GetScrollbar(ScrollbarKey)?.SetHeights(viewportHeight, totalHeight);
    }

    /// <summary>
    /// Scrollbar callback: shift the scroll container up by the scrollbar value
    /// (relative to its resting Y) and recompute its world bounds so the
    /// clipped content repaints at the new offset.
    /// </summary>
    private void OnScroll(float value)
    {
        if (_scrollContentBounds is null) return;
        _scrollContentBounds.fixedY = _scrollContentRestY - value;
        _scrollContentBounds.CalcWorldBounds();
    }

    // --- Layout helpers (build rows into the scroll container) ---

    private int AddSection(GuiElementContainer container, int startY, string titleKey,
        Vintagestory.API.Common.Func<GuiElementContainer, int, int> build)
    {
        ElementBounds titleBounds = ElementBounds.Fixed(LabelX, startY, ContentWidth, SectionHeaderHeight);
        container.Add(new GuiElementStaticText(capi, Lang.Get(titleKey),
            EnumTextOrientation.Left, titleBounds,
            CairoFont.WhiteSmallishText().WithWeight(Cairo.FontWeight.Bold)));

        int y = startY + SectionHeaderHeight + RowGap;
        y = build(container, y);
        return y + SectionGap;
    }

    private int AddSwitch(GuiElementContainer container, int y, string labelKey, string nameSuffix,
        bool initial, Action<bool> onChange)
    {
        ElementBounds labelBounds = ElementBounds.Fixed(LabelX, y, LabelWidth, RowHeight);
        ElementBounds controlBounds = ElementBounds.Fixed(ControlX, y + 4, RowHeight - 8, RowHeight - 8);

        container.Add(new GuiElementStaticText(capi, Lang.Get(labelKey),
            EnumTextOrientation.Left, labelBounds, CairoFont.WhiteSmallText()));

        var sw = new GuiElementSwitch(capi, onChange, controlBounds);
        container.Add(sw);
        _switches[nameSuffix] = sw;
        _switchInitial[nameSuffix] = initial;

        return y + RowHeight + RowGap;
    }

    private int AddDropDown(GuiElementContainer container, int y, string labelKey,
        string[] codes, string[] labels, int initialIndex, string nameSuffix,
        SelectionChangedDelegate onChange)
    {
        ElementBounds labelBounds = ElementBounds.Fixed(LabelX, y, LabelWidth, RowHeight);
        ElementBounds controlBounds = ElementBounds.Fixed(ControlX, y + 2, ControlWidth, RowHeight - 4);

        container.Add(new GuiElementStaticText(capi, Lang.Get(labelKey),
            EnumTextOrientation.Left, labelBounds, CairoFont.WhiteSmallText()));

        var dd = new GuiElementDropDown(capi, codes, labels, initialIndex, onChange,
            controlBounds, CairoFont.WhiteSmallText(), false);
        container.Add(dd);

        return y + RowHeight + RowGap;
    }

    /// <inheritdoc />
    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        if (SingleComposer is null) return;

        // Apply initial switch states. The switches live inside the scroll
        // container (not the composer's flat lookup), so we set them via the
        // references captured during Build rather than GetSwitch(key).
        foreach (var kvp in _switches)
        {
            if (_switchInitial.TryGetValue(kvp.Key, out bool on))
            {
                kvp.Value.On = on;
            }
        }
    }

    private void OnClose() => TryClose();

    // --- Enum helpers ---

    private static T ParseEnum<T>(string code) where T : struct, Enum
        => (T)Enum.Parse(typeof(T), code);

    private static string[] HudAnchorLocales() => LocalizeEnumNames<HudAnchor>(LangKeys.Settings.HudPositionStem);
    private static string[] IconThemeLocales() => LocalizeEnumNames<IconTheme>(LangKeys.Settings.IconThemeStem);
    private static string[] HudTextColorLocales() => LocalizeEnumNames<HudTextColor>(LangKeys.Settings.HudTextColorStem);
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
            HudTextColor h   => h.ToKeySuffix(),
            TimeFormat t     => t.ToKeySuffix(),
            WindDisplay w    => w.ToKeySuffix(),
            StormDisplay s   => s.ToKeySuffix(),
            RiftDisplay r    => r.ToKeySuffix(),
            _ => value.ToString().ToLowerInvariant(),
        };
    }
}
