using System.IO;
using System.Text.Json;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class SavePresetCommand : BaseCommand
{
    public PresetManagerViewModel MainViewModel { get; set; }

    private readonly JsonSerializerOptions serializerOptions;


    public SavePresetCommand(PresetManagerViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        serializerOptions = new JsonSerializerOptions() { WriteIndented = true };
    }

    public override void Execute(object? parameter)
    {
        if (MainViewModel.CurrentChosen == null) return;

        var data = JsonSerializer.Serialize<object>(MainViewModel.CurrentChosen, serializerOptions);

        string path = MainViewModel.CurrentChosen.GetPath();
        File.WriteAllText(path, data);

        MainViewModel.IsCurrentPresetSaved = true;
    }
}
