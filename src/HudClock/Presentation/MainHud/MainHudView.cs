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
    private const int IconSize = 200;
    private const int RoomIconSize = 32;

    private readonly MainHudViewModel _viewModel;
    private readonly IconCache _iconCache;
    private readonly ModLog _log;

    // Tracks which line keys are currently laid out; used during UpdateTexts
    // to skip lines that were not part of the last Rebuild.
    private readonly HashSet<MainHudLineKey> _currentLineKeys = new();
    private HudAnchor _currentAnchor = HudAnchor.TopLeft;
    // Logical-pixel content height from the last successful Rebuild. Used by
    // sibling HUDs (Storm) to stack themselves below us without having to
    // unscale VS's post-compose abs bounds. Zero when the dialog is closed
    // or was torn down in the empty-state branch.
    private int _lastContentHeight;

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
            // No content to show. Don't null SingleComposer — VS 1.22's setter NREs
            // on null. Closing the dialog hides it; a subsequent Rebuild will
            // replace the composer with fresh content.
            _lastContentHeight = 0;
            TryClose();
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

        // Snapshot logical content height for sibling HUDs' stacking math.
        _lastContentHeight = layout.TotalHeight;

        int solo = HudAnchorOffsets.GetSoloOffsetY(anchor);
        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(anchor.ToDialogArea())
            .WithFixedAlignmentOffset(0.0, solo)
            .WithFixedPadding(Padding);

        ElementBounds bgBounds = ElementBounds.Fill;
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        foreach (ElementBounds b in layout.Lines) bgBounds.WithChild(b);

        GuiComposer composer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddShadedDialogBG(bgBounds, withTitleBar: false, 0.0)
            // Static custom draws rasterize once into the composer's cached
            // background texture. That's why the controller must trigger a
            // full Rebuild when the season or room status changes: the
            // callback won't re-run on its own. Dynamic custom draws would
            // re-run but render in screen coordinates rather than the
            // composer's local space, which misplaces the icon entirely.
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

        // Room indicator, bottom-right of the panel. Laid out when the
        // indicator is enabled in settings. Like the season icon, the
        // bitmap is baked into the composer's cached texture at Compose
        // time — a change in room status triggers a full Rebuild via the
        // viewmodel's change-detection, see MainHudViewModel.Tick.
        if (_viewModel.IsRoomIndicatorVisible)
        {
            ElementBounds roomBounds = ElementBounds.FixedOffseted(
                EnumDialogArea.RightBottom, 0, 0, RoomIconSize, RoomIconSize);
            composer.AddStaticCustomDraw(roomBounds, DrawRoomIcon);
        }

        SingleComposer = composer.Compose();

        // Re-open if we were previously closed (e.g. after an empty-state transition).
        // TryOpen is idempotent so calling it when already open is harmless.
        if (!IsOpened()) TryOpen();

        // Record which icon values are now baked into the static custom-draw
        // texture, so the controller knows not to re-Rebuild on the next tick
        // unless the icon state actually changes again.
        _viewModel.MarkIconsBaked();

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

    /// <summary>
    /// Current anchor of the composed HUD. Storm HUD reads this when it
    /// shares an anchor with Main to compute the stacking offset.
    /// </summary>
    public HudAnchor CurrentAnchor => _currentAnchor;

    /// <summary>
    /// Outer height of the current composed HUD in <b>logical</b> pixels
    /// (pre-GUI-scale). Returns 0 when the HUD isn't currently showing
    /// (empty state, not yet built). Storm reads this to offset itself
    /// below Main when they share an anchor.
    /// </summary>
    /// <remarks>
    /// Computed from <see cref="BoundsBuilder.TotalHeight"/> plus the dialog
    /// padding rather than reading <c>SingleComposer.Bounds.OuterHeight</c>,
    /// which is populated post-<c>CalcWorldBounds</c> in GUI-scaled screen
    /// pixels. Feeding that scaled value back into
    /// <c>WithFixedAlignmentOffset</c> (which expects logical pixels) would
    /// double-scale the gap.
    /// </remarks>
    public double CurrentLogicalOuterHeight =>
        (IsOpened() && _lastContentHeight > 0) ? _lastContentHeight + 2 * Padding : 0.0;

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
