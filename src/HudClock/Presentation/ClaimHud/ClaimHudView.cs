using System;
using HudClock.Core;
using HudClock.Presentation.Shared;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock.Presentation.ClaimHud;

/// <summary>
/// A small centered-top banner showing the owner of the claim the player is
/// currently standing in. Auto-appears when the player enters a claim and
/// closes when they leave.
/// </summary>
internal sealed class ClaimHudView : HudElement
{
    private const int LineHeight = 24;
    private const int LineWidth = 300;
    private const double InnerPadding = 5.0;
    private const string TextElementName = "hudclock:claim-owner";

    private readonly ModLog _log;
    private string _currentOwner = string.Empty;

    public ClaimHudView(ICoreClientAPI capi, ModLog log) : base(capi)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public override string ToggleKeyCombinationCode => "hudclock:claimhud";

    /// <summary>
    /// Update the displayed owner name. If the dialog hasn't been composed yet,
    /// this builds its layout on first call.
    /// </summary>
    public void SetOwner(string ownerName)
    {
        if (string.IsNullOrEmpty(ownerName))
        {
            // Empty name = nothing to display. Hide if we were showing.
            if (IsOpened()) TryClose();
            return;
        }

        _currentOwner = ownerName;

        if (SingleComposer is null)
        {
            Build();
        }

        string text = Lang.Get(LangKeys.Claim.Owner, _currentOwner);
        SingleComposer?.GetDynamicText(TextElementName)?.SetNewText(text);

        if (!IsOpened()) TryOpen();
    }

    /// <summary>Hide the banner without destroying its layout.</summary>
    public void Hide()
    {
        if (IsOpened()) TryClose();
    }

    private void Build()
    {
        ElementBounds dialogBounds = ElementStdBounds
            .AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterTop)
            .WithFixedAlignmentOffset(0.0, HudAnchorOffsets.ClaimBannerOffsetY);

        ElementBounds bgBounds = ElementBounds.Fill;
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithFixedPadding(InnerPadding, InnerPadding);

        ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.None, 0.0, 0.0, LineWidth, LineHeight);
        bgBounds.WithChild(textBounds);

        CairoFont font = CairoFont.WhiteSmallishText();
        font.Orientation = EnumTextOrientation.Center;

        SingleComposer = capi.Gui
            .CreateCompo(ToggleKeyCombinationCode, dialogBounds)
            .AddGameOverlay(bgBounds)
            .BeginChildElements(bgBounds)
            .AddDynamicText(string.Empty, font, textBounds, TextElementName)
            .EndChildElements()
            .Compose();
    }
}
