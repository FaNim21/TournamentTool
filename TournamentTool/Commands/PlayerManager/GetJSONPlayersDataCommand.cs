using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public struct JSONPlayer
{
    [JsonPropertyName("ign")]
    public string IGN { get; set; }

    [JsonPropertyName("twitch_username")]
    public string Twitch { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }
}

public class GetJSONPlayersDataCommand : BaseCommand
{
    public PlayerManagerViewModel PlayerManagerViewModel { get; set; }

    public GetJSONPlayersDataCommand(PlayerManagerViewModel viewModel)
    {
        PlayerManagerViewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (PlayerManagerViewModel == null) return;

        OpenFileDialog openFileDialog = new() { Filter = "All Files (*.json)|*.json", };
        string path = openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        if (string.IsNullOrEmpty(path)) return;

        Task.Run(() => LoadFile(path));
    }

    private async Task LoadFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            JSONPlayer[] players = JsonSerializer.Deserialize<JSONPlayer[]>(json)!;
            if (players.Length == 0) return;

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];

                Player data = new()
                {
                    Name = player.DisplayName.Trim(),
                    InGameName = player.IGN.Trim(),
                };
                data.StreamData.SetName(player.Twitch.Trim());

                if (PlayerManagerViewModel.Tournament!.IsNameDuplicate(data.StreamData.Main)) continue;
                await data.UpdateHeadImage();
                PlayerManagerViewModel.Tournament!.AddPlayer(data);
            }

            PlayerManagerViewModel.SavePreset();
            DialogBox.Show("Done loading data from .json file");
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error loading json data: {ex.Message}", "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
