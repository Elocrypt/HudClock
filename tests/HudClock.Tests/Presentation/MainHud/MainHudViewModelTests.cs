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
        bool isMultiplayer = false,
        int onlinePlayers = 0)
    {
        settings ??= new HudClockSettings();
        calendar ??= new FakeCalendarService();
        weather ??= new FakeWeatherService();
        rift ??= new FakeRiftService();
        room ??= new FakeRoomService();
        claim ??= new FakeClaimService();
        var formatter = new StubTimeFormatter();

        var vm = new MainHudViewModel(
            settings, formatter, calendar, weather, rift, room, claim,
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
        var formatter = new StubTimeFormatter();

        Assert.Throws<System.ArgumentNullException>(() =>
            new MainHudViewModel(
                null!, formatter, calendar, weather, rift, room, claim,
                Identity, IdentityFormat,
                () => new BlockPos(0, 0, 0),
                () => 0,
                isMultiplayer: false));
    }
}
