using Vintagestory.API.MathTools;

namespace HudClock.Domain.Weather;

/// <summary>
/// Read access to the game's weather system for the main HUD.
/// Returns neutral numeric data; viewmodels handle unit conversion and formatting.
/// </summary>
internal interface IWeatherService
{
    /// <summary>Normalized wind speed at the given position, or 0 if weather data isn't yet available.</summary>
    double GetWindSpeed(BlockPos pos);

    /// <summary>Ambient temperature at the given position in degrees Celsius.</summary>
    float GetTemperatureCelsius(BlockPos pos);

    /// <summary>
    /// Normalized <i>current</i> precipitation in [0, 1] at the given position
    /// (how hard it is raining right now). Returns 0 when climate data isn't
    /// available. Distinct from <see cref="GetAreaRainfall"/>.
    /// </summary>
    float GetRainfall(BlockPos pos);

    /// <summary>
    /// Normalized <i>area</i> (world-generation) rainfall in [0, 1] at the
    /// given position — the biome's baseline wetness, i.e. how rainy the area
    /// tends to be. This is the value the vanilla Environment dialog shows as
    /// "Rainfall in this Area", and is independent of the current weather.
    /// Returns 0 when climate data isn't available.
    /// </summary>
    float GetAreaRainfall(BlockPos pos);
}
