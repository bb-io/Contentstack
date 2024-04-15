using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Models.Response;

public class ErrorResponse
{
    public string ErrorMessage { get; set; }
    public JObject? Errors { get; set; }
}