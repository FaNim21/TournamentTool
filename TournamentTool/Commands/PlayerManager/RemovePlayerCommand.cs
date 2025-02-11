﻿using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class RemovePlayerCommand : BaseCommand
{
    private readonly PlayerManagerViewModel _playerManager;
    private readonly IPresetSaver _presetSaver;

    
    public RemovePlayerCommand(PlayerManagerViewModel playerManager, IPresetSaver presetSaver)
    {
        _playerManager = playerManager;
        _presetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not Player player) return;

        _playerManager.Remove(player);
        _presetSaver.SavePreset();
    }
}
