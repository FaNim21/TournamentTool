using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentState
{
    Tournament CurrentPreset { get; }
    bool IsModified { get; }
    bool IsCurrentlyOpened { get; }

    event EventHandler<Tournament?>? PresetChanged;
    event EventHandler<bool>? ModificationStateChanged;
    event EventHandler<string>? PresetNameChanged;

    void MarkAsModified();
    void MarkAsUnmodified();

    void ChangePreset(Tournament? newPreset);
    void DeletePreset();
    
    void ChangeName(string newName);
}

public class TournamentState : ITournamentState
{
    private readonly IDialogService _dialogService;
    
    public Tournament CurrentPreset { get; private set; } = new();
    public bool IsModified { get; private set; }
    public bool IsCurrentlyOpened { get; private set; }

    public event EventHandler<Tournament?>? PresetChanged;
    public event EventHandler<bool>? ModificationStateChanged;
    public event EventHandler<string>? PresetNameChanged;


    public TournamentState(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    
    public void MarkAsModified()
    {
        if (IsModified) return;
        
        IsModified = true;
        ModificationStateChanged?.Invoke(this, true);
    }
    public void MarkAsUnmodified()
    {
        if (!IsModified) return;
        
        IsModified = false;
        ModificationStateChanged?.Invoke(this, false);
    }

    public void ChangePreset(Tournament? newPreset)
    {
        if (newPreset == null)
        {
            DeletePreset();
            return;
        }
        
        CurrentPreset = newPreset;
        IsCurrentlyOpened = true;
        MarkAsUnmodified();
        PresetChanged?.Invoke(this, newPreset);
    }
    public void DeletePreset()
    {
        IsCurrentlyOpened = false;
        CurrentPreset = new Tournament();
        PresetChanged?.Invoke(this, null);
    }
    
    public void ChangeName(string newName)
    {
        if (!IsCurrentlyOpened) return;
        
        CurrentPreset.Name = newName;
        PresetNameChanged?.Invoke(this, newName);
    }
}