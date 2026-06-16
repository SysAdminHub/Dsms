namespace Dsms.Web.Services.Feedback;

/// <summary>
/// Scoped UI-State zwischen Sidebar-Button und globalem Feedback-Modal im MainLayout.
/// </summary>
public sealed class FeedbackModalState
{
    public bool IsOpen { get; private set; }
    public bool Cooldown { get; private set; }

    public event Action? Changed;

    public void Open()
    {
        IsOpen = true;
        Notify();
    }

    public void Close()
    {
        IsOpen = false;
        Notify();
    }

    public void StartCooldown()
    {
        Cooldown = true;
        Notify();
    }

    public void EndCooldown()
    {
        Cooldown = false;
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
