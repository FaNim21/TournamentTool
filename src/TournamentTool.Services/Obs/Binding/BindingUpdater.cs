namespace TournamentTool.Services.Obs.Binding;

public interface IBindingDataObtainer
{
    void Obtain(string field);
}

public interface IBindingUpdater
{
    void Update(IBindingDataObtainer dataObtainer);
}

public class BindingUpdater : IBindingUpdater
{
    private readonly IBindingEngine _bindingEngine;
    
    //TODO: 0 Trzeba obowiazkowo zrobic publikowanie zmian do bindingu na bazie modeli
    //TODO: 0 Bez tego nie da sie sensowanie zcentralizowac wymuszenia aktualizacji przy zmianie bindingu
    //TODO: -1 Wiec trzeba wymyslec uniwersalne rozwiazanie oparte o generic interface z danymi w modelach pod dictionary
    //         z opcja cache'owania statycznie properties po to zeby tez przy rejestrowaniu bindingow

    
    public BindingUpdater(IBindingEngine bindingEngine)
    {
        _bindingEngine = bindingEngine;
    }
    
    public void Update(IBindingDataObtainer dataObtainer)
    {
        // nic mi z tego obtainer'a jak i tak musze miec opcje zzakautlizowac wszystkie bindingi bez wyboru, 
        
        // A MOZE ZROBIC TAK: Dac opcje wrzucenia dowolnej klasy typu obtainer do bindingNode, zeby wtedy wymusic update na node'ie zeby 
    }
}