using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class GetCSVPlayersDataCommand : BaseCommand
{
    public PlayerManagerViewModel PlayerManagerViewModel { get; set; }

    public GetCSVPlayersDataCommand(PlayerManagerViewModel viewModel)
    {
        PlayerManagerViewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (PlayerManagerViewModel == null) return;

        OpenFileDialog openFileDialog = new() { Filter = "All Files (*.csv)|*.csv", };
        string path = openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;

        Task.Run(() => LoadFile(path));
    }

    private async Task LoadFile(string path)
    {
        List<Player> dataList = [];
        using TextFieldParser parser = new(path);
        parser.HasFieldsEnclosedInQuotes = true;
        parser.SetDelimiters(",");
        parser.ReadLine();

        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();
            if (fields == null) continue;

            Player data = new()
            {
                Name = fields[1],
                UUID = fields[2],
                PersonalBest = fields[3],
                TwitchName = fields[4]
            };
            if (string.IsNullOrEmpty(data.TwitchName)) continue;
            if (PlayerManagerViewModel.Tournament!.IsNameDuplicate(data.TwitchName)) continue;
            await data.CompleteData();
            PlayerManagerViewModel.Tournament!.AddPlayer(data);
        }

        DialogBox.Show("Done loading data from .csv file");
    }
}
