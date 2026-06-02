using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SymphoniaLegato.Core.Models;

namespace SymphoniaLegato.Desktop.ViewModels;

public sealed partial class ScorePropertiesViewModel : ViewModelBase
{
    private Score? _score;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _subtitle = string.Empty;
    [ObservableProperty] private string _composer = string.Empty;
    [ObservableProperty] private string _lyricist = string.Empty;
    [ObservableProperty] private string _arranger = string.Empty;
    [ObservableProperty] private string _copyright = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private int _tempo = 120;
    [ObservableProperty] private int _timeNumerator = 4;
    [ObservableProperty] private int _timeDenominator = 4;
    [ObservableProperty] private int _selectedPageSizeIndex = 2; // A4 default
    [ObservableProperty] private bool _isModified;

    public static IReadOnlyList<string> PageSizeOptions { get; } =
        ["US Letter", "US Legal", "A4 (ISO)", "A3 (ISO)", "Custom"];

    public event EventHandler<bool>? CloseRequested;

    public void LoadFrom(Score score)
    {
        _score = score;

        Title        = score.Title;
        Subtitle     = score.Subtitle;
        Composer     = score.Composer;
        Lyricist     = score.Lyricist;
        Arranger     = score.Arranger;
        Copyright    = score.Copyright;
        Notes        = score.Notes;
        Tempo        = score.InitialTempo;
        TimeNumerator   = score.InitialTimeSignature.Numerator;
        TimeDenominator = score.InitialTimeSignature.Denominator;
        SelectedPageSizeIndex = score.PageSize switch
        {
            PageSize.Letter => 0,
            PageSize.Legal  => 1,
            PageSize.A4     => 2,
            PageSize.A3     => 3,
            _               => 4
        };
        IsModified = false;
    }

    [RelayCommand]
    private void Apply()
    {
        CommitToScore();
        IsModified = false;
    }

    [RelayCommand]
    private void Ok()
    {
        CommitToScore();
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private void CommitToScore()
    {
        if (_score is null) return;

        _score.Title     = Title;
        _score.Subtitle  = Subtitle;
        _score.Composer  = Composer;
        _score.Lyricist  = Lyricist;
        _score.Arranger  = Arranger;
        _score.Copyright = Copyright;
        _score.Notes     = Notes;
        _score.InitialTempo = Tempo;
        _score.InitialTimeSignature = new SymphoniaLegato.Core.Models.TimeSignature(TimeNumerator, TimeDenominator);
        _score.PageSize = SelectedPageSizeIndex switch
        {
            0 => PageSize.Letter,
            1 => PageSize.Legal,
            2 => PageSize.A4,
            3 => PageSize.A3,
            _ => PageSize.Custom
        };
        _score.ModifiedAt = DateTime.UtcNow;
    }

    partial void OnTitleChanged(string value) => IsModified = true;
    partial void OnComposerChanged(string value) => IsModified = true;
    partial void OnTempoChanged(int value) => IsModified = true;
}
