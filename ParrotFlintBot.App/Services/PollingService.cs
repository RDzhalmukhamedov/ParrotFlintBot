using Microsoft.Extensions.Logging;
using ParrotFlintBot.App.Abstract;

namespace ParrotFlintBot.App.Services;

public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : base(serviceProvider, logger)
    {
    }
}