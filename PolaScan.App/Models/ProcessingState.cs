using System.Collections.ObjectModel;

namespace PolaScan.App.Models;

public class ProcessingState
{
    public ObservableCollection<ImageWithMedia> PolaroidsWithMeta { get; set; } = new();
    public int ScanFileCount { get; set; } = 0;
    public int LocationCount { get; set; } = 0;

    public bool IsStarted { get; set; }
    public bool IsExported { get; set; }
    public bool IsWorking { get; set; }

    public string StatusMessage { get; set; }
}
