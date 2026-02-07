using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace MQTTnet.PowerShell.Cmdlets;

[Cmdlet(VerbsLifecycle.Register, "MqttMessageHandler")]
public class RegisterMqttMessageHandlerCmdlet : PSCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public required PsMqttSession Session { get; set; }

    [Parameter(Mandatory = true)]
    public ScriptBlock? Action { get; set; }

    protected override void ProcessRecord()
    {
        // Capture the ScriptBlock and current default runspace (available during cmdlet execution)
        var scriptBlock = Action ?? throw new ArgumentNullException(nameof(Action));
        var capturedRunspace = Runspace.DefaultRunspace ?? throw new InvalidOperationException("No default runspace available during cmdlet execution.");
        var capturedHost = Host;

        EventHandler<PsMqttMessage> handler = (sender, e) =>
        {
            // Run asynchronously to avoid blocking the event thread
            Task.Run(() =>
            {
                // Temporarily set the default runspace for this thread
                var originalRunspace = Runspace.DefaultRunspace;
                Runspace.DefaultRunspace = capturedRunspace;

                try
                {
                    // Invoke the ScriptBlock directly with arguments (returns Collection<PSObject>)
                    var results = scriptBlock.Invoke(e.Topic, e.Payload, e); // Pass topic, payload, and full message

                    // Handle output manually (e.g., write to host/console)
                    foreach (var result in results)
                    {
                        capturedHost.UI.WriteLine(result?.ToString() ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    // Handle errors by writing to host error stream
                    capturedHost.UI.WriteErrorLine($"MQTT handler error: {ex.Message}");
                    // Optionally: capturedHost.UI.WriteVerboseLine(ex.StackTrace); for more details
                }
                finally
                {
                    // Restore the original default runspace for this thread
                    Runspace.DefaultRunspace = originalRunspace;
                }
            });
        };

        Session.MessageReceived += handler;
        WriteObject($"Handler registered for MQTT messages on session.");
    }
}