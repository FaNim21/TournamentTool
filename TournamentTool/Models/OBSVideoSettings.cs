using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class OBSVideoSettings : BaseViewModel
{
    private int _baseWidth;
    public int BaseWidth
    {
        get => _baseWidth;
        set
        {
            _baseWidth = value;
            OnPropertyChanged(nameof(BaseWidth));
        }
    }

    private int _baseHeight;
    public int BaseHeight
    {
        get => _baseHeight;
        set
        {
            _baseHeight = value;
            OnPropertyChanged(nameof(BaseHeight));
        }
    }

    private int _outputWidth;
    public int OutputWidth
    {
        get => _outputWidth;
        set
        {
            _outputWidth = value;
            OnPropertyChanged(nameof(OutputWidth));
        }
    }

    private int _outputHeight;
    public int OutputHeight
    {
        get => _outputHeight;
        set
        {
            _outputHeight = value;
            OnPropertyChanged(nameof(OutputHeight));
        }
    }

    private float _aspectRatio;
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = value;
            OnPropertyChanged(nameof(AspectRatio));
        }
    }
}
