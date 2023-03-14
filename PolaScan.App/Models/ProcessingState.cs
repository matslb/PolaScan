namespace PolaScan.App.Models;

public class ProcessingState
{
    public List<string> ScanFiles { get; set; } = new();
    public List<PolaroidWithMeta> PolaroidsWithMeta { get; set; } = new();
    public bool IsStarted { get; set; }

}
