using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Mount.FileSystem;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace WatchTemplate.Services
{
    public class TemplateConverter
    {
        IEngineEnvironmentSettings envSettings;

        string tempPath = Path.Combine(Path.GetTempPath(), nameof(WatchTemplate));
        string path = Program.OriginalFolder;
        string originalPath;
        string originalPathOutput;
        ITemplate originalTemplate;
        FileSystemWatcher filewatcher;

        public TemplateConverter()
        {
            Initialize();
        }

        private static DefaultTemplateEngineHost CreateHost(bool emitTimings)
        {
            var preferences = new Dictionary<string, string>
            {
                { "prefs:language", "C#" }
            };

            preferences["dotnet-cli-version"] = "5.0.0";

            var builtIns = new AssemblyComponentCatalog(new[]
            {
                typeof(RunnableProjectGenerator).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Orchestrator.RunnableProjects
                typeof(NupkgInstallUnitDescriptorFactory).GetTypeInfo().Assembly,   // for assembly: Microsoft.TemplateEngine.Edge
            });

            DefaultTemplateEngineHost host = new DefaultTemplateEngineHost("previewer", "1.0", CultureInfo.CurrentCulture.Name, preferences, builtIns, new[] { "dotnetcli" });

            if (emitTimings)
            {
                host.OnLogTiming = (label, duration, depth) =>
                {
                    string indent = string.Join("", Enumerable.Repeat("  ", depth));
                    Console.WriteLine($"{indent} {label} {duration.TotalMilliseconds}");
                };
            }


            return host;
        }

        public void Initialize()
        {
            if (!File.Exists(Path.Combine(path, ".template.config/template.json")))
            {
                throw new Exception("Not inside template folder.");
            }
            originalPath = Path.Combine(tempPath, Guid.NewGuid().ToString("N"));
            originalPathOutput = Path.Combine(tempPath, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(originalPath);
            Utils.DirectoryCopy(path, originalPath, true);


            var host = CreateHost(false);
            envSettings = new EngineEnvironmentSettings(host, (x) => new SettingsLoader(x));
            filewatcher = new FileSystemWatcher
            {
                Path = path, // Any path  
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            filewatcher.Changed += Fw_Changed;
            AllRequestData = new DiffRequestData();
            AllRequestData.LeftRequestData = new RequestData()
            {
                OutputPath = originalPathOutput,
                SourcePath = originalPath,
                All = AllRequestData,
                LeftAndNotRight = true
            };
            AllRequestData.RightRequestData = new RequestData()
            {
                OutputPath = Path.Combine(tempPath, Guid.NewGuid().ToString("N")),
                SourcePath = path,
                All = AllRequestData,
                LeftAndNotRight = false
            };
        }

        public event Action FilesChanged;

        public DiffRequestData AllRequestData { get; set; }

        private void Fw_Changed(object sender, FileSystemEventArgs e)
        {
            FilesChanged?.Invoke();
        }

        public class DiffRequestData
        {
            public RequestData LeftRequestData { get; set; }
            public RequestData RightRequestData { get; set; }
            public Dictionary<string, ParameterValue> ParametersValues { get; } = new Dictionary<string, ParameterValue>();
            public List<string> GetAllParameterNames()
            {
                var list = LeftRequestData.Template.Parameters.Select(p => p.Name)
                    .Union(RightRequestData.Template.Parameters.Select(p => p.Name))
                    .Distinct()
                    .OrderBy(k => k)
                    .ToList();
                foreach (var item in list)
                {
                    if (!ParametersValues.ContainsKey(item))
                        ParametersValues[item] = new ParameterValue() { Name = item };
                }
                return list;
            }
        }

        public class ParameterValue
        {
            private string leftValue;
            private string rightValue;

            public string GetValue(bool leftAndNotRight)
            {
                return leftAndNotRight ? LeftValue : RightValue;
            }

            public void SetValue(bool leftAndNotRight, string value)
            {
                if (leftAndNotRight)
                    LeftValue = value;
                else
                    RightValue = value;
                ValueChanged?.Invoke(value);
            }

            public event Action<string> ValueChanged;

            public bool Sync { get; set; } = true;
            public string Name { get; set; }
            public string LeftValue
            {
                get { return leftValue; }
                set
                {
                    if (leftValue == value)
                        return;
                    leftValue = value;
                    if (Sync)
                        RightValue = value;
                }
            }
            public string RightValue
            {
                get { return rightValue; }
                set
                {
                    if (rightValue == value)
                        return;
                    rightValue = value;
                    if (Sync)
                        LeftValue = value;
                }
            }
        }

        public class RequestData
        {
            public DiffRequestData All { get; init; }
            public string SourcePath { get; init; }
            public string OutputPath { get; init; }
            public ITemplate Template { get; init; }
            public bool LeftAndNotRight { get; init; }

            public Dictionary<string, string> GetParameterValues()
            {
                var dict = new Dictionary<string, string>();
                foreach (var param in Template.Parameters)
                {
                    if (All.ParametersValues.TryGetValue(param.Name, out var val))
                    {
                        if ((LeftAndNotRight ? val.LeftValue : val.RightValue) is string value)
                            dict[param.Name] = value;
                    }
                }
                return dict;
            }

            public RequestData WithTemplate(ITemplate template)
            {
                return new RequestData()
                {
                    OutputPath = OutputPath,
                    SourcePath = SourcePath,
                    Template = template,
                    All = All,
                    LeftAndNotRight = LeftAndNotRight
                };
            }
        }

        private Diff lastDiff;
        public Task<Diff> GetLastDiffAsync()
        {
            if (lastDiff == null)
                return CreateDiffAsync(null);
            return Task.FromResult(lastDiff);
        }

        public async Task<Diff> CreateDiffAsync(Diff oldDiff)
        {
            AllRequestData.LeftRequestData = await Update(AllRequestData.LeftRequestData);
            AllRequestData.RightRequestData = await Update(AllRequestData.RightRequestData);
            return lastDiff = new Diff(oldDiff, AllRequestData);
        }

        private async Task<RequestData> Update(RequestData requestData)
        {
            if (Directory.Exists(requestData.OutputPath))
                Directory.Delete(requestData.OutputPath, true);
            Directory.CreateDirectory(requestData.OutputPath);
            try
            {
                var scanner = new Scanner(envSettings);
                var result = scanner.Scan(requestData.SourcePath, true);
                var template = new WorkaroundTemplateInfo((RunnableProjectTemplate)result.Templates.Single());
                requestData = requestData.WithTemplate(template);
                var paramValues = requestData.GetParameterValues();
                if (!paramValues.TryGetValue("name", out var name))
                    name = "UniqueName";
                var creator = new TemplateCreator(envSettings);
                var instantiateResult = await creator.InstantiateAsync(template, name, "FallBackUniqueName", requestData.OutputPath, paramValues, true, false, null);
                Console.WriteLine(instantiateResult.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                File.WriteAllText(Path.Combine(requestData.OutputPath, "exception.txt"), ex.ToString());
            }
            return requestData;
        }
    }
}
