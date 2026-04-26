using System.Collections.Generic;
using HudClock.Configuration;
using HudClock.Presentation.MainHud;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Tests.Presentation.MainHud;

public class MainHudViewModelTests
{
    // Identity translators return the key as the translated string. Makes it
    // trivial to assert on output without maintaining an expected-strings table.
    private static readonly MainHudViewModel.Translator Identity = k => k;
    private static readonly MainHudViewModel.FormatTranslator IdentityFormat =
        (k, args) => string.Format(k + " " + string.Join(",", args), args);

    private static MainHudViewModel MakeVm(
        HudClockSettings? settings = null,
        FakeCalendarService? calendar = null,
        FakeWeatherService? weather = null,
        FakeRiftService? rift = null,
        FakeRoomService? room = null,
        FakeClaimService? claim = null,
        FakePlayerStatsService? playerStats = null,
        bool isMultiplayer = false,
        int onlinePlayers = 0)
    {
        settings ??= new HudClockSettings();
        calendar ??= new FakeCalendarService();
        weather ??= new FakeWeatherService();
        rift ??= new FakeRiftService();
        room ??= new FakeRoomService();
        claim ??= new FakeClaimService();
        playerStats ??= new FakePlayerStatsService();
        var formatter = new StubTimeFormatter();

        var vm = new MainHudViewModel(
            settings, formatter, calendar, weather, rift, room, claim, playerStats,
            translate: Identity,
            translateFormat: IdentityFormat,
            playerPosition: () => new BlockPos(0, 0, 0),
            onlinePlayerCount: () => onlinePlayers,
            isMultiplayer: isMultiplayer);

        vm.Tick();  // prime the cache so property reads have data
        return vm;
    }

    // --- Visibility rules ---

    [Fact]
    public void Empty_when_all_lines_disabled()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = false;
        settings.Weather.ShowTemperature = false;
        settings.Weather.Wind = WindDisplay.Hidden;
        settings.Time.ShowDate = false;
        settings.Time.ShowTime = false;
        settings.Time.ShowRealtime = false;
        settings.Rift.Display = RiftDisplay.Hidden;
        settings.Multiplayer.ShowOnlinePlayerCount = false;
        settings.Room.ShowRoomIndicator = false;

        var vm = MakeVm(settings: settings);

