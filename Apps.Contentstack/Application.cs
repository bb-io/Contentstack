using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack;

public class Application : IApplication
{
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