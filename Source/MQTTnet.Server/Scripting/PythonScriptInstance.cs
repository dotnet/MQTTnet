using System;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace MQTTnet.Server.Scripting
{
    public class PythonScriptInstance
    {
        private readonly ScriptScope _scriptScope;

        public PythonScriptInstance(string uid, string path, ScriptScope scriptScope)
        {
            Uid = uid;
            Path = path;

            _scriptScope = scriptScope;
        }

        public string Uid { get; }

        public string Path { get; }

        public bool InvokeOptionalFunction(string name, params object[] parameters)
        {
            return InvokeOptionalFunction(name, parameters, out _);
        }

        public bool InvokeOptionalFunction(string name, object[] parameters, out object result)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptScope)
            {
                if (!_scriptScope.Engine.Operations.TryGetMember(_scriptScope, name, out var member))
                {
                    result = null;
                    return false;
                }

                if (!(member is PythonFunction function))
                {
                    throw new Exception($"Member '{name}' is no Python function.");
                }

                try
                {
                    result = _scriptScope.Engine.Operations.Invoke(function, parameters);
                    return true;
                }
                catch (Exception exception)
                {
                    var details = _scriptScope.Engine.GetService<ExceptionOperations>().FormatException(exception);
                    var message = $"Error while invoking function '{name}'. " + Environment.NewLine + details;

                    throw new Exception(message, exception);
                }
            }
        }
    }
}