        Assert.True(vm.IsEmpty);
        Assert.Empty(vm.VisibleLineKeys());
    }

    [Fact]
    public void Online_players_line_hidden_in_singleplayer_even_when_enabled()
    {
        var settings = new HudClockSettings();
        settings.Multiplayer.ShowOnlinePlayerCount = true;

        var vm = MakeVm(settings: settings, isMultiplayer: false);

        Assert.False(vm.IsOnlinePlayersLineVisible);
        Assert.Null(vm.OnlinePlayersText);
    }

    [Fact]
    public void Online_players_line_shown_in_multiplayer_when_enabled()
    {
        var settings = new HudClockSettings();
        settings.Multiplayer.ShowOnlinePlayerCount = true;

        var vm = MakeVm(settings: settings, isMultiplayer: true, onlinePlayers: 7);

        Assert.True(vm.IsOnlinePlayersLineVisible);
        Assert.Contains("7", vm.OnlinePlayersText);
    }

    [Fact]
    public void Rift_line_hidden_when_rift_code_is_null_even_if_always_mode()
    {
        var settings = new HudClockSettings();
        settings.Rift.Display = RiftDisplay.Always;

        var rift = new FakeRiftService { IsAvailable = true, ActivityCode = null };

        var vm = MakeVm(settings: settings, rift: rift);

        Assert.False(vm.IsRiftLineVisible);
        Assert.Null(vm.RiftText);
    }

    [Fact]
    public void Rift_line_visible_in_always_mode_when_code_present_but_not_available()
    {
        var settings = new HudClockSettings();
        settings.Rift.Display = RiftDisplay.Always;

        var rift = new FakeRiftService { IsAvailable = false, ActivityCode = "calm" };

        var vm = MakeVm(settings: settings, rift: rift);

        Assert.True(vm.IsRiftLineVisible);
    }

    [Fact]
    public void Rift_line_hidden_in_world_config_dependent_mode_when_unavailable()
    {
        var settings = new HudClockSettings();
        settings.Rift.Display = RiftDisplay.WorldConfigDependent;

        var rift = new FakeRiftService { IsAvailable = false, ActivityCode = "calm" };

        var vm = MakeVm(settings: settings, rift: rift);

        Assert.False(vm.IsRiftLineVisible);
    }

    [Fact]
    public void Room_indicator_hidden_when_no_room_even_if_enabled()
    {
        var settings = new HudClockSettings();
        settings.Room.ShowRoomIndicator = true;
        var room = new FakeRoomService { CurrentStatus = global::HudClock.Domain.Rooms.RoomStatus.None };

        var vm = MakeVm(settings: settings, room: room);

        Assert.False(vm.IsRoomIndicatorVisible);
        Assert.Null(vm.RoomIconPath);
    }

    // --- Visible line ordering ---

    [Fact]
    public void Visible_line_keys_come_out_in_rendering_order()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = true;
        settings.Time.ShowDate = true;
        settings.Time.ShowRealtime = true;
        settings.Weather.Wind = WindDisplay.BeaufortText;
        settings.Rift.Display = RiftDisplay.Always;
        settings.Multiplayer.ShowOnlinePlayerCount = true;

        var rift = new FakeRiftService { IsAvailable = true, ActivityCode = "calm" };

        var vm = MakeVm(settings: settings, rift: rift, isMultiplayer: true, onlinePlayers: 3);

        var expected = new[]
        {
            MainHudLineKey.SeasonAndTemperature,
            MainHudLineKey.DateAndTime,
            MainHudLineKey.Realtime,
            MainHudLineKey.Wind,
            MainHudLineKey.Rift,
            MainHudLineKey.OnlinePlayers,
        };
        Assert.Equal(expected, new List<MainHudLineKey>(vm.VisibleLineKeys()));
    }

    // --- Text formatting ---

    [Fact]
    public void Season_only_returns_translated_season_name()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = true;
        settings.Weather.ShowTemperature = false;
        var calendar = new FakeCalendarService { Season = EnumSeason.Spring };

        var vm = MakeVm(settings: settings, calendar: calendar);

        Assert.Equal("hudclock:season-spring", vm.SeasonAndTemperatureText);
    }

    [Fact]
    public void Temperature_respects_fahrenheit_toggle()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = false;
        settings.Weather.ShowTemperature = true;
        settings.Weather.Fahrenheit = true;
        var weather = new FakeWeatherService { TemperatureCelsius = 0f }; // 0C = 32F

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Contains("32.0", vm.SeasonAndTemperatureText);
    }

    [Fact]
    public void Temperature_celsius_path_uses_celsius_key()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = false;
        settings.Weather.ShowTemperature = true;
        settings.Weather.Fahrenheit = false;
        var weather = new FakeWeatherService { TemperatureCelsius = 20.5f };

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Contains("20.5", vm.SeasonAndTemperatureText);
        Assert.Contains("celsius", vm.SeasonAndTemperatureText);
    }

    [Fact]
    public void Season_and_temperature_combined_uses_comma_separator()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowSeason = true;
        settings.Weather.ShowTemperature = true;
        var calendar = new FakeCalendarService { Season = EnumSeason.Summer };
        var weather = new FakeWeatherService { TemperatureCelsius = 25f };

        var vm = MakeVm(settings: settings, calendar: calendar, weather: weather);

        Assert.Contains(",", vm.SeasonAndTemperatureText);
    }

    // --- Body temperature (comfort signal) ---

    [Fact]
    public void Body_temperature_hidden_when_setting_off()
    {
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = false;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = 33.0f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Null(vm.BodyTemperatureText);
        Assert.False(vm.IsBodyTemperatureLineVisible);
    }

    [Fact]
    public void Body_temperature_hidden_when_attribute_missing()
    {
        // Setting on but the underlying WatchedAttribute is missing — service
        // returns null. Line stays hidden so we don't show fake or stale data.
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = null };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Null(vm.BodyTemperatureText);
        Assert.False(vm.IsBodyTemperatureLineVisible);
    }

    [Theory]
    [InlineData(45.0f)]  // raw max
    [InlineData(41.0f)]  // spawn default (NormalBodyTemperature + 4)
    [InlineData(37.0f)]  // exactly normal — boundary, hide
    public void Body_temperature_hidden_when_at_or_above_normal(float raw)
    {
        // Comfortable / warm states should hide the line. The player
        // doesn't need a number when they're fine.
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = raw };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Null(vm.BodyTemperatureText);
        Assert.False(vm.IsBodyTemperatureLineVisible);
        Assert.False(vm.IsFreezing);
    }

    [Fact]
    public void Body_temperature_below_normal_shows_cool_state_with_signed_delta()
    {
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = 34.6f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.NotNull(vm.BodyTemperatureText);
        Assert.Contains("state-cool", vm.BodyTemperatureText);
        Assert.Contains("-2.4", vm.BodyTemperatureText);  // 34.6 - 37 = -2.4
        Assert.False(vm.IsFreezing);
    }

    [Fact]
    public void Body_temperature_at_freezing_threshold_marks_freezing()
    {
        // 33.0 is the damage threshold from EntityBehaviorBodyTemperature
        // (NormalBodyTemperature - CurBodyTemperature > 4 → damage).
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = 33.0f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.NotNull(vm.BodyTemperatureText);
        Assert.Contains("state-freezing", vm.BodyTemperatureText);
        Assert.Contains("-4.0", vm.BodyTemperatureText);
        Assert.True(vm.IsFreezing);
    }

    [Fact]
    public void Body_temperature_well_below_freezing_still_marks_freezing()
    {
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = 31.0f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Contains("state-freezing", vm.BodyTemperatureText);
        Assert.Contains("-6.0", vm.BodyTemperatureText);
        Assert.True(vm.IsFreezing);
    }

    [Fact]
    public void Body_temperature_delta_scales_for_fahrenheit()
    {
        // Delta of -2.4 °C = -4.32 °F. The signed-delta format rounds
        // to one decimal, so we expect "-4.3".
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowBodyTemperature = true;
        settings.Weather.Fahrenheit = true;
        var stats = new FakePlayerStatsService { BodyTemperatureCelsius = 34.6f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Contains("-4.3", vm.BodyTemperatureText);
        Assert.Contains("°F", vm.BodyTemperatureText);
    }

    // --- Intoxication ---

    [Fact]
    public void Intoxication_hidden_when_setting_off()
    {
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowIntoxication = false;
        var stats = new FakePlayerStatsService { Intoxication = 0.5f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Null(vm.IntoxicationText);
        Assert.False(vm.IsIntoxicationLineVisible);
    }

    [Fact]
    public void Intoxication_hidden_when_zero_even_if_setting_on()
    {
        // Sober player should not see the line — matches Status HUD Continued.
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowIntoxication = true;
        var stats = new FakePlayerStatsService { Intoxication = 0f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.Null(vm.IntoxicationText);
        Assert.False(vm.IsIntoxicationLineVisible);
    }

    [Fact]
    public void Intoxication_renders_rounded_percent_from_normalized_value()
    {
        // 0.456 -> 46% (banker's rounding via Math.Round defaults to even, but
        // 0.456 * 100 = 45.6 which always rounds to 46 regardless of mode).
        var settings = new HudClockSettings();
        settings.PlayerStats.ShowIntoxication = true;
        var stats = new FakePlayerStatsService { Intoxication = 0.456f };

        var vm = MakeVm(settings: settings, playerStats: stats);

        Assert.NotNull(vm.IntoxicationText);
        Assert.Contains("46", vm.IntoxicationText);
        Assert.Contains("intoxication", vm.IntoxicationText);
    }

    // --- Rainfall ---

    [Fact]
    public void Rainfall_hidden_when_setting_off()
    {
        var settings = new HudClockSettings();
        settings.Weather.ShowRainfall = false;
        var weather = new FakeWeatherService { Rainfall = 0.5f };

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Null(vm.RainfallText);
        Assert.False(vm.IsRainfallLineVisible);
    }

    [Theory]
    [InlineData(0.00f, "rare")]
    [InlineData(0.09f, "rare")]
    [InlineData(0.10f, "light")]
    [InlineData(0.29f, "light")]
    [InlineData(0.30f, "moderate")]
    [InlineData(0.54f, "moderate")]
    [InlineData(0.55f, "high")]
    [InlineData(0.79f, "high")]
    [InlineData(0.80f, "veryhigh")]
    [InlineData(1.00f, "veryhigh")]
    public void Rainfall_buckets_match_thresholds(float value, string expectedBucketStem)
    {
        // The viewmodel should map normalized rainfall to vanilla
        // Environment-dialog-style buckets at the documented thresholds.
        // The test asserts the bucket stem appears in the output, since the
        // identity translator returns the lang key as-is and it includes the
        // stem.
        var settings = new HudClockSettings();
        settings.Weather.ShowRainfall = true;
        var weather = new FakeWeatherService { Rainfall = value };

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.NotNull(vm.RainfallText);
        Assert.Contains("rainfall-" + expectedBucketStem, vm.RainfallText);
    }

    [Fact]
    public void Wind_hidden_returns_null_regardless_of_speed()
    {
        var settings = new HudClockSettings();
        settings.Weather.Wind = WindDisplay.Hidden;
        var weather = new FakeWeatherService { WindSpeed = 0.5 };

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Null(vm.WindText);
    }

    [Fact]
    public void Wind_percentage_renders_rounded_percent()
    {
        var settings = new HudClockSettings();
        settings.Weather.Wind = WindDisplay.Percentage;
        var weather = new FakeWeatherService { WindSpeed = 0.347 };

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Contains("35%", vm.WindText);
    }

    [Fact]
    public void Wind_beaufort_text_uses_beaufort_key_with_level()
    {
        var settings = new HudClockSettings();
        settings.Weather.Wind = WindDisplay.BeaufortText;
        var weather = new FakeWeatherService { WindSpeed = 0.65 }; // Beaufort 8

        var vm = MakeVm(settings: settings, weather: weather);

        Assert.Contains("wind-beaufort-8", vm.WindText);
    }

    [Fact]
    public void Date_only_formats_as_expected()
    {
        var settings = new HudClockSettings();
        settings.Time.ShowDate = true;
        settings.Time.ShowTime = false;
        var calendar = new FakeCalendarService
        {
            Snapshot = new global::HudClock.Domain.Calendar.CalendarSnapshot(15, "june", 1047, 0, 0),
        };

        var vm = MakeVm(settings: settings, calendar: calendar);

        Assert.Contains("15", vm.DateAndTimeText);
        Assert.Contains("1047", vm.DateAndTimeText);
    }

    [Fact]
    public void Time_only_uses_time_formatter()
    {
        var settings = new HudClockSettings();
        settings.Time.ShowDate = false;
        settings.Time.ShowTime = true;
        var calendar = new FakeCalendarService
        {
            Snapshot = new global::HudClock.Domain.Calendar.CalendarSnapshot(1, "january", 1, 14, 30),
        };

        var vm = MakeVm(settings: settings, calendar: calendar);

        Assert.Equal("14:30", vm.DateAndTimeText);
    }

    [Fact]
    public void Season_icon_path_matches_season_on_default_modern_theme()
    {
        var calendar = new FakeCalendarService { Season = EnumSeason.Fall };

        var vm = MakeVm(calendar: calendar);

        Assert.Equal("hudclock:textures/hud/modern/fall-large.png", vm.SeasonIconPath);
    }

    [Fact]
    public void Season_icon_path_resolves_classic_theme_when_selected()
    {
        var settings = new HudClockSettings();
        settings.Appearance.IconTheme = IconTheme.Classic;
        var calendar = new FakeCalendarService { Season = EnumSeason.Fall };

        var vm = MakeVm(settings: settings, calendar: calendar);

        Assert.Equal("hudclock:textures/hud/classic/fall-large.png", vm.SeasonIconPath);
    }

    [Fact]
    public void Room_icon_path_matches_status_on_default_modern_theme()
    {
        var settings = new HudClockSettings();
        settings.Room.ShowRoomIndicator = true;
        var room = new FakeRoomService { CurrentStatus = global::HudClock.Domain.Rooms.RoomStatus.Greenhouse };

        var vm = MakeVm(settings: settings, room: room);

        Assert.Equal("hudclock:textures/room/modern/greenhouse.png", vm.RoomIconPath);
    }

    [Fact]
    public void Room_icon_path_resolves_classic_theme_when_selected()
    {
        var settings = new HudClockSettings();
        settings.Room.ShowRoomIndicator = true;
        settings.Appearance.IconTheme = IconTheme.Classic;
        var room = new FakeRoomService { CurrentStatus = global::HudClock.Domain.Rooms.RoomStatus.Greenhouse };

        var vm = MakeVm(settings: settings, room: room);

        Assert.Equal("hudclock:textures/room/classic/greenhouse.png", vm.RoomIconPath);
    }

    // --- Constructor guards ---

    [Fact]
    public void Constructor_rejects_null_settings()
    {
        var calendar = new FakeCalendarService();
        var weather = new FakeWeatherService();
        var rift = new FakeRiftService();
        var room = new FakeRoomService();
        var claim = new FakeClaimService();
        var stats = new FakePlayerStatsService();
        var formatter = new StubTimeFormatter();

        Assert.Throws<System.ArgumentNullException>(() =>
            new MainHudViewModel(
                null!, formatter, calendar, weather, rift, room, claim, stats,
                Identity, IdentityFormat,
                () => new BlockPos(0, 0, 0),
                () => 0,
                isMultiplayer: false));
    }
}
