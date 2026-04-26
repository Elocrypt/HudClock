using System;
using System.Collections.Generic;
using HudClock.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HudClock.Infrastructure.Assets;

/// <summary>
/// Shared owner of every <see cref="BitmapRef"/> used by the HUD. Views request
/// bitmaps by asset path; the cache loads on first request and keeps the
/// handle alive for the mod's lifetime, disposing them all together when the
/// mod is disposed.
/// </summary>
/// <remarks>
/// <para>
/// Handles returned from <see cref="Get"/> are <b>non-owning</b> references.
/// Callers must never dispose them; the cache retains ownership.
/// </para>
/// <para>
/// This replaces the scattered bitmap lifetime management in 3.x, where four
/// separate classes (<c>TimeDialog</c>, <c>StormDialog</c>,
/// <c>RoomIndicatorClient</c>) each loaded and disposed their own bitmaps
/// across independent <c>OnLeaveWorld</c> paths.
/// </para>
/// </remarks>
internal sealed class IconCache : IDisposable
{
    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;
    private readonly Dictionary<string, BitmapRef> _bitmaps = new(StringComparer.Ordinal);
    private bool _disposed;

    public IconCache(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Return a shared, non-owning reference to the bitmap at
    /// <paramref name="assetPath"/>, loading it on first request. Returns
    /// null if the asset cannot be loaded; a warning is logged for the first
    /// failure per path.
    /// </summary>
    /// <param name="assetPath">An asset path such as <c>hudclock:textures/hud/modern/spring-large.png</c>.</param>
    public BitmapRef? Get(string assetPath)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IconCache));
        if (string.IsNullOrWhiteSpace(assetPath)) return null;

        if (_bitmaps.TryGetValue(assetPath, out BitmapRef? existing))
        {
            return existing;
        }

        try
        {
            // TryGet returns null for missing files instead of throwing.
            // Custom-theme users may not have shipped overrides for every
            // slot, so a missing-file path needs to short-circuit to null
            // and let the caller's guard skip the draw rather than throw.
            IAsset? asset = _api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset is null)
            {
                _log.Warning("Icon asset not found: '{0}'", assetPath);
                return null;
            }

            BitmapRef? bitmap = asset.ToBitmap(_api);
            if (bitmap is null)
            {
                _log.Warning("Icon '{0}' failed to decode", assetPath);
                return null;
            }

            _bitmaps[assetPath] = bitmap;
            return bitmap;
        }
        catch (Exception ex)
        {
            _log.Warning("Failed to load icon '{0}': {1}", assetPath, ex.Message);
            return null;
        }
    }

    /// <summary>Dispose every retained bitmap. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (KeyValuePair<string, BitmapRef> kvp in _bitmaps)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _log.Warning("Error disposing icon '{0}': {1}", kvp.Key, ex.Message);
            }
        }
        _bitmaps.Clear();
    }
}
