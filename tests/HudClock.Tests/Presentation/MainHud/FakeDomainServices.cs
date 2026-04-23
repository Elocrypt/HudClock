using HudClock.Domain.Calendar;
using HudClock.Domain.Claims;
using HudClock.Domain.Rifts;
using HudClock.Domain.Rooms;
using HudClock.Domain.Time;
using HudClock.Domain.Weather;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Tests.Presentation.MainHud;

// Minimal fakes for the services consumed by MainHudViewModel. Each exposes
// mutable fields so tests can script scenarios without setting up mocks.

internal sealed class FakeCalendarService : ICalendarService
{
    public CalendarSnapshot Snapshot { get; set; } = new(1, "january", 1, 9, 0);
    public EnumSeason Season { get; set; } = EnumSeason.Winter;
    public CalendarSnapshot GetSnapshot() => Snapshot;
    public EnumSeason GetSeason(BlockPos pos) => Season;
}

internal sealed class FakeWeatherService : IWeatherService
{
    public double WindSpeed { get; set; }
    public float TemperatureCelsius { get; set; }
    public double GetWindSpeed(BlockPos pos) => WindSpeed;
    public float GetTemperatureCelsius(BlockPos pos) => TemperatureCelsius;
}

internal sealed class FakeRiftService : IRiftService
{
    public bool IsAvailable { get; set; }
    public string? ActivityCode { get; set; }
    public string? GetCurrentActivityCode() => ActivityCode;
}

internal sealed class FakeRoomService : IRoomService
{
    public RoomStatus CurrentStatus { get; set; } = RoomStatus.None;
    public void Dispose() { }
}

internal sealed class FakeClaimService : IClaimService
{
    public ClaimInfo? Result { get; set; }
    public ClaimInfo? GetClaimAt(BlockPos pos) => Result;
}

internal sealed class StubTimeFormatter : ITimeFormatter
{
    public string Format(int hour24, int minute) => $"{hour24:00}:{minute:00}";
}
