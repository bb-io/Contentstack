using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Entities;
public class UserEntity
{
    [JsonProperty("uid")]
    public string Id { get; set; }
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }
}
