using System.IO;
using System.Text.Json;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class DuplicatePresetCommand : BaseCommand
{
    public MainViewModel MainViewModel { get; set; }

    public DuplicatePresetCommand(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not Tournament tournament) return;

        string name = tournament.Name;
        name = Helper.GetUniqueName(name, name, MainViewModel.IsPresetNameUnique);
        string newPath = Path.Combine(Consts.PresetsPath, name + ".json");

        File.Copy(tournament.GetPath(), newPath);

        string text = File.ReadAllText(newPath) ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(text)) return;
            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;
            data.Name = name;

            MainViewModel.AddItem(data);
        }
        catch { }
    }
}
