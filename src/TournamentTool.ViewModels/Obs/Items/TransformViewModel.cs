using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Presentation.Obs.Entities;

namespace TournamentTool.ViewModels.Obs.Items;

public class TransformViewModel : BaseViewModel
{
    private readonly Transform _transform;

    public int OriginWidth => _transform.OriginWidth;
    public int OriginHeight => _transform.OriginHeight;
    public int OriginX => _transform.OriginX;
    public int OriginY => _transform.OriginY;

    private int _width;
    public int Width
    {
        get => _width; 
        private set => SetField(ref _width, value);
    }
    
    private int _height;
    public int Height 
    {
        get => _height; 
        private set => SetField(ref _height, value);    
    }    
    
    private int _x;
    public int X
    {
        get => _x;
        private set => SetField(ref _x, value);
    }

    private int _y;
    public int Y 
    {
        get => _y;
        private set => SetField(ref _y, value);
    }


    public TransformViewModel(Transform transform, IDispatcherService dispatcher) : base(dispatcher)
    {
        _transform = transform;
    }
    
    public void UpdateProportions(float proportion)
    {
        X = (int)(OriginX / proportion);
        Y = (int)(OriginY / proportion);

        Width = (int)(OriginWidth / proportion);
        Height = (int)(OriginHeight / proportion);
    }
}