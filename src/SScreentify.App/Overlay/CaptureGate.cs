namespace SScreentify.App.Overlay;

/// <summary>Allows a single open selector; repeated hotkey presses while one is open are ignored.</summary>
public sealed class CaptureGate
{
    private bool _open;

    public bool TryEnter()
    {
        if (_open) return false;
        _open = true;
        return true;
    }

    public void Exit() => _open = false;
}
