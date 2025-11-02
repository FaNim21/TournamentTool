using System.Text.Json;
using NSubstitute;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Tests.Selectable;

public class PresetManagerViewModelTests
{
    private readonly PresetManagerViewModel _presetManager;

    private readonly IPresetSaver _presetService;
    private readonly ICoordinator _coordinator;
    private readonly IBackgroundCoordinator _backgroundCoordinator;
    private readonly ISettings _settingsService;
    private readonly ILuaScriptsManager _luaScriptManager;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IUIInteractionService _uiInteraction;
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentPlayerRepository _playerRepository;

    
    public PresetManagerViewModelTests()
    {
        Consts.IsTesting = true;

        Directory.CreateDirectory(Consts.PresetsPath);
        
        _presetService = Substitute.For<IPresetSaver>();
        _coordinator = Substitute.For<ICoordinator>();
        _backgroundCoordinator = Substitute.For<IBackgroundCoordinator>();
        _settingsService = Substitute.For<ISettings>();
        _luaScriptManager = Substitute.For<ILuaScriptsManager>();
        var dispatcher = Substitute.For<IDispatcherService>();
        _navigationService = Substitute.For<INavigationService>();
        _dialogService = Substitute.For<IDialogService>();
        _uiInteraction = Substitute.For<IUIInteractionService>();
        _tournamentState = Substitute.For<ITournamentState>();
        _playerRepository = Substitute.For<ITournamentPlayerRepository>();

        _presetManager = new PresetManagerViewModel(_coordinator, _presetService, _tournamentState, _playerRepository, _backgroundCoordinator,
            Substitute.For<ILoggingService>(), _settingsService, _luaScriptManager, dispatcher, _navigationService, _dialogService, _uiInteraction);
        
        _presetManager.Presets.Clear();
    }


    [Fact]
    public void AddItem_AddsPresetToCollection()
    {
        //Arrange
        var preset = new TournamentPreset("TestPreset");
        
        //Act
        _presetManager.AddNewItem(preset);

        //Assert
        Assert.Single(_presetManager.Presets);
        _presetService.Received(1).SavePreset(Arg.Any<IPreset>());
    }

    [Fact]
    public void RemoveItem_ByNameArgument_RemovesPresetFromCollection()
    {
        //Arrange
        var preset = new TournamentPreset("TestPresetRemoveByName");
        _tournamentState.CurrentPreset.Returns(new Tournament {Name = "TestPresetRemoveByName"});
        
        //Act
        _presetManager.AddNewItem(preset);
        _presetManager.RemoveItem(preset.Name);
        
        //Assert
        _tournamentState.Received(1).DeletePreset();
        Assert.Empty(_presetManager.Presets);
    }
    
    [Fact]
    public void RemoveItem_ByItemArgument_RemovesPresetFromCollection()
    {
        //Arrange
        var preset = new TournamentPreset("TestPresetRemoveByItemViewModel");
        _tournamentState.CurrentPreset.Returns(new Tournament {Name = "TestPresetRemoveByItemViewModel"});
        
        //Act
        var item = _presetManager.AddNewItem(preset);
        _presetManager.RemoveItem(item);
        
        //Assert
        _tournamentState.Received(1).DeletePreset();
        Assert.Empty(_presetManager.Presets);
    }

    [Fact]
    public void IsPresetNameUnique_ReturnsTrueForUniqueName()
    {
        //Arrange
        var preset1 = new TournamentPreset("Preset1");
        var preset2 = new TournamentPreset("NameUnique");
        
        //Act
        _presetManager.AddNewItem(preset1);
        _presetManager.AddNewItem(preset2);
        
        //Assert
        Assert.True(_presetManager.IsPresetNameUnique("UniqueName"));
        Assert.Equal(2, _presetManager.Presets.Count);
    }

    [Fact]
    public void IsPresetNameUnique_ReturnsFalseForDuplicateName()
    {
        //Arrange
        var preset = new TournamentPreset("Duplicate");
        
        //Act
        _presetManager.AddNewItem(preset);

        //Assert
        Assert.False(_presetManager.IsPresetNameUnique("Duplicate"));
        Assert.Single(_presetManager.Presets);
    }

    [Fact]
    public void CurrentChosen_SetsCurrentlyOpened_InTournamentState()
    {
        //Arrange
        bool isCurrentlyOpened = false;
        var preset = new TournamentPreset("Preset1");
        var presetViewModel = _presetManager.AddNewItem(preset);
        
        string path = presetViewModel.GetPath();
        var data = JsonSerializer.Serialize<object>(presetViewModel);
        File.WriteAllText(path, data);
        
        _tournamentState.When(x => x.ChangePreset(Arg.Any<Tournament>())).Do(callInfo =>
        {
            var arg = callInfo.Arg<Tournament>();
            if (arg != null) isCurrentlyOpened = true;
        });

        //Act
        _presetManager.CurrentChosen = presetViewModel;

        //Assert
        Assert.True(isCurrentlyOpened);
        Assert.Single(_presetManager.Presets);
    }

