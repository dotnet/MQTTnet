using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace MQTTnet.Server.Scripting
{
    public class PythonScriptHostService
    {
        private readonly IDictionary<string, object> _proxyObjects = new ExpandoObject();
        private readonly List<PythonScriptInstance> _scriptInstances = new List<PythonScriptInstance>();
        private readonly ILogger<PythonScriptHostService> _logger;
        private readonly ScriptEngine _scriptEngine;
        
        public PythonScriptHostService(PythonIOStream pythonIOStream, ILogger<PythonScriptHostService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _scriptEngine = IronPython.Hosting.Python.CreateEngine();
            _scriptEngine.Runtime.IO.SetOutput(pythonIOStream, Encoding.UTF8);
        }

        public void Configure()
        {
            AddSearchPaths(_scriptEngine);

            var scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            foreach (var filename in Directory.GetFiles(scriptsDirectory, "*.py", SearchOption.AllDirectories).OrderBy(file => file))
            {
                TryInitializeScript(filename);
            }
        }

        public void RegisterProxyObject(string name, object action)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (action == null) throw new ArgumentNullException(nameof(action));

            _proxyObjects.Add(name, action);
        }

        public void InvokeOptionalFunction(string name, object parameters)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            lock (_scriptInstances)
            {
                foreach (var pythonScriptInstance in _scriptInstances)
                {
                    try
                    {
                        pythonScriptInstance.InvokeOptionalFunction(name, parameters);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"Error while invoking function '{name}' at script '{pythonScriptInstance.Name}'.");
                    }
                }
            }
        }

        private void TryInitializeScript(string filename)
        {
            try
            {
                var scriptName = new FileInfo(filename).Name;

                _logger.LogTrace($"Initializing Python script '{scriptName}'...");
                var code = File.ReadAllText(filename);

                var scriptInstance = CreateScriptInstance(scriptName, code);
                scriptInstance.InvokeOptionalFunction("initialize");

                _scriptInstances.Add(scriptInstance);

                _logger.LogInformation($"Initialized script '{scriptName}'.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while initializing script '{new FileInfo(filename).Name}'.");
            }
        }

        private PythonScriptInstance CreateScriptInstance(string name, string code)
        {
            var scriptScope = _scriptEngine.CreateScope();

            var source = scriptScope.Engine.CreateScriptSourceFromString(code, SourceCodeKind.File);
            var compiledCode = source.Compile();

            scriptScope.SetVariable("mqtt_net_server", _proxyObjects);
            compiledCode.Execute(scriptScope);
            
            return new PythonScriptInstance(name, scriptScope);
        }

        private void AddSearchPaths(ScriptEngine scriptEngine)
        {
            var paths = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib"),
                "/usr/lib/python2.7",
                @"C:\Python27\Lib"
            };
            
            AddSearchPaths(scriptEngine, paths);
        }

        private void AddSearchPaths(ScriptEngine scriptEngine, IEnumerable<string> paths)
        {
            var searchPaths = scriptEngine.GetSearchPaths();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    searchPaths.Add(path);
                    _logger.LogInformation($"Added Python lib path: {path}");
                }
            }

            scriptEngine.SetSearchPaths(searchPaths);
        }
    }
}
