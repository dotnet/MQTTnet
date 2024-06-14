# MQTTNet.Extensions.TopicTemplate

Provides mqtt topic templating logic to support dispatch, routing and
similar functionality based on the well known moustache syntax, aka
AsyncAPI dynamic channel address.

Generating and parsing MQTT topics and topic filters is often done in
an ad-hoc fashion, leading to error-prone code and subtly introduced
showstoppers like forgetting to strip a slash from a parameter.  To
remedy, this extension aids you write safe, maintainable logic
for topic generation, routing and dispatch.


## How it works

The package lets you write MQTTNet code like:
```csharp
var template = new MqttTopicTemplate("App/v1/{sender}/message");

var filter = new MqttTopicFilterBuilder()
    .WithTopicTemplate(template) // (1)
    .WithAtLeastOnceQoS()
    .WithNoLocal()
    .Build();

// filter.Topic == "App/v1/+/message"

var myTopic = template
    .WithParameter("sender", "me, myself & i"); // (2)

var message = new MqttApplicationMessageBuilder()
    .WithTopicTemplate(  // (3)
        myTopic)
    .WithPayload("Hello!")
    .Build();

// message.Topic == "App/v1/me, myself & i/message"

Assert.IsTrue(message.MatchesTopicTemplate(template)); // (4)
```

At (1), the parameter "sender" was automatically replaced by a plus sign
(single level wildcard) because the caller is creating a topic filter.

Using the same template (they are immutable), you can send
a message. At (2), we replace the parameter with a known value.
If that value contained a level separator or topic filter only
characters, we'd get an ArgumentException. If we forgot to replace
the parameter "sender" (e.g. when placing template not myTopic),
we would be prevented from sending at (3).

On the receiving side, a message can be matched to a template
using code like at (4).

These daily tasks become much safer and easier to code than 
string.Format() and Split() allow for. More elaborate computation is supported
too. You could extract the sender using the template or route
messages based on topic content.

## Features

- Safe handling of topic parameters in moustache syntax
- Simplifies topic generation and parsing logic
- Match topics and extract the values of their parameters
- Translate between topic templates to ease dynamic routing
- Derive a canonical topic filter from a bunch of templates
- Integrates with MQTTNet message, subscription and filter builders
- Extension method friendly (sealed class with one public ctor)

## Additional documentation

- Some of the [unit tests](https://github.com/dotnet/MQTTnet/blob/master/Source/MQTTnet.Tests/Extensions/MqttTopicTemplate_Tests.cs) are instructive
- Some examples feature the extension, e.g. TopicGenerator, Client_Subscribe_Samples

## Feedback

Report any issues with the main project on https://github.com/dotnet/MQTTnet/issues


