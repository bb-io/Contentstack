﻿using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Metadata;

namespace Apps.Contentstack;

public class Application : IApplication, ICategoryProvider
{
    public IEnumerable<ApplicationCategory> Categories
    {
        get => [ApplicationCategory.Cms];
        set { }
    }
    
    public string Name
    {
        get => "Contentstack";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }
}