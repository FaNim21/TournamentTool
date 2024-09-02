﻿using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TournamentTool.Services;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;


    public App()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ControllerViewModel>();
        services.AddSingleton<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>();

        services.AddSingleton<UpdatesViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        mainViewModel.NavigationService.NavigateTo<PresetManagerViewModel>(mainViewModel.Configuration!);
        mainWindow.Show();

        InputController.Instance.Initialize();
        mainViewModel.HotkeySetup();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
