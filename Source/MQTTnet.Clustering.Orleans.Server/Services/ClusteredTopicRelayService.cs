using MQTTnet.Clustering.Orleans.Server.Behaviors;
using MQTTnet.Clustering.Orleans.Server.Internals;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Services
{
    public class ClusteredTopicRelayService
    {
        private readonly Dictionary<ulong, HashSet<ClusteredRelayBehaviorWithTopic>> _noWildcardBehaviorsByTopicHash = new();
        private readonly Dictionary<ulong, TopicHashMaskBehaviors> _wildcardBehaviorsByTopicHash = new();
        private readonly AsyncLock _behaviorsLock = new();
        private ClusteredRelayBehavior? _defaultBehavior;
        
        internal async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs arg)
        {
            var applicationMessage = arg.ApplicationMessage;
            var possibleBehaviors = new List<ClusteredRelayBehaviorWithTopic>();
            MqttSubscription.CalculateTopicHash(applicationMessage.Topic, out var topicHash, out _, out _);
            
            using (await _behaviorsLock.EnterAsync(CancellationToken.None))
            {
                if (_noWildcardBehaviorsByTopicHash.TryGetValue(topicHash, out var noWildcardBehaviors))
                {
                    possibleBehaviors.AddRange(noWildcardBehaviors);
                }

                foreach (var wcb in _wildcardBehaviorsByTopicHash)
                {
                    var behaviorTopicHash = wcb.Key;
                    var behaviorsByHashMask = wcb.Value.BehaviorsByHashMask;

                    foreach (var bhm in behaviorsByHashMask)
                    {
                        var behaviorHashMask = bhm.Key;

                        if ((topicHash & behaviorHashMask) == behaviorTopicHash)
                        {
                            var behaviors = bhm.Value;
                            foreach (var behavior in behaviors)
                            {
                                if (MqttTopicFilterComparer.Compare(arg.ApplicationMessage.Topic, behavior.Topic) != MqttTopicFilterCompareResult.IsMatch)
                                    continue;

                                await behavior.Handler(arg);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void SetDefaultBehavior(ClusteredRelayBehavior behavior)
        {
            _defaultBehavior = behavior;
        }

        public void ClearDefaultBehavior()
        {
            _defaultBehavior = null;
        }

        public async Task AddBehaviorAsync(ClusteredRelayBehaviorWithTopic behavior)
        {
            using (await _behaviorsLock.EnterAsync())
            {
                if (!behavior.TopicHasWildcard)
                {
                    if (!_noWildcardBehaviorsByTopicHash.TryGetValue(behavior.TopicHash, out var noWildcardBehaviors))
                    {
                        noWildcardBehaviors = new();
                        _noWildcardBehaviorsByTopicHash.Add(behavior.TopicHash, noWildcardBehaviors);
                    }
                    noWildcardBehaviors.Add(behavior);
                    return;
                }

                if (!_wildcardBehaviorsByTopicHash.TryGetValue(behavior.TopicHash, out var wildcardBehaviors))
                {
                    wildcardBehaviors = new TopicHashMaskBehaviors();
                    _wildcardBehaviorsByTopicHash.Add(behavior.TopicHash, wildcardBehaviors);
                }

                if (!wildcardBehaviors.BehaviorsByHashMask.TryGetValue(behavior.TopicHashMask, out var behaviors))
                {
                    behaviors = new HashSet<ClusteredRelayBehaviorWithTopic>();
                    wildcardBehaviors.BehaviorsByHashMask.Add(behavior.TopicHashMask, behaviors);
                }

                behaviors.Add(behavior);
            }
        }

        public async Task RemoveBehaviorAsync(string topicFilter)
        {
            MqttSubscription.CalculateTopicHash(topicFilter, out var topicHash, out var topicHashMask, out var hasWildcard);

            using (await _behaviorsLock.EnterAsync())
            {
                if (!hasWildcard)
                {
                    if (!_noWildcardBehaviorsByTopicHash.TryGetValue(topicHash, out var noWildcardBehaviors))
                        return;

                    noWildcardBehaviors.RemoveWhere(b => b.Topic == topicFilter);
                    return;
                }

                if (!_wildcardBehaviorsByTopicHash.TryGetValue(topicHash, out var wildcardBehaviors))
                    return;

                if (!wildcardBehaviors.BehaviorsByHashMask.TryGetValue(topicHashMask, out var behaviors))
                    return;

                var behavior = behaviors.FirstOrDefault(b => b.Topic == topicFilter);
                if (behavior == null)
                    return;
                behaviors.Remove(behavior);

                if (behaviors.Any())
                    return;
                
                wildcardBehaviors.BehaviorsByHashMask.Remove(topicHashMask);

                if (wildcardBehaviors.BehaviorsByHashMask.Any())
                    return;

                _wildcardBehaviorsByTopicHash.Remove(topicHash);
            }
        }

        internal async Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
        {
            
        }

        internal async Task OnClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
        {
            
        }

    }
}