    [Fact]
    public void CurrentChosen_ShouldNotOpenAny_WhenCurrentIsSetToNull()
    {
        //Arrange
        bool isNotOpened = true;
        _tournamentState.When(x => x.ChangePreset(Arg.Any<Tournament>())).Do(callInfo =>
        {
            var arg = callInfo.Arg<Tournament>();
            if (arg != null) isNotOpened = true;
        });

        //Act
        _presetManager.CurrentChosen = null;

        //Assert
        Assert.True(isNotOpened);
        Assert.Empty(_presetManager.Presets);
    }

    [Fact]
    public void Clear_ShouldNotClear_WhenUserClickNo()
    {
        // Arrange
        _dialogService.Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>()).Returns(MessageBoxResult.No);
        _tournamentState.CurrentPreset.Returns(new Tournament { Name = "Clearing test"});

        // Act
        _presetManager.Clear();

        // Assert
        _tournamentState.DidNotReceive().MarkAsModified();
    }
    [Fact]
    public void Clear_ShouldClearAndMarkModified_WhenUserClicksYes()
    {
        // Arrange
        _dialogService.Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>()).Returns(MessageBoxResult.Yes);
        _tournamentState.CurrentPreset.Returns(new Tournament { Name = "Clearing test"});

        // Act
        _presetManager.Clear();

        // Assert
        _tournamentState.Received(1).MarkAsModified();
    }
    
    public class CommandsTests
    {
        private readonly PresetManagerViewModel _presetManager;
   
        private readonly IPresetSaver _presetService;
        private readonly ICoordinator _coordinator;
        private readonly IBackgroundCoordinator _backgroundCoordinator;
        private readonly ISettings _settingsService;
        private readonly ILuaScriptsManager _luaScriptManager;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IUIInteractionService _uiInteraction;
        private readonly ITournamentState _tournamentState;
        private readonly ITournamentPlayerRepository _playerRepository;

    
        public CommandsTests()
        {
            Consts.IsTesting = true;
        
            Directory.CreateDirectory(Consts.PresetsPath);
            
            _presetService = Substitute.For<IPresetSaver>();
            _coordinator = Substitute.For<ICoordinator>();
            _backgroundCoordinator = Substitute.For<IBackgroundCoordinator>();
            _settingsService = Substitute.For<ISettings>();
            _luaScriptManager = Substitute.For<ILuaScriptsManager>();
            var dispatcher = Substitute.For<IDispatcherService>();
            _navigationService = Substitute.For<INavigationService>();
            _dialogService = Substitute.For<IDialogService>();
            _uiInteraction = Substitute.For<IUIInteractionService>();
            _tournamentState = Substitute.For<ITournamentState>();
            _playerRepository = Substitute.For<ITournamentPlayerRepository>();

            _presetManager = new PresetManagerViewModel(_coordinator, _presetService, _tournamentState, _playerRepository, _backgroundCoordinator,
                Substitute.For<ILoggingService>(), _settingsService, _luaScriptManager, dispatcher, _navigationService, _dialogService, _uiInteraction);
            
            _presetManager.Presets.Clear();
        }

        [Fact]
        public void SavePresetCommand_SavesPresetCorrectly()
        {
            //Arrange & Act
            _presetManager.SavePresetCommand.Execute(null);
            
            //Assert
            _presetService.Received(1).SavePreset();
        }

        [Fact]
        public void OpenControllerCommand_ChangesToCorrectViewModel()
        {
            //Arrange & Act
            _presetManager.OpenControllerCommand.Execute(null);
            
            //Assert
            _navigationService.Received(1).NavigateTo<ControllerViewModel>();
        }

        [Fact]
        public void OpenLeaderboardCommand_ChangesToCorrectViewModel()
        {
            //Arrange & Act
            _presetManager.OpenLeaderboardCommand.Execute(null);
            
            //Assert
            _navigationService.Received(1).NavigateTo<LeaderboardPanelViewModel>();
        }

        [Fact]
        public void AddNewPresetCommand_CorrectlyAddsPreset()
        {
            //Arrange & Act
            _presetManager.AddNewPresetCommand.Execute(null);

            //Assert
            Assert.Contains(_presetManager.Presets, p => p.Name.Equals("New Preset"));
            Assert.Equal("New Preset", _presetManager.Presets.Last().Name);
        }

        [Fact]
        public void ClearCurrentPresetCommand_ClearingAllVariables()
        {
            //Arrange
            /*Tournament tournament = new Tournament
            {
                Name = "_ClearTest",
                
                IsUsingTeamNames = true,
                IsUsingWhitelistOnPaceMan = false,
                
                SceneCollection = "asdf",
                
                SetPovPBText = true,
                SetPovHeadsInBrowser = true,
                DisplayedNameType = DisplayedNameType.WhiteList,
                
                IsUsingTwitchAPI = true,
                ShowStreamCategory = false,
                
                PaceManRefreshRateMiliseconds = 10000,
                
                Structure2GoodPaceMiliseconds = 20,
                FirstPortalGoodPaceMiliseconds = 20,
                EnterEndGoodPaceMiliseconds = 20,
                CreditsGoodPaceMiliseconds = 20,
                EnterStrongholdGoodPaceMiliseconds = 20,
                
                ControllerMode = ControllerMode.Ranked,
                
                RankedApiKey = "asdf",
                RankedApiPlayerName = "asdf"
            };

            _mockTournamentViewModel.Object.ChangeData(tournament);
            _viewModel.SavePresetCommand.Execute(null);
            
            _mockTournamentViewModel.Object.Clear();
            var tournamentCleared = _mockTournamentViewModel.Object.GetData();
            
            Assert.False(tournamentCleared.IsUsingTeamNames);
            Assert.True(tournamentCleared.IsUsingWhitelistOnPaceMan);
    
            Assert.Equal(string.Empty, tournamentCleared.SceneCollection);

            Assert.False(tournamentCleared.SetPovHeadsInBrowser);
            Assert.False(tournamentCleared.SetPovPBText);
            Assert.Equal(DisplayedNameType.None, tournamentCleared.DisplayedNameType);

            Assert.False(tournamentCleared.IsUsingTwitchAPI);
            Assert.True(tournamentCleared.ShowStreamCategory);

            Assert.Equal(3000, tournamentCleared.PaceManRefreshRateMiliseconds);

            Assert.Equal(270000, tournamentCleared.Structure2GoodPaceMiliseconds);
            Assert.Equal(360000, tournamentCleared.FirstPortalGoodPaceMiliseconds);
            Assert.Equal(450000, tournamentCleared.EnterStrongholdGoodPaceMiliseconds);
            Assert.Equal(480000, tournamentCleared.EnterEndGoodPaceMiliseconds);
            Assert.Equal(600000, tournamentCleared.CreditsGoodPaceMiliseconds);

            Assert.Equal(ControllerMode.None, tournamentCleared.ControllerMode);

            Assert.Equal(string.Empty, tournamentCleared.RankedApiKey);
            Assert.Equal(string.Empty, tournamentCleared.RankedApiPlayerName);*/
        }

        [Fact]
        public void DuplicateCurrentPresetCommand_CorrectlyDuplicates()
        {
            //Arrange
            var preset = new TournamentPreset("DuplicateTest");
            string expectedName = $"{preset.Name} (1)";
            
            File.Delete(Path.Combine(Consts.PresetsPath, $"{preset.Name}.json"));
            File.Delete(Path.Combine(Consts.PresetsPath, $"{expectedName}.json"));
            
            var item = _presetManager.AddNewItem(preset);
        
            string path = item.GetPath();
            var data = JsonSerializer.Serialize<object>(item);
            File.WriteAllText(path, data);
            
            //Act
            _presetManager.DuplicateCurrentPresetCommand.Execute(item);
            
            //Assert
            _presetService.Received(2).SavePreset(Arg.Any<IPreset>());
            Assert.Equal(expectedName, _presetManager.Presets[^1].Name);
        }

        [Fact]
        public void RenameItemCommand_CorrectlyRenaming()
        {
            //Arrange & Act
            _presetManager.RenameItemCommand.Execute(null);
            
            //Assert
            _presetService.Received(1).SavePreset();
            _uiInteraction.Received(1).EnterEditModeOnHoverTextBlock();
        }

        [Fact]
        public void RemoveCurrentPresetCommand_CorrectlyRemovesPreset()
        {
            //Arrange
            _dialogService.Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>()).Returns(MessageBoxResult.Yes);
            _tournamentState.CurrentPreset.Returns(new Tournament { Name = "Other preset"});
            var preset = new TournamentPreset("Remove command test");
        
            //Act
            var item = _presetManager.AddNewItem(preset);
            _presetManager.RemoveCurrentPresetCommand.Execute(item);
        
            //Assert
            _tournamentState.Received(0).DeletePreset();
            Assert.Empty(_presetManager.Presets);
        }
    }
}