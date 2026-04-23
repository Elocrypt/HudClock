using System;
using Cairo;
using HudClock.Core;
using HudClock.Domain.Storms;
using HudClock.Infrastructure.Assets;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock.Presentation.StormHud;

/// <summary>
/// Small top-left panel showing temporal-storm status. Accepts a fully-decided
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
    private readonly ModLog _log;

    public StormHudView(ICoreClientAPI capi, IconCache iconCache, ModLog log) : base(capi)
    {
        _iconCache = iconCache ?? throw new ArgumentNullException(nameof(iconCache));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        Build();
    }

    /// <inheritdoc />
    public override string ToggleKeyCombinationCode => "hudclock:stormhud";

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
            .WithAlignment(EnumDialogArea.LeftTop)
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
        BitmapRef? bitmap = _iconCache.Get(AssetPaths.Hud.Storm);
        if (bitmap is null) return;
        surface.Image(bitmap, (int)bounds.drawX, (int)bounds.drawY, (int)bounds.InnerWidth, (int)bounds.InnerHeight);
    }
}
