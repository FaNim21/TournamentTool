using System.Net.Http;
using System.Text.Json;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class LoadDataFromPacemanCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; set; }


    public LoadDataFromPacemanCommand(PlayerManagerViewModel playerManager)
    {
        PlayerManager = playerManager;
    }

    public override void Execute(object? parameter)
    {
        Task.Run(async () => { await LoadDataFromPaceManAsync(PlayerManager.ChosenEvent); });
    }

    private async Task LoadDataFromPaceManAsync(PaceManEvent? choosenEvent)
    {
        if (choosenEvent == null) return;

        using HttpClient client = new();

        var requestData = new { uuids = choosenEvent!.WhiteList };
        string jsonContent = JsonSerializer.Serialize(requestData);

        List<Player> eventPlayers = [];
        for (int i = 0; i < choosenEvent!.WhiteList!.Length; i++)
        {
            var current = choosenEvent!.WhiteList[i];
            Player player = new() { UUID = current };
            eventPlayers.Add(player);
        }

        List<PaceManTwitchResponse>? twitchNames = null;
        try
        {
            HttpContent content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(Consts.PaceManTwitchAPI, content);

            if (!response.IsSuccessStatusCode) return;
            string responseContent = await response.Content.ReadAsStringAsync();
            twitchNames = JsonSerializer.Deserialize<List<PaceManTwitchResponse>>(responseContent);
        }
        catch (Exception ex)
        {
            DialogBox.Show(ex.Message);
        }

        if (twitchNames == null) return;
        await UpdateWhitelist(eventPlayers, twitchNames);

        PlayerManager.SavePreset();
        DialogBox.Show("Done loading data from paceman event");
    }

    private async Task UpdateWhitelist(List<Player> eventPlayers, List<PaceManTwitchResponse> twitchNames)
    {
        for (int i = 0; i < twitchNames.Count; i++)
        {
            var current = twitchNames[i];
            if (PlayerManager.Tournament!.IsStreamNameDuplicate(current.liveAccount))
            {
                twitchNames.RemoveAt(i);
                i--;
            }
        }

        for (int i = 0; i < eventPlayers.Count; i++)
        {
            var player = eventPlayers[i];
            for (int j = 0; j < twitchNames.Count; j++)
            {
                var twitch = twitchNames[j];
                if (player.UUID == twitch.uuid)
                {
                    player.StreamData.Main = twitch.liveAccount ?? string.Empty;
                    player.PersonalBest = string.Empty;
                    await player.CompleteData();
                    player.Name = twitch.liveAccount ?? player.InGameName;
                    PlayerManager.Tournament!.AddPlayer(player);
                    break;
                }
            }
        }
    }
}
