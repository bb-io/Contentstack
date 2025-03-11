using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.ContentType
{
    public  class ContentTypesRequest
    {
        [Display("Content type ID")]
        [DataSource(typeof(ContentTypeDataHandler))]
        public IEnumerable<string>? ContentTypeId { get; set; }
    }
}
