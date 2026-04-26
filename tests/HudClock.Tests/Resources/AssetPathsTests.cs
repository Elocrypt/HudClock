using HudClock.Configuration;
using HudClock.Resources;
using Xunit;

namespace HudClock.Tests.Resources;

/// <summary>
/// Verifies that <see cref="AssetPaths"/> resolves to the documented
/// per-theme texture paths. The Custom theme uses a different filename
/// convention from Modern/Classic — it drops the historical "-large"
/// suffix so user-supplied texture-pack PNGs can use simpler names.
/// </summary>
public class AssetPathsTests
{
    [Fact]
    public void Modern_season_paths_use_modern_folder_and_large_suffix()
    {
        Assert.Equal("hudclock:textures/hud/modern/spring-large.png", AssetPaths.Hud.SeasonSpring(IconTheme.Modern));
        Assert.Equal("hudclock:textures/hud/modern/winter-large.png", AssetPaths.Hud.SeasonWinter(IconTheme.Modern));
        Assert.Equal("hudclock:textures/hud/modern/tempstorm-large.png", AssetPaths.Hud.Storm(IconTheme.Modern));
    }

    [Fact]
    public void Classic_season_paths_use_classic_folder_and_large_suffix()
    {
        Assert.Equal("hudclock:textures/hud/classic/summer-large.png", AssetPaths.Hud.SeasonSummer(IconTheme.Classic));
        Assert.Equal("hudclock:textures/hud/classic/fall-large.png",   AssetPaths.Hud.SeasonFall(IconTheme.Classic));
    }

    [Fact]
    public void Custom_season_paths_use_custom_folder_without_large_suffix()
    {
        // Texture-pack-friendly: spring.png not spring-large.png. Documented
        // in CHANGELOG and README. Test locks the contract so a future
        // refactor doesn't quietly break user texture packs.
        Assert.Equal("hudclock:textures/hud/custom/spring.png", AssetPaths.Hud.SeasonSpring(IconTheme.Custom));
        Assert.Equal("hudclock:textures/hud/custom/summer.png", AssetPaths.Hud.SeasonSummer(IconTheme.Custom));
        Assert.Equal("hudclock:textures/hud/custom/fall.png",   AssetPaths.Hud.SeasonFall(IconTheme.Custom));
        Assert.Equal("hudclock:textures/hud/custom/winter.png", AssetPaths.Hud.SeasonWinter(IconTheme.Custom));
        Assert.Equal("hudclock:textures/hud/custom/tempstorm.png", AssetPaths.Hud.Storm(IconTheme.Custom));
    }

    [Fact]
    public void Room_paths_use_bare_filenames_across_all_themes()
    {
        // Room icons never carried the -large suffix even on Modern/Classic,
        // so all three themes share the same simple convention here.
        Assert.Equal("hudclock:textures/room/modern/room.png",   AssetPaths.Room.Generic(IconTheme.Modern));
        Assert.Equal("hudclock:textures/room/classic/cellar.png", AssetPaths.Room.Cellar(IconTheme.Classic));
        Assert.Equal("hudclock:textures/room/custom/greenhouse.png", AssetPaths.Room.Greenhouse(IconTheme.Custom));
    }
}
