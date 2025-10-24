using TournamentTool.Domain.Entities;

namespace TournamentTool.Core.Interfaces;

public interface IPlayerViewModel
{
    Guid Id { get; }
    Player Data { get; }

    string UUID { get; }
    string Name { get; }
    string InGameName { get; set; }

    void UpdateHeadImage();
    void SetStreamName(string name);
    
    void ShowCategory(bool option);
    void ShowTeamName(bool option);

    void UpdateData(IPlayerViewModel dataToUpdate);
    
    Task CompleteData(bool completeUUID = true);

    Task<ResponseMojangProfileAPI?> GetDataFromUUID();
    Task<ResponseMojangProfileAPI?> GetDataFromInGameName();

    Task UpdateHeadImageAsync();
    Task ForceUpdateHeadImage();
    
    void ClearFromController();
    void ClearStreamData();
    void ClearPOVDependencies();

    // \/ do reworku w jeden equal? i zwracanie przez out string?
    bool EqualsNoDialog(Player player);
    bool Equals(Player player);
}