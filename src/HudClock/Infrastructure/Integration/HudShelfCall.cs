using System;
using HudShelf;
using Vintagestory.API.Client;

namespace HudClock.Infrastructure.Integration;

/// <summary>
/// Inner bridge that references HudShelf types directly. Only ever
/// invoked from inside <c>HudShelfBridge.IsLoaded</c> checks, so
/// the JIT never compiles these methods when HudShelf is absent.
/// </summary>
internal static class HudShelfCall
{
    public static object? Register(
        ICoreClientAPI api,
        string id,
        GuiDialog element,
        string defaultAnchor,
        double defaultOffsetX,
        double defaultOffsetY,
        Action<string, double, double> onPositionChanged,
        Func<(double Width, double Height)> getBounds)
    {
        var shelf = api.ModLoader.GetModSystem<HudShelfModSystem>();
        if (shelf?.Api is null) return null;

        if (!Enum.TryParse<HudAnchor>(defaultAnchor, ignoreCase: false, out var anchor))
        {
            api.Logger.Warning(
                $"[HudClock/HudShelfBridge] Unknown anchor '{defaultAnchor}'. Falling back to TopLeft.");
            anchor = HudAnchor.TopLeft;
        }

        try
        {
            var handle = shelf.Api.Register(new HudRegistration
            {
                Id = id,
                Element = element,
                DefaultAnchor = anchor,
                DefaultOffsetX = defaultOffsetX,
                DefaultOffsetY = defaultOffsetY,
                GetBounds = getBounds,
                OnPositionChanged = p =>
                    onPositionChanged(p.Anchor.ToString(), p.OffsetX, p.OffsetY),
            });

            return handle;
        }
        catch (Exception ex)
        {
            api.Logger.Warning($"[HudClock/HudShelfBridge] Register failed for '{id}': {ex.Message}");
            return null;
        }
    }

    public static (string Anchor, double OffsetX, double OffsetY) GetPosition(object handle)
    {
        var registered = (IRegisteredHud)handle;
        var p = registered.CurrentPosition;
        return (p.Anchor.ToString(), p.OffsetX, p.OffsetY);
    }

    public static void Unregister(object handle)
    {
        var registered = (IRegisteredHud)handle;
        registered.Unregister();
    }
}
