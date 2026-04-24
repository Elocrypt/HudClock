using HudClock.Configuration;

namespace HudClock.Tests.Configuration;

/// <summary>
/// Locks in the default values for <see cref="HudClockSettings"/> so changes to
/// out-of-box behavior require an explicit, reviewable edit to these tests.
/// Defaults are chosen to match the 3.x user experience.
/// </summary>
public class HudClockSettingsDefaultsTests
{
    private readonly HudClockSettings _defaults = new();

    [Fact]
    public void SchemaVersion_is_one()
    {
        Assert.Equal(1, _defaults.SchemaVersion);
    }

    // --- Display ---

    [Fact]
    public void Display_anchor_is_TopLeft()
    {
        Assert.Equal(HudAnchor.TopLeft, _defaults.Display.Anchor);
    }

    // --- Time ---

    [Fact]
    public void Time_shows_date_by_default()
    {
        Assert.True(_defaults.Time.ShowDate);
    }

    [Fact]
    public void Time_shows_in_game_time_by_default()
    {
        Assert.True(_defaults.Time.ShowTime);
    }

    [Fact]
    public void Time_hides_realtime_by_default()
    {
        Assert.False(_defaults.Time.ShowRealtime);
    }

    [Fact]
    public void Time_format_is_24h_by_default()
    {
        Assert.Equal(TimeFormat.TwentyFourHour, _defaults.Time.Format);
    }

    // --- Weather ---

    [Fact]
    public void Weather_shows_season_by_default()
    {
        Assert.True(_defaults.Weather.ShowSeason);
    }

    [Fact]
    public void Weather_shows_temperature_by_default()
    {
        Assert.True(_defaults.Weather.ShowTemperature);
    }

    [Fact]
    public void Weather_temperature_is_celsius_by_default()
    {
        Assert.False(_defaults.Weather.Fahrenheit);
    }

    [Fact]
    public void Weather_wind_shows_Beaufort_text_by_default()
    {
        // Matches 3.x: the old WindDisplayState field had no initializer,
        // which meant the default was the first enum member (TEXT).
        Assert.Equal(WindDisplay.BeaufortText, _defaults.Weather.Wind);
    }

    // --- Storm ---

    [Fact]
    public void Storm_display_is_TriggerOnly_by_default()
    {
        Assert.Equal(StormDisplay.TriggerOnly, _defaults.Storm.Display);
    }

    // --- Rift ---

    [Fact]
    public void Rift_display_is_WorldConfigDependent_by_default()
    {
        Assert.Equal(RiftDisplay.WorldConfigDependent, _defaults.Rift.Display);
    }

    // --- Claim ---

    [Fact]
    public void Claim_is_hidden_by_default()
    {
        Assert.False(_defaults.Claim.ShowClaimedArea);
    }

    // --- Multiplayer ---

    [Fact]
    public void Multiplayer_shows_online_player_count_by_default()
    {
        Assert.True(_defaults.Multiplayer.ShowOnlinePlayerCount);
    }

    // --- Room ---

    [Fact]
    public void Room_indicator_is_enabled_by_default()
    {
        Assert.True(_defaults.Room.ShowRoomIndicator);
    }

    // --- Appearance ---

    [Fact]
    public void Icon_theme_defaults_to_Modern()
    {
        // Fresh installs should see the refreshed 4.x art. Users upgrading
        // from 4.0.0 with no Appearance section in their settings file
        // deserialize to this default as well.
        Assert.Equal(IconTheme.Modern, _defaults.Appearance.IconTheme);
    }

    // --- Keybinds: defaults must match 3.x so existing muscle memory still works. ---

    [Fact]
    public void OpenSettings_keybind_defaults_to_Shift_A()
    {
        Keybind k = _defaults.Keybinds.OpenSettings;

        Assert.Equal(97, k.KeyCode);
        Assert.True(k.Shift);
        Assert.False(k.Ctrl);
        Assert.False(k.Alt);
    }

    [Fact]
    public void ToggleMainHud_keybind_defaults_to_Ctrl_G()
    {
        Keybind k = _defaults.Keybinds.ToggleMainHud;

        Assert.Equal(103, k.KeyCode);
        Assert.True(k.Ctrl);
        Assert.False(k.Shift);
        Assert.False(k.Alt);
    }

    [Fact]
    public void ToggleStormHud_keybind_defaults_to_Ctrl_LeftBracket()
    {
        Keybind k = _defaults.Keybinds.ToggleStormHud;

        Assert.Equal(91, k.KeyCode);
        Assert.True(k.Ctrl);
        Assert.False(k.Shift);
        Assert.False(k.Alt);
    }

    // --- Keybind model ---

    [Fact]
    public void Keybind_constructor_assigns_all_modifiers()
    {
        var k = new Keybind(keyCode: 42, alt: true, ctrl: true, shift: true);

        Assert.Equal(42, k.KeyCode);
        Assert.True(k.Alt);
        Assert.True(k.Ctrl);
        Assert.True(k.Shift);
    }

    [Fact]
    public void Keybind_parameterless_constructor_yields_no_modifiers()
    {
        var k = new Keybind();

        Assert.Equal(0, k.KeyCode);
        Assert.False(k.Alt);
        Assert.False(k.Ctrl);
        Assert.False(k.Shift);
    }
}
