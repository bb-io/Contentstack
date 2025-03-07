using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Contentstack.Actions;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Workflow;
using OpenAITests.Base;

namespace Tests.Contentstack
{
    [TestClass]
    public class EntryTests :TestBase
    {
        [TestMethod]
        public async Task GetEntry_ReturnsSuccess()
        {
            var action = new EntriesActions(InvocationContext,FileManager);
            var entryRequest = new EntryRequest
            {
                ContentTypeId = "missions",
                EntryId = "bltb0b17fd01c287e55"
            };
            var localeRequest = new LocaleRequest{   };
            var fileRequest = new FileExtensionRequest { };

            var result = await action.GetEntry(entryRequest, localeRequest, fileRequest);

            Console.WriteLine(result.Uid);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task AddEntryTag_ReturnsSuccess()
        {
            var action = new EntriesActions(InvocationContext, FileManager);
            var entryRequest = new EntryRequest
            {
                ContentTypeId = "missions",
                EntryId = "bltb0b17fd01c287e55"
            };
            var localeRequest = new LocaleRequest { };
            var fileRequest = new FileExtensionRequest { };

            //var result = await action.AddTagToEntry(entryRequest, "insights explore toolkit1", localeRequest);
            var result = await action.AddTagToEntry(entryRequest, "insights explore toolkit2", localeRequest);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task RemoveEntryTag_ReturnsSuccess()
        {
            var action = new EntriesActions(InvocationContext, FileManager);
            var entryRequest = new EntryRequest
            {
                ContentTypeId = "missions",
                EntryId = "bltb0b17fd01c287e55"
            };
            var localeRequest = new LocaleRequest { };
            var fileRequest = new FileExtensionRequest { };

            var result = await action.RemoveTagFromEntry(entryRequest, "insights explore toolkit1", localeRequest);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SearchEntries_ReturnsSuccess()
        {
            var action = new EntriesActions(InvocationContext, FileManager);
            var contenteRequest = new ContentTypeRequest { ContentTypeId = "missions", };
            var localeRequest = new LocaleRequest { };
            var workflowRequest = new WorkflowStageFilterRequest { };
            var tagFilter = new TagFilterRequest { Tag = "insights explore toolkit1" };

            var result = await action.SearchEntries(contenteRequest, workflowRequest,localeRequest, tagFilter);
            foreach (var item in result.Entries)
            {
                Console.WriteLine($"{item.Uid} - {item.Title} - {item.Tags}");
                Assert.IsNotNull(item);
            }  
        }
    }
}
