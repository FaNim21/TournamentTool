using OBSStudioClient.Classes;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class SceneManagerViewModel : BaseViewModel
{
    public MainViewModel MainViewModel { get; set; }

    public ICommand GetPOVsCommand { get; set; }

    private const int canvasWidth = 426;
    private const int canvasHeight = 240;


    public SceneManagerViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;

        GetPOVsCommand = new RelayCommand(GetDataFromScene);
    }


    private void GetDataFromScene()
    {
        Task.Run(GetDataFromSceneAsync);
    }
    private async Task GetDataFromSceneAsync()
    {
        if (MainViewModel.Client == null || MainViewModel.CurrentChosen == null) return;

        try
        {
            Application.Current.Dispatcher.Invoke(MainViewModel.CurrentChosen.POVs.Clear);

            var settings = await MainViewModel.Client.GetVideoSettings();
            float xAxisRatio = settings.BaseWidth / canvasWidth;
            float yAxisRatio = settings.BaseHeight / canvasHeight;

            SceneItem[] sceneItems = await MainViewModel.Client.GetSceneItemList(MainViewModel.CurrentChosen.Scene);
            foreach (var item in sceneItems)
            {
                if (item.SourceName.StartsWith("pov", StringComparison.OrdinalIgnoreCase))
                {
                    PointOfView pov = new();
                    SceneItemTransform transform = await MainViewModel.Client.GetSceneItemTransform(MainViewModel.CurrentChosen.Scene, item.SceneItemId);

                    pov.SceneName = MainViewModel.CurrentChosen.Scene;
                    pov.SceneItemName = item.SourceName;

                    pov.X = (int)(transform.PositionX / xAxisRatio);
                    pov.Y = (int)(transform.PositionY / yAxisRatio);

                    pov.Width = (int)(transform.Width / xAxisRatio);
                    pov.Height = (int)(transform.Height / yAxisRatio);

                    pov.Text = item.SourceName;

                    Application.Current.Dispatcher.Invoke(delegate { MainViewModel.AddPOV(pov); });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
            MainViewModel.Disconnect();
            return;
        }
    }

    /*private void GetDataFromScene()
    {
        if (MainViewModel.client == null || MainViewModel.CurrentChosen == null) return;

        try
        {
            Application.Current.Dispatcher.Invoke(MainViewModel.CurrentChosen.POVs.Clear);

            ObsVideoSettings settings = MainViewModel.client.GetVideoSettings();
            float xAxisRatio = settings.BaseWidth / canvasWidth;
            float yAxisRatio = settings.BaseHeight / canvasHeight;

            List<SceneItemDetails> sceneItems = MainViewModel.client.GetSceneItemList(MainViewModel.CurrentChosen.Scene);
            foreach (var item in sceneItems)
            {
                if (item.SourceName.StartsWith("pov", StringComparison.OrdinalIgnoreCase))
                {
                    PointOfView pov = new();
                    SceneItemTransformInfo response = MainViewModel.client.GetSceneItemTransform(MainViewModel.CurrentChosen.Scene, item.ItemId);

                    pov.SceneName = MainViewModel.CurrentChosen.Scene;
                    pov.SceneItemName = item.SourceName;

                    pov.X = (int)(response.X / xAxisRatio);
                    pov.Y = (int)(response.Y / yAxisRatio);

                    pov.Width = (int)(response.Width / xAxisRatio);
                    pov.Height = (int)(response.Height / yAxisRatio);

                    pov.Text = item.SourceName;

                    Application.Current.Dispatcher.Invoke(delegate { MainViewModel.AddPOV(pov); });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
            MainViewModel.Disconnect();
            return;
        }
    }*/

    /*private async Task GetDataFromSceneAsync()
    {
        if (MainViewModel.client == null || MainViewModel.CurrentChosen == null) return;

        try
        {
            await Task.Delay(1500);

            SceneItem[] sceneItems = await MainViewModel.client.GetSceneItemList(MainViewModel.CurrentChosen.Scene);
            foreach (var item in sceneItems)
            {
                if (item.SourceName.StartsWith("pov2", StringComparison.OrdinalIgnoreCase))
                {
                    PointOfView pov = new();

                    var responce = await MainViewModel.client.GetSceneItemTransform(MainViewModel.CurrentChosen.Scene, item.SceneItemId);

                    InputSettingsResponse settingsResponse = await MainViewModel.client.GetInputSettings(item.SourceName);
                    Dictionary<string, object> settings = settingsResponse.InputSettings;

                    string jsonString = JsonSerializer.Serialize(settings);
                    JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
                    JsonElement root = jsonDocument.RootElement;

                    JsonElement heightElement = root.GetProperty("height");
                    JsonElement widthElement = root.GetProperty("width");
                    JsonElement posXElement = root.GetProperty("x");
                    JsonElement posYElement = root.GetProperty("y");

                    int x, y;
                    int width, height;

                    x = int.Parse(posXElement.GetString()!);
                    y = int.Parse(posYElement.GetString()!);
                    width = widthElement.GetInt32();
                    height = heightElement.GetInt32();

                    pov.X = x;
                    pov.Y = y;

                    pov.Width = width;
                    pov.Height = height;

                    pov.Text = "ELOOOOOO";
                    //pov.X = item.

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainViewModel.AddPOV(pov);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
            MainViewModel.Disconnect();
            return;
        }
    }*/
}
