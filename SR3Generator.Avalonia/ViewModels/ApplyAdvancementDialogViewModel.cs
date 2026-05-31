namespace SR3Generator.Avalonia.ViewModels;

public class ApplyAdvancementDialogViewModel : ViewModelBase
{
    public string Summary { get; }
    public int TotalKarma { get; }
    public int RemainingAfter { get; }

    public string TotalKarmaDisplay => $"{TotalKarma} karma";
    public string RemainingAfterDisplay => $"{RemainingAfter} karma remaining after";

    public ApplyAdvancementDialogViewModel(string summary, int totalKarma, int remainingAfter)
    {
        Summary = summary;
        TotalKarma = totalKarma;
        RemainingAfter = remainingAfter;
    }

    public ApplyAdvancementDialogViewModel() : this("", 0, 0) { }
}
