using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HudClock.Domain.Weather;

/// <summary>
/// Production <see cref="IWeatherService"/>. Wraps
/// <see cref="WeatherSystemBase"/> for wind and <c>BlockAccessor.GetClimateAt</c>
/// for temperature.
/// </summary>
/// <remarks>
/// Reuses a single <see cref="Vec3d"/> scratch instance to avoid the per-tick
/// allocation the 3.x <c>TimeDialog.getWindSpeed</c> caused.
/// </remarks>
internal sealed class WeatherService : IWeatherService
{
    private readonly ICoreClientAPI _api;
    private readonly WeatherSystemBase? _weather;
    private readonly Vec3d _scratch = new();

    public WeatherService(ICoreClientAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _weather = api.ModLoader.GetModSystem<WeatherSystemBase>();
    }

    /// <inheritdoc />
    public double GetWindSpeed(BlockPos pos)
    {
        if (pos is null) throw new ArgumentNullException(nameof(pos));
        if (_weather?.WeatherDataSlowAccess is null) return 0.0;

        _scratch.X = pos.X;
        _scratch.Y = pos.Y;
        _scratch.Z = pos.Z;
        return _weather.WeatherDataSlowAccess.GetWindSpeed(_scratch);
    }

    /// <inheritdoc />
    public float GetTemperatureCelsius(BlockPos pos)
    {
        if (pos is null) throw new ArgumentNullException(nameof(pos));
        return _api.World.BlockAccessor.GetClimateAt(pos).Temperature;
    }
}
