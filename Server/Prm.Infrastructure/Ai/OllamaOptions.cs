namespace Prm.Infrastructure.Ai;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "gemma3:12b-it-q8_0";

    public int TimeoutSeconds { get; set; } = 120;
}
