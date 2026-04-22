using System;
using Vintagestory.API.Common;

namespace HudClock.Core;

/// <summary>
/// Lightweight scoped logger that prefixes every message with <c>[HudClock]</c>
/// so output from this mod is easy to filter in the game log. Thin wrapper over
/// <see cref="ILogger"/>; one instance is created at mod startup and passed
/// through the service graph.
/// </summary>
internal sealed class ModLog
{
    private const string Prefix = "[HudClock] ";

    private readonly ILogger _logger;

    public ModLog(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Parameterless overloads route through "{0}" so message content containing
    // literal braces (e.g. JSON fragments) is not interpreted as format slots.

    public void Notification(string message) => _logger.Notification("{0}", Prefix + message);

    public void Warning(string message) => _logger.Warning("{0}", Prefix + message);

    public void Error(string message) => _logger.Error("{0}", Prefix + message);

    public void Debug(string message) => _logger.Debug("{0}", Prefix + message);

    public void Notification(string format, params object[] args) => _logger.Notification(Prefix + format, args);

    public void Warning(string format, params object[] args) => _logger.Warning(Prefix + format, args);

    public void Error(string format, params object[] args) => _logger.Error(Prefix + format, args);

    public void Debug(string format, params object[] args) => _logger.Debug(Prefix + format, args);
}
