// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;

namespace MQTTnet.Server.Internal;

public sealed class MqttSessionsStorage
{
    readonly Dictionary<string, MqttSession> _sessions = new Dictionary<string, MqttSession>(4096);

    public void Clear()
    {
        // Make sure that the sessions are also getting disposed!
        // This object can be reused after disposing.
        Dispose();
    }

    public void Dispose()
    {
        foreach (var sessionItem in _sessions)
        {
            sessionItem.Value.Dispose();
        }

        _sessions.Clear();
    }

    public IReadOnlyCollection<MqttSession> ReadAllSessions()
    {
        return new ReadOnlyCollection<MqttSession>(_sessions.Values.ToList());
    }

    public bool TryGetSession(string id, out MqttSession session)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!_sessions.TryGetValue(id, out session))
        {
            return false;
        }

        // If the Session Expiry Interval is 0xFFFFFFFF (UINT_MAX), the Session does not expire.
        if (session.ExpiryInterval > 0 && session.ExpiryInterval != uint.MaxValue)
        {
            if (session.DisconnectedTimestamp.HasValue)
            {
                if (DateTime.UtcNow > session.DisconnectedTimestamp.Value.AddSeconds(session.ExpiryInterval))
                {
                    TryRemoveSession(id, out var oldSession);
                    oldSession?.Dispose();
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryRemoveSession(string id, out MqttSession session)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (_sessions.TryGetValue(id, out session))
        {
            _sessions.Remove(id);
            return true;
        }

        return false;
    }

    public void UpdateSession(string id, MqttSession session)
    {
        ArgumentNullException.ThrowIfNull(id);

        _sessions[id] = session;
    }
}