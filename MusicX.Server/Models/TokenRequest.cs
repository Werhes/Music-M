using System.Text.Json.Serialization;

public class TokenRequest
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }
}