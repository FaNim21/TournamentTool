using System.Windows;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.Commands.DialogBoxCommands;

class DialogBoxButtonClickCommand : BaseCommand
{
    public DialogBoxViewModel DialogBox { get; set; }

    public DialogBoxButtonClickCommand(DialogBoxViewModel DialogBox)
    {
        this.DialogBox = DialogBox;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;

        MessageBoxResult variable = (MessageBoxResult)parameter;
        string output = variable.ToString().ToLower();
        if (string.IsNullOrEmpty(output)) return;

        DialogBox.Result = output switch
        {
            "ok" => MessageBoxResult.OK,
            "yes" => MessageBoxResult.Yes,
            "no" => MessageBoxResult.No,
            "cancel" => MessageBoxResult.Cancel,
            _ => MessageBoxResult.None,
        };

        DialogBox.Close();
    }
}
