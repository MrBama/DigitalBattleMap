using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;
public class TokenLinkWindowViewModel : ViewModelBase
{
    public TokenLinkWindowViewModel()
    {
    }

    public TokenLinkWindowViewModel(ObservableCollection<TokenListItem> tokenList)
    {
        TokenList = tokenList;
    }

    protected override void InitializeCommands()
    {
        SelectCommand = new RelayCommand(p => SelectButton());
    }

    public bool Success { get; set; }
    public TokenListItem SelectedToken { get => Get<TokenListItem>(); set => Set(value, () => NotifyPropertyChange(nameof(IsTokenSelected))); }
    public bool IsTokenSelected { get => SelectedToken != null; }
    public ObservableCollection<TokenListItem> TokenList { get; set; } = new ObservableCollection<TokenListItem>();
    public ICommand SelectCommand { get; set; }
    
    private void SelectButton()
    {
        Success = true;
    }
}
