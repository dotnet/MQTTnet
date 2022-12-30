using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    public class NodeListGrain : Grain, INodeListGrain
    {
        private readonly IClusterMembershipService _clusterMembership;
        private readonly HashSet<SiloAddress> _nodes = new();
        private MembershipVersion _cacheMembershipVersion;
        private HashSet<SiloAddress>? _cache;

        public NodeListGrain(IClusterMembershipService clusterMembership)
        {
            _clusterMembership = clusterMembership;
        }

        public ValueTask AddNode(SiloAddress host)
        {
            _cache = null;
            _nodes.Add(host);

            return default;
        }

        public ValueTask<HashSet<SiloAddress>> GetNodes()
            => new(GetCachedNodes());

        private HashSet<SiloAddress> GetCachedNodes()
        {
            var clusterMembers = _clusterMembership.CurrentSnapshot;
            if (_cache is { } && clusterMembers.Version == _cacheMembershipVersion)
            {
                return _cache;
            }
            
            var nodes = new HashSet<SiloAddress>();
            var toDelete = new List<SiloAddress>();
            foreach (var node in _nodes)
            {
                var hostStatus = clusterMembers.GetSiloStatus(node);
                switch (hostStatus)
                {
                    case SiloStatus.Dead:
                        toDelete.Add(node);
                        break;
                    case SiloStatus.Active:
                        nodes.Add(node);
                        break;
                }
            }

            foreach (var node in toDelete)
                _nodes.Remove(node);

            _cache = nodes;
            _cacheMembershipVersion = clusterMembers.Version;

            return nodes;
        }

    }
}
