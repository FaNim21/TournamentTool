using TournamentTool.ViewModels;

namespace TournamentTool.Commands;

class UpdateViewCommand : BaseCommand
{
    private readonly MainViewModel viewModel;

    public UpdateViewCommand(MainViewModel viewModel) { this.viewModel = viewModel; }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;

        /*string result = parameter?.ToString() ?? "";
        result = result.ToLower() + "viewmodel";

        if (result.Equals(viewModel.SelectedViewModel?.GetType().Name.ToLower())) return;

        viewModel.SelectedViewModel?.OnDisable();

        for (int i = 0; i < viewModel.baseViewModels.Count; i++)
        {
            var current = viewModel.baseViewModels[i];

            if (current.GetType().Name.ToLower().Equals(result))
            {
                viewModel.SelectedViewModel = current;
                break;
            }
        }

        viewModel.SelectedViewModel?.OnEnable();*/
    }
}
