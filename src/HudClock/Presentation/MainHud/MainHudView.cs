using System;
using System.Collections.Generic;
using Cairo;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Infrastructure.Assets;
using HudClock.Presentation.Shared;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HudClock.Presentation.MainHud;

/// <summary>
/// Main HUD rendering surface. Extends <see cref="HudElement"/> so VS manages
/// its visibility like any other in-game dialog.
/// </summary>
/// <remarks>
/// <para>Two rendering paths, chosen by the caller:</para>
/// <list type="bullet">
///   <item><b>Rebuild()</b> — full layout: discards the existing composer,
///     builds a fresh one with one <c>AddDynamicText</c> element per visible
///     line, plus static-draw slots for icons. Call when the <em>set</em> of
///     visible lines changes (toggling a setting).</item>
///   <item><b>UpdateTexts()</b> — fast path: just calls <c>SetNewText</c> on
///     the existing dynamic text elements. Call on every tick.</item>
/// </list>
/// <para>
/// This is the core performance improvement over 3.x, which called
/// <c>SingleComposer.Clear()</c> and re-added every element on every tick.
/// </para>
/// </remarks>
internal sealed class MainHudView : HudElement
{
    // Layout constants — tuned to match 3.x dimensions.
    private const int OffsetX = 10;
    private const int LineWidth = 300;
    private const int LineHeight = 17;
    private const int Padding = 10;
    private const int LinePadding = 5;
    private const int IconSize = 100;
    private const int RoomIconSize = 32;

    private readonly MainHudViewModel _viewModel;
    private readonly IconCache _iconCache;
    private readonly ModLog _log;

    // Tracks which line keys are currently laid out; used during UpdateTexts
    // to skip lines that were not part of the last Rebuild.
    private readonly HashSet<MainHudLineKey> _currentLineKeys = new();
    private HudAnchor _currentAnchor = HudAnchor.TopLeft;

    public MainHudView(ICoreClientAPI capi, MainHudViewModel viewModel, IconCache iconCache, ModLog log)
        : base(capi)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _iconCache = iconCache ?? throw new ArgumentNullException(nameof(iconCache));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public override string ToggleKeyCombinationCode => "hudclock:mainhud";

    /// <summary>
    /// Rebuild the entire layout from scratch. Call after a settings change that
    /// alters which lines are visible or where the HUD is anchored.
    /// </summary>
    public void Rebuild(HudAnchor anchor)
    {
        _currentAnchor = anchor;
        _currentLineKeys.Clear();

        if (_viewModel.IsEmpty)
        {
            // No content to show — dispose any existing composer so nothing renders.
            SingleComposer?.Dispose();
            SingleComposer = null;
            return;
        }

        var layout = new BoundsBuilder(OffsetX, LineHeight, LinePadding);

        // Reserve bounds for every visible line, in order.
        var lineBoundsByKey = new Dictionary<MainHudLineKey, ElementBounds>();
        foreach (MainHudLineKey key in _viewModel.VisibleLineKeys())
        {
            lineBoundsByKey[key] = layout.AddLine(LineWidth);
            _currentLineKeys.Add(key);
        }

        // The right-aligned season-icon slot spans the whole stack.
        ElementBounds iconSlot = layout.AddRightSlot(LineWidth, IconSize);

        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(anchor.ToDialogArea())
            .WithFixedPadding(Padding);

        ElementBounds bgBounds = ElementBounds.Fill;
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        foreach (ElementBounds b in layout.Lines) bgBounds.WithChild(b);

        GuiComposer composer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddShadedDialogBG(bgBounds, withTitleBar: false, 0.0)
            .AddStaticCustomDraw(iconSlot, DrawSeasonIcon);

        // One AddDynamicText per visible line, named by the line key so we can
        // look them up by name in UpdateTexts without maintaining a parallel list.
        foreach (KeyValuePair<MainHudLineKey, ElementBounds> kvp in lineBoundsByKey)
        {
            composer.AddDynamicText(
                string.Empty,
                CairoFont.WhiteDetailText(),
                kvp.Value,
                LineKeyToElementName(kvp.Key));
        }

        // Room indicator, bottom-right of the panel. Always laid out when the
        // indicator is enabled in settings; the draw callback checks per-frame
        // whether there's a current room icon to render.
        if (_viewModel.IsRoomIndicatorVisible)
        {
            ElementBounds roomBounds = ElementBounds.FixedOffseted(
                EnumDialogArea.RightBottom, 0, 0, RoomIconSize, RoomIconSize);
            composer.AddStaticCustomDraw(roomBounds, DrawRoomIcon);
        }

        SingleComposer = composer.Compose();
        UpdateTexts();
    }

    /// <summary>Fast-path update: refresh dynamic text content without recomposing.</summary>
    public void UpdateTexts()
    {
        if (SingleComposer is null) return;

        foreach (MainHudLineKey key in _currentLineKeys)
        {
            string? text = _viewModel.GetLineText(key);
            if (text is null) continue;  // line went blank between rebuilds; leave last value
            SingleComposer.GetDynamicText(LineKeyToElementName(key))?.SetNewText(text);
        }
    }

    /// <summary>Match <see cref="HudAnchor"/> to the view's current anchor.</summary>
    public bool IsCurrentAnchor(HudAnchor anchor) => _currentAnchor == anchor;

    // --- Draw callbacks ---

    private void DrawSeasonIcon(Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        string? path = _viewModel.SeasonIconPath;
        if (path is null) return;
        BitmapRef? bitmap = _iconCache.Get(path);
        if (bitmap is null) return;
        surface.Image(bitmap, (int)bounds.drawX, (int)bounds.drawY, (int)bounds.InnerWidth, (int)bounds.InnerHeight);
    }

    private void DrawRoomIcon(Context ctx, ImageSurface surface, ElementBounds bounds)
    {
        string? path = _viewModel.RoomIconPath;
        if (path is null) return;
        BitmapRef? bitmap = _iconCache.Get(path);
        if (bitmap is null) return;
        surface.Image(bitmap, (int)bounds.drawX, (int)bounds.drawY, (int)bounds.InnerWidth, (int)bounds.InnerHeight);
    }

    private static string LineKeyToElementName(MainHudLineKey key) => "hudclock:line-" + key;
}
