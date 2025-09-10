using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Factories;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardEntryEditViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _originalEntry;
    private readonly PlayerViewModel? _player;

    public LeaderboardEntryViewModel? EditedEntry { get; private set; }

    public ICommand SaveCommand { get; private set; }
    public ICommand RollbackCommand { get; private set; }

    
    public LeaderboardEntryEditViewModel(LeaderboardEntry originalEntry, PlayerViewModel? player)
    {
        _originalEntry = originalEntry;
        _player = player;
        DeepCopyToEdited();

        SaveCommand = new RelayCommand(Save);
        RollbackCommand = new RelayCommand(Rollback);
    }

    private void Save()
    {
        string ign = EditedEntry?.Player?.InGameName ?? string.Empty;
        if (DialogBox.Show($"Are you sure you want to save?\nYou will apply all changes for {ign} entry\nThis will mark this entry as edited", "Rolling back changes", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        
        //TODO: 0 Tu trzeba uwzglednic statystki i chyba nie ma co ich kopiowac
        // tylko przy zapisywaniu stworzyc na nowo na bazie obecnych milestones
        
        //TODO: NA TYM SKONCZYLEM SESJE Z WIECZORU
    }

    private void Rollback()
    {
        if (DialogBox.Show("Are you sure you want to rollback?\nYou will lose all changes the changes", "Rolling back changes", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        DeepCopyToEdited();
    }

    private void DeepCopyToEdited()
    {
        var duplicatedEntry = new LeaderboardEntry()
        {
            PlayerUUID = _originalEntry.PlayerUUID,
            Points = _originalEntry.Points,
            Position = _originalEntry.Position,
            IsEdited = true,
        };

        foreach (var milestone in _originalEntry.Milestones)
        {
            EntryMilestoneData duplicatedMilestone = LeaderboardEntryMilestoneFactory.DeepCopy(milestone);
            duplicatedEntry.Milestones.Add(duplicatedMilestone);
        }
        
        EditedEntry = new LeaderboardEntryViewModel(duplicatedEntry, _player);
        EditedEntry.SetupOpeningWindow();
    }
}