using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Clustering.Orleans.Server.Options;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Services;

public class NodeListUpdaterService : BackgroundService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<NodeListUpdaterService> _nodeListUpdater;
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly NodeSettings _nodeSettings;
    private readonly ILogger<NodeListUpdaterService> _logger;

    public NodeListUpdaterService(
        IGrainFactory grainFactory,
        ILogger<NodeListUpdaterService> nodeListUpdater,
        ILocalSiloDetails localSiloDetails,
        IOptions<NodeSettings> nodeSettings,
        ILogger<NodeListUpdaterService> logger)
    {
        _grainFactory = grainFactory;
        _nodeListUpdater = nodeListUpdater;
        _localSiloDetails = localSiloDetails;
        _nodeSettings = nodeSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nodeListGrain = _grainFactory.GetGrain<INodeListGrain>(_nodeSettings.GroupName);
        var localSiloAddress = _localSiloDetails.SiloAddress;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await nodeListGrain.AddNode(localSiloAddress);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error posting location to node list grain");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch
                {
                    // Ignore cancellation exceptions
                }
            }
        }
    }

}
