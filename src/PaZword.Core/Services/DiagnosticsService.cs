using Microsoft.Extensions.Logging;

namespace PaZword.Core.Services;

/// <summary>
/// Example service stub — demonstrates constructor-injection pattern under AOT/MEDI.
/// Sprint 1+ will replace this with real domain services.
/// </summary>
public sealed class DiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;

    public DiagnosticsService(ILogger<DiagnosticsService> logger)
    {
        _logger = logger;
    }

    public void LogStartup() =>
        _logger.LogInformation("PaZword startup — DiagnosticsService operational.");
}
