namespace Noats.Models;

public class AppState
{
    public List<NoatState> Noats { get; set; } = [];
    public DateTime LastSaved { get; set; }
    public int Version { get; set; } = 1;
}
