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
}
