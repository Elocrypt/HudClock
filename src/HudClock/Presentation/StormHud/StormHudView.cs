using System;
using Cairo;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Storms;
using HudClock.Infrastructure.Assets;
using HudClock.Presentation.Shared;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock.Presentation.StormHud;

/// <summary>
/// Small panel showing temporal-storm status. Accepts a fully-decided
/// <see cref="StormHudAction"/> from <see cref="StormHudStateMachine"/> and
/// renders accordingly; contains no policy of its own.
/// </summary>
internal sealed class StormHudView : HudElement
{
    private const int LineHeight = 17;
    private const int LineWidth = 180;
    private const int IconWidth = 100;
    private const int LinePadding = 10;
    private const int Padding = 10;
    private const string TextElementName = "hudclock:storm-text";

    private readonly IconCache _iconCache;
    private readonly HudClockSettings _settings;
    private readonly ModLog _log;
    private HudAnchor _currentAnchor;
    private double _currentOffsetY;

    public StormHudView(ICoreClientAPI capi, IconCache iconCache, HudClockSettings settings, ModLog log, HudAnchor anchor) : base(capi)
    {
        _iconCache = iconCache ?? throw new ArgumentNullException(nameof(iconCache));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _currentAnchor = anchor;
        Build();
    }

    /// <inheritdoc />
    public override string ToggleKeyCombinationCode => "hudclock:stormhud";

    /// <summary>
    /// Rebuild the dialog with a new anchor and vertical offset. Call from
    /// the controller when the anchor changes or when Main's layout changes
    /// (so we can re-stack below Main in shared anchors).
    /// </summary>
    /// <param name="anchor">Screen corner to anchor to.</param>
    /// <param name="offsetY">
    /// Offset in logical pixels from the anchor edge. Sign convention:
    /// positive moves away from a top anchor (downward on screen); negative
    /// moves away from a bottom anchor (upward on screen). The controller
    /// picks the right sign based on anchor.
    /// </param>
    public void Rebuild(HudAnchor anchor, double offsetY)
    {
        _currentAnchor = anchor;
        _currentOffsetY = offsetY;
        // Dispose the old composer but don't null-out the property — VS 1.22's
        // GuiDialog.SingleComposer setter NREs on null. Build() below assigns
        // a fresh composer, which is all we need.
        SingleComposer?.Dispose();
        Build();
    }

    /// <summary>Current anchor, used by the controller for stacking offsets.</summary>
    public HudAnchor CurrentAnchor => _currentAnchor;

    /// <summary>Current offset applied to the anchor, read by the controller if needed.</summary>
    public double CurrentOffsetY => _currentOffsetY;

    /// <summary>
    /// Apply an action from the state machine. Updates the dialog's visibility
    /// and text in one call.
    /// </summary>
    public void Apply(StormHudAction action, StormStatus status)
    {
        if (action == StormHudAction.Hidden)
        {
            if (IsOpened()) TryClose();
            return;
        }

        string text = action switch
        {
            StormHudAction.ShowDeactivated => Lang.Get(LangKeys.Storm.Deactivated),
            StormHudAction.ShowFarAway     => Lang.Get(LangKeys.Storm.FarAway),
            StormHudAction.ShowActive      => Lang.Get(LangKeys.Storm.Active),
            StormHudAction.ShowApproaching => FormatApproachingText(status.DaysUntilNext),
            _ => string.Empty,
        };

        SingleComposer?.GetDynamicText(TextElementName)?.SetNewText(text);
        if (!IsOpened()) TryOpen();
    }

    private string FormatApproachingText(double daysUntilNext)
    {
        // Render as hh:mm of in-game time until the storm.
        double totalHours = TimeSpan.FromDays(daysUntilNext).TotalHours
                            % capi.World.Calendar.HoursPerDay;
        int hour = (int)totalHours;
        int minute = (int)((totalHours - hour) * 60.0);
        string formatted = $"{hour:00}:{minute:00}";
        return Lang.Get(LangKeys.Storm.Approaching, formatted);
    }

    private void Build()
    {
        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(_currentAnchor.ToDialogArea())
            .WithFixedAlignmentOffset(0.0, _currentOffsetY)
            .WithFixedPadding(Padding);

        ElementBounds textBounds = ElementBounds.Fixed(10, LinePadding, LineWidth, LineHeight);
        ElementBounds iconBounds = ElementBounds.Fixed(145, 0, IconWidth, LinePadding * 2 + LineHeight);

        ElementBounds bgBounds = ElementBounds.Fill;
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChild(textBounds).WithChild(iconBounds);

        SingleComposer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddShadedDialogBG(bgBounds, withTitleBar: false, 0.0)
            .AddStaticCustomDraw(iconBounds, DrawStormIcon)
            .AddDynamicText(string.Empty, CairoFont.WhiteDetailText(), textBounds, TextElementName)
            .Compose();
    }

    private void DrawStormIcon(Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        BitmapRef? bitmap = _iconCache.Get(AssetPaths.Hud.Storm(_settings.Appearance.IconTheme));
        if (bitmap is null) return;
        surface.Image(bitmap, (int)bounds.drawX, (int)bounds.drawY, (int)bounds.InnerWidth, (int)bounds.InnerHeight);
    }
}
