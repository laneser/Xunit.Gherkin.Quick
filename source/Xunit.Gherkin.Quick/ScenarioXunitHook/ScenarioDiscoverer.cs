﻿using Gherkin;
using Gherkin.Ast;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.Gherkin.Quick
{
    internal sealed class ScenarioDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _messageSink;

        public ScenarioDiscoverer(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, 
            IAttributeInfo factAttribute)
        {
            var gherkinDocument = GetGherkinDocumentByType(testMethod.TestClass.Class.ToRuntimeType());

            var featureTags = gherkinDocument.Feature.Tags?.ToList();
            foreach (var scenario in gherkinDocument.Feature.Children)
            {
                var scenarioWithTags = scenario as global::Gherkin.Ast.Scenario;
                var tags = featureTags.ToList().Union(scenarioWithTags?.Tags ?? new List<Tag>()).Select(t => t.Name.StartsWith("@") ? t.Name.Substring(1) : t.Name).Distinct();
                
                yield return new ScenarioXunitTestCase(_messageSink, testMethod, gherkinDocument.Feature.Name, scenario.Name, tags, new object[] { scenario.Name });
            }
        }

        public static GherkinDocument GetGherkinDocumentByType(Type classType)
        {
            var fileName = classType.FullName;
            fileName = fileName.Substring(fileName.LastIndexOf('.') + 1) + ".feature";

            if (!File.Exists(fileName))
            {
                var path = (classType.GetTypeInfo().GetCustomAttributes(typeof(FeatureFileAttribute))
                    .FirstOrDefault() as FeatureFileAttribute)
                    ?.Path;

                if (path == null || !File.Exists(path))
                {
                    throw new TypeLoadException($"Cannot find feature file `{fileName}` in the output root directory. If it's somewhere else, use {nameof(FeatureFileAttribute)} to specify file path.");
                }

                fileName = path;
            }

            var parser = new Parser();
            using (var gherkinFile = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var gherkinReader = new StreamReader(gherkinFile))
                {
                    var gherkinDocument = parser.Parse(gherkinReader);
                    return gherkinDocument;
                }
            }
        }
    }
}
