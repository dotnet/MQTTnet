using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains;

[Reentrant]
[StatelessWorker]
public class PublishMessageGrain : Grain, IPublishMessageGrain
{
    private readonly IGrainFactory _grainFactory;
    private readonly PublishMessageBehavior _behavior;
    private readonly ILogger<PublishMessageGrain> _logger;
    private readonly Dictionary<string, List<PublishMessageRoutine>> _messageQueues = new();
    private readonly Dictionary<string, HashSet<SiloAddress>> _nodes = new();
    private readonly Dictionary<ulong, PublishMessageRoutineTracker> _routineTrackers = new();
    private Task _flushTask = Task.CompletedTask;
    private ulong _routineId = 0;
    
    public PublishMessageGrain(IGrainFactory grainFactory, PublishMessageBehavior behavior, ILogger<PublishMessageGrain> logger)
    {
        _grainFactory = grainFactory;
        _behavior = behavior;
        _logger = logger;
    }

    private ulong NewRoutineId()
    {
        unchecked
        {
            if (++_routineId == 0)
                _routineId++;
            return _routineId;
        }
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RegisterTimer(
            _ =>
            {
                Flush();
                return Task.CompletedTask;
            },
            null,
            TimeSpan.FromMilliseconds(15),
            TimeSpan.FromMilliseconds(15));

        await RefreshNodesAsync();

        RegisterTimer(
            asyncCallback: async _ => await RefreshNodesAsync(),
            state: null,
            dueTime: TimeSpan.FromSeconds(60),
            period: TimeSpan.FromSeconds(60));

        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Flush();
        await _flushTask;
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async ValueTask<PublishMessageResult> PublishMessageAcrossNodes(ClusterApplicationMessage message)
    {
        var nodeGroupName = await _behavior.GetTargetNodeGroupNameAsync(message);
        if (!_messageQueues.TryGetValue(nodeGroupName, out var messageQueue))
            _messageQueues.Add(nodeGroupName, messageQueue = new());
        messageQueue.Add(new PublishMessageRoutine
        { 
            RoutineId = NewRoutineId(),
            Message = message
        });

        if (messageQueue.Count > 25)
        {
            Flush();
        }

        return new PublishMessageResult(0);
    }

    private async ValueTask RefreshNodesAsync()
    {
        foreach (var nodeGroupName in _messageQueues.Keys.ToList())
        {
            var nodeListGrain = _grainFactory.GetGrain<INodeListGrain>(nodeGroupName);
            _nodes[nodeGroupName] = await nodeListGrain.GetNodes();
        }
    }

    private void Flush()
    {
        if (_flushTask.IsCompleted)
        {
            _flushTask.Ignore();
            _flushTask = FlushInternalAsync();
        }
        
        async Task FlushInternalAsync()
        {
            foreach (var queueKvp in _messageQueues)
            { 
                if (!_nodes.TryGetValue(queueKvp.Key, out var nodes))
                {
                    // Purge items if they keep building up because no nodes are available to relay to.
                    // TODO: Add logging here
                    // TODO: Make upper limit configurable
                    var removeItemCount = Math.Max(queueKvp.Value.Count - 10000, 0);
                    if (removeItemCount > 0)
                        queueKvp.Value.RemoveRange(10000, removeItemCount);
                    return;
                }

                if (queueKvp.Value.Count == 0)
                    return;

                do
                {
                    var messagesToSend = queueKvp.Value.ToArray();
                    queueKvp.Value.Clear();

                    var tasks = new List<Task<IEnumerable<PublishMessageRoutineResult>?>>();
                    var publishMessagesBatch = new PublishMessageBatch { Messages = messagesToSend };
                    foreach (var node in nodes)
                    {
                        var nodeGrain = _grainFactory.GetGrain<INodeGrain>(node.ToString());
                        tasks.Add(BroadcastMessagesAsync(node, publishMessagesBatch, nodeGrain, _logger));

                        static async Task<IEnumerable<PublishMessageRoutineResult>?> BroadcastMessagesAsync(SiloAddress address, PublishMessageBatch batch, INodeGrain nodeGrain, ILogger logger)
                        {
                            try
                            {
                                return await nodeGrain.PublishMessagesAsync(batch);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error broadcasting to host {NodeAddress}.", address);
                                return null;
                            }
                        }
                    }

                    await Task.WhenAll(tasks);

                    foreach (var routineResults in tasks)
                    {
                        if (routineResults.Result == null)
                            continue;

                        foreach (var routineResult in routineResults.Result)
                        {
                            if (!_routineTrackers.TryGetValue(routineResult.RoutineId, out var tracker))
                                continue;

                            tracker.TaskCompletionSource.SetResult(routineResult);
                        }
                    }
                } while (queueKvp.Value.Count > 25);
            }
        }
    }

    class PublishMessageRoutineTracker
    {

        public required PublishMessageRoutine Routine { get; init; }

        public required DateTime Started { get; init; }

        public TaskCompletionSource<PublishMessageRoutineResult> TaskCompletionSource { get; } = new();

    }

}