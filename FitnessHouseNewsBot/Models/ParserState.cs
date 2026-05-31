namespace FitnessHouseNewsBot.Models;

public class ParserState
{
    public event Action? OnChange;

    private bool _isRunning;

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            NotifyStateChanged();
        }
    }

    private DateTime? _lastRun;

    public DateTime? LastRun
    {
        get => _lastRun;
        set
        {
            _lastRun = value;
            NotifyStateChanged();
        }
    }

    private string _lastStatus = "-";

    public string LastStatus
    {
        get => _lastStatus;
        set
        {
            _lastStatus = value;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}