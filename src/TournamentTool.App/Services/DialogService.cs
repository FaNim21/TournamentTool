using Microsoft.Win32;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Modals;
using MessageBoxButton = TournamentTool.Domain.Enums.MessageBoxButton;
using MessageBoxImage = TournamentTool.Domain.Enums.MessageBoxImage;
using MessageBoxResult = TournamentTool.Domain.Enums.MessageBoxResult;

namespace TournamentTool.App.Services;

public class DialogService : IDialogService
{
    private readonly IWindowService _windowService;
    private IDispatcherService Dispatcher { get; }
    

    public DialogService(IWindowService windowService, IDispatcherService dispatcher)
    {
        _windowService = windowService;
        Dispatcher = dispatcher;
    }
    
    public MessageBoxResult Show(string text, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        var buttons = CreateButtons(button);
        DialogBoxViewModel model = new(Dispatcher)
        {
            Text = text,
            Caption = caption,
            Buttons = buttons,
            Icon = icon,
            Result = MessageBoxResult.None,
        };

        Dispatcher.Invoke(() =>
        {
            _windowService.ShowDialog(model, null, "DialogBoxWindow");
        });

        return model.Result;
    }
    
    public string ShowOpenFile(string? filter = null)
    {
        if (string.IsNullOrEmpty(filter))
        {
            filter = "All Files (*.*)|*.*";
        }
        
        OpenFileDialog openFileDialog = new() { Filter = filter, };
        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
    }
    public string ShowOpenFolder()
    {
        OpenFolderDialog openFolderDialog = new();
        return openFolderDialog.ShowDialog() == true ? openFolderDialog.FolderName : string.Empty;
    }

    private IEnumerable<DialogBoxButton> CreateButtons(MessageBoxButton buttons, params string?[]? names)
    {
        if (names == null || names.Length < 3)
        {
            var defaultNames = new string?[] { null, null, null };
            if (names is { Length: > 0 })
                Array.Copy(names, defaultNames, names.Length);

            names = defaultNames;
        }

        return buttons switch
        {
            MessageBoxButton.OK =>
            [
                new DialogBoxButton { Title = names[0] ?? "Ok", Result = MessageBoxResult.OK }
            ],
            MessageBoxButton.OKCancel =>
            [
                new DialogBoxButton { Title = names[0] ?? "Ok", Result = MessageBoxResult.OK },
                new DialogBoxButton { Title = names[1] ?? "Cancel", Result = MessageBoxResult.Cancel }
            ],
            MessageBoxButton.YesNo =>
            [
                new DialogBoxButton { Title = names[0] ?? "Yes", Result = MessageBoxResult.Yes },
                new DialogBoxButton { Title = names[1] ?? "No", Result = MessageBoxResult.No }
            ],
            MessageBoxButton.YesNoCancel =>
            [
                new DialogBoxButton { Title = names[0] ?? "Yes", Result = MessageBoxResult.Yes },
                new DialogBoxButton { Title = names[1] ?? "No", Result = MessageBoxResult.No },
                new DialogBoxButton { Title = names[2] ?? "Cancel", Result = MessageBoxResult.Cancel }
            ],
            _ => []
        };
    }
}