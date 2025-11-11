using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

public class TournamentState : ITournamentState
{
    public Tournament CurrentPreset { get; private set; } = new();
    public bool IsModified { get; private set; }
    public bool IsCurrentlyOpened { get; private set; }

    public event EventHandler<Tournament?>? PresetChanged;
    public event EventHandler<bool>? ModificationStateChanged;
    public event EventHandler<string>? PresetNameChanged;

    
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

    public bool IsEmpty()
    {
        return CurrentPreset == null || string.IsNullOrEmpty(CurrentPreset.Name);
    }
}