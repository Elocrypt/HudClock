using System;
using System.Collections.Generic;
using System.IO;
using HudClock.Core;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

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
/// Callers must never dispose them; the cache retains ownership. Every entry
/// is its own bitmap instance (a Custom slot that falls back to Modern art
/// loads a separate copy), so <see cref="Dispose"/> never double-frees.
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
    /// <summary>Player-editable art folder, under VS's ModConfig directory.</summary>
    private static readonly string CustomFolder = Path.Combine(GamePaths.ModConfig, "hudclock", "custom");

    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;
    private readonly Dictionary<string, BitmapRef> _bitmaps = new(StringComparer.Ordinal);
    private bool _customFolderEnsured;
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

        BitmapRef? bitmap = AssetPaths.TryResolveCustom(assetPath, out string userRelative, out string modernFallback)
            ? LoadCustom(assetPath, userRelative, modernFallback)
            : LoadAsset(assetPath, warnIfMissing: true);

        if (bitmap is not null) _bitmaps[assetPath] = bitmap;
        return bitmap;
    }

    /// <summary>
    /// Resolve a Custom-theme slot: the player's file in ModConfig, then the
    /// mod/texture-pack asset, then the matching Modern art.
    /// </summary>
    /// <remarks>
    /// Before 4.5.0 the only way to supply custom art was editing PNGs inside
    /// the mod zip (lost on every update) or a texture pack, and an unfilled
    /// slot rendered the shipped transparent placeholder, i.e. no icon at all.
    /// Players read that as a bug. Fully transparent images are therefore
    /// treated as "not supplied".
    /// </remarks>
    private BitmapRef? LoadCustom(string assetPath, string userRelative, string modernFallback)
    {
        EnsureCustomFolder();

        string userFile = Path.Combine(CustomFolder, userRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(userFile))
        {
            try
            {
                BitmapRef? fromFile = _api.Render.BitmapCreateFromPng(File.ReadAllBytes(userFile));
                if (Usable(fromFile))
                {
                    _log.Notification("Custom icon loaded from '{0}'", userFile);
                    return fromFile;
                }
                fromFile?.Dispose();
                _log.Warning("Custom icon '{0}' is empty or fully transparent; ignoring it.", userFile);
            }
            catch (Exception ex)
            {
                _log.Warning("Failed to read custom icon '{0}': {1}", userFile, ex.Message);
            }
        }

        // Texture packs can still override the custom/ asset path directly.
        BitmapRef? fromAsset = LoadAsset(assetPath, warnIfMissing: false);
        if (Usable(fromAsset)) return fromAsset;
        fromAsset?.Dispose();

        return LoadAsset(modernFallback, warnIfMissing: true);
    }

    private BitmapRef? LoadAsset(string assetPath, bool warnIfMissing)
    {
        try
        {
            // TryGet returns null for missing files instead of throwing.
            IAsset? asset = _api.Assets.TryGet(new AssetLocation(assetPath));
            if (asset is null)
            {
                if (warnIfMissing) _log.Warning("Icon asset not found: '{0}'", assetPath);
                return null;
            }

            BitmapRef? bitmap = asset.ToBitmap(_api);
            if (bitmap is null)
            {
                _log.Warning("Icon '{0}' failed to decode", assetPath);
            }
            return bitmap;
        }
        catch (Exception ex)
        {
            _log.Warning("Failed to load icon '{0}': {1}", assetPath, ex.Message);
            return null;
        }
    }

    /// <summary>True when the bitmap exists and has at least one non-transparent pixel.</summary>
    private static bool Usable(BitmapRef? bitmap)
    {
        if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0) return false;

        // One-time scan per slot at load, not per frame. Early-outs on the
        // first visible pixel, so real art returns almost immediately.
        int[] pixels = bitmap.Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (((uint)pixels[i] >> 24) != 0) return true;
        }
        return false;
    }

    /// <summary>
    /// Create <c>ModConfig/hudclock/custom/{hud,room}/</c> with a README the
    /// first time the Custom theme is used, so players can find where to put
    /// art. Never overwrites an existing README.
    /// </summary>
    private void EnsureCustomFolder()
    {
        if (_customFolderEnsured) return;
        _customFolderEnsured = true;

        try
        {
            Directory.CreateDirectory(Path.Combine(CustomFolder, "hud"));
            Directory.CreateDirectory(Path.Combine(CustomFolder, "room"));

            string readme = Path.Combine(CustomFolder, "README.txt");
            if (!File.Exists(readme))
            {
                File.WriteAllText(readme, CustomReadme);
            }
        }
        catch (Exception ex)
        {
            _log.Warning("Could not create custom icon folder '{0}': {1}", CustomFolder, ex.Message);
        }
    }

    private const string CustomReadme =
        "HUD Clock - Custom icon theme\n" +
        "=============================\n" +
        "\n" +
        "Select \"Custom\" as the Icon theme in HUD Clock's settings (Shift+O), then put\n" +
        "PNG files with these exact names in the folders next to this file:\n" +
        "\n" +
        "  hud/spring.png  hud/summer.png  hud/fall.png  hud/winter.png   season art\n" +
        "  hud/tempstorm.png                                              storm dialog art\n" +
        "  room/room.png   room/cellar.png  room/greenhouse.png           room indicator\n" +
        "\n" +
        "Any size works; images are scaled to fit. For reference the built-in Modern art\n" +
        "is 200x100 (seasons), 100x50 (storm) and 64x64 (room icons).\n" +
        "\n" +
        "Any file you leave out shows the Modern icon instead. Files here survive mod\n" +
        "updates. Leave and rejoin the world to pick up changed files.\n";

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
