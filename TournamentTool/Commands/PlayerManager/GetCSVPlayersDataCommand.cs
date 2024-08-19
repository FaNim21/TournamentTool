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
        if (string.IsNullOrEmpty(path)) return;

        Task.Run(() => LoadFile(path));
    }

    private async Task LoadFile(string path)
    {
        using TextFieldParser parser = new(path);
        parser.HasFieldsEnclosedInQuotes = true;
        parser.SetDelimiters(",");
        parser.ReadLine();

        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();
            if (fields == null) continue;

            //TODO: 9 Zrobic dynamiczny tooltip dla headerow
            Player data = new()
            {
                Name = fields[0],
                InGameName = fields[1],
                UUID = fields[2],
                PersonalBest = fields[3]
            };
            if (fields.Length > 4) {
                data.StreamData.SetName(fields[4].ToLower().Trim());
                if (fields.Length > 5)
                    data.StreamData.SetName(fields[5].ToLower().Trim());
            }

            if (PlayerManagerViewModel.Tournament!.IsNameDuplicate(data.StreamData.Main)) continue;
            await data.CompleteData();
            PlayerManagerViewModel.Tournament!.AddPlayer(data);
        }

        PlayerManagerViewModel.SavePreset();
        DialogBox.Show("Done loading data from .csv file");
    }
}
