using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WatchTemplate.Services
{
    public class ParameterSet : IParameterSet
    {
        private readonly IDictionary<string, ITemplateParameter> _parameters = new Dictionary<string, ITemplateParameter>(StringComparer.OrdinalIgnoreCase);

        public ParameterSet(IRunnableProjectConfig config)
        {
            foreach (var p in config.Parameters)
            {
                p.Value.Name = p.Key;
                _parameters[p.Key] = p.Value;
            }
            ResolvedValues = new Dictionary<ITemplateParameter, object>(_parameters.Values.Select(p => new KeyValuePair<ITemplateParameter, object>(p, p.DefaultValue)));
        }

        public IEnumerable<ITemplateParameter> ParameterDefinitions => _parameters.Values;

        public IDictionary<ITemplateParameter, object> ResolvedValues { get; }

        public IEnumerable<string> RequiredBrokerCapabilities => Enumerable.Empty<string>();

        public void AddParameter(ITemplateParameter param)
        {
            _parameters[param.Name] = param;
        }

        public bool TryGetParameterDefinition(string name, out ITemplateParameter parameter)
        {
            if (_parameters.TryGetValue(name, out parameter))
            {
                return true;
            }

            parameter = new Parameter
            {
                Name = name,
                Requirement = TemplateParameterPriority.Optional,
                IsVariable = true,
                Type = "string"
            };

            return true;
        }
    }
}