namespace CorrigeTesCours.Api.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "meta-llama/llama-3.3-70b-instruct";
    public int TimeoutSeconds { get; set; } = 30;
}
