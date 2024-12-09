﻿using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using OpenAITests.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Contentstack
{
    [TestClass]
    public class DataSources : TestBase
    {
        [TestMethod]
        public async Task WorkflowStageSources()
        {
            var handler = new WorkflowStageDataHandler(InvocationContext);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            foreach (var item in data)
            {
                Console.WriteLine($"{item.Value}: {item.DisplayName}");
            }

            Assert.AreNotEqual(data.Count(), 0);
        }

        [TestMethod]
        public async Task LanguageSources()
        {
            var handler = new LanguageDataHandler(InvocationContext);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            foreach (var item in data)
            {
                Console.WriteLine($"{item.Value}: {item.DisplayName}");
            }

            Assert.AreNotEqual(data.Count(), 0);
        }

        [TestMethod]
        public async Task ContentTypeSources()
        {
            var handler = new ContentTypeDataHandler(InvocationContext);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            foreach (var item in data)
            {
                Console.WriteLine($"{item.Value}: {item.DisplayName}");
            }

            Assert.AreNotEqual(data.Count(), 0);
        }

        [TestMethod]
        public async Task AssetDataSources()
        {
            var handler = new AssetDataHandler(InvocationContext);
            var data = await handler.GetDataAsync(new DataSourceContext(), CancellationToken.None);

            foreach (var item in data)
            {
                Console.WriteLine($"{item.Value}: {item.DisplayName}");
            }

            Assert.AreNotEqual(data.Count(), 0);
        }
    }
}