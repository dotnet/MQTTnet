// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace MQTTnet.Packets;

public sealed class MqttUserProperty
{
    readonly string _stringValue;
    readonly ReadOnlyMemory<byte> _valueBuffer;
    readonly bool _hasValueBuffer;
    string _cachedValue;

    public MqttUserProperty(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _stringValue = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MqttUserProperty(string name, ArraySegment<byte> value)
        : this(name, CreateMemory(value))
    {
    }

    public MqttUserProperty(string name, ReadOnlyMemory<byte> value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _valueBuffer = value;
        _hasValueBuffer = true;

        if (value.Length == 0)
        {
            _stringValue = string.Empty;
        }
    }

    public string Name { get; }

    public bool HasValueBuffer => _hasValueBuffer;

    public ReadOnlyMemory<byte> ValueBuffer => _valueBuffer;

    public string Value
    {
        get
        {
            if (!_hasValueBuffer)
            {
                return _stringValue;
            }

            if (_cachedValue != null)
            {
                return _cachedValue;
            }

            if (_stringValue != null)
            {
                _cachedValue = _stringValue;
                return _cachedValue;
            }

            _cachedValue = Encoding.UTF8.GetString(_valueBuffer.Span);
            return _cachedValue;
        }
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as MqttUserProperty);
    }

    public bool Equals(MqttUserProperty other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if (_hasValueBuffer && other._hasValueBuffer)
        {
            return _valueBuffer.Span.SequenceEqual(other._valueBuffer.Span);
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }


    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        if (!string.IsNullOrEmpty(Name))
        {
            hashCode.Add(Name, StringComparer.Ordinal);
        }
        else
        {
            hashCode.Add(0);
        }

        if (_hasValueBuffer)
        {
            hashCode.AddBytes(_valueBuffer.Span);
        }
        else
        {
            hashCode.Add(_stringValue ?? string.Empty, StringComparer.Ordinal);
        }

        return hashCode.ToHashCode();
    }


    public override string ToString()
    {
        return $"{Name} = {Value}";
    }

    static ReadOnlyMemory<byte> CreateMemory(ArraySegment<byte> value)
    {
        if (value.Array == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ReadOnlyMemory<byte>(value.Array, value.Offset, value.Count);
    }
}