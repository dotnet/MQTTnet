# MQTTnet.Extensions.DelayedPublish

Adds EMQX-compatible **delayed publish** support to the MQTTnet broker.

A client publishes to a topic of the form:

```
$delayed/<Interval>/<RealTopic>
```

Where `<Interval>` is either:

* A relative delay in **seconds** (1 .. 4 294 967), or
* An absolute **Unix epoch time in seconds** (values above the threshold are interpreted as an absolute due time).

The broker holds the message and republishes it on `<RealTopic>` when the interval elapses.

## Usage

```csharp
using MQTTnet.Extensions.DelayedPublish;

var server = new MqttServerFactory().CreateMqttServer(options);
await server.StartAsync();

using var _ = server.UseDelayedPublish();
```

See the `DelayedPublishOptions` type for limits (maximum delay, maximum pending messages, topic prefix, etc.).

## Notes

* QoS 1/2 acknowledgements are sent to the publisher immediately when the broker accepts the delayed publish; the delay only affects dispatch to subscribers.
* MQTT 5 `MessageExpiryInterval` is decremented by the elapsed delay at dispatch time; messages that have already expired are dropped.
* The default store is in-memory; implement `IDelayedMessageStore` for durable persistence.
