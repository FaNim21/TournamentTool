using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Obs;

public class TransformViewModel : BaseViewModel
{
    public int OriginWidth { get; private set; }
    public int OriginHeight { get; private set; }
    public int OriginX { get; private set; }
    public int OriginY { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    
    
    public TransformViewModel(IDispatcherService dispatcher) : base(dispatcher) { }

    public void Initialize(SceneItemTransformStub itemTransform, SceneItemTransformStub? groupTransform)
    {
        double positionX = itemTransform.PositionX ?? 0d;
        double positionY = itemTransform.PositionY ?? 0d;

        double width = itemTransform.Width ?? 0d;
        double height = itemTransform.Height ?? 0d;

        if (groupTransform != null)
        {
            positionX *= groupTransform.ScaleX ?? 0d;
            positionY *= groupTransform.ScaleY! ?? 0d;

            positionX += groupTransform.PositionX ?? 0d;
            positionY += groupTransform.PositionY ?? 0d;

            width *= groupTransform.ScaleX ?? 0d;
            height *= groupTransform.ScaleY ?? 0d;
        }

        OriginX = (int)positionX;
        OriginY = (int)positionY;
        OriginWidth = (int)width;
        OriginHeight = (int)height;
    }
    
    public void UpdateProportions(float proportion)
    {
        X = (int)(OriginX / proportion);
        Y = (int)(OriginY / proportion);

        Width = (int)(OriginWidth / proportion);
        Height = (int)(OriginHeight / proportion);

        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));

        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }
}