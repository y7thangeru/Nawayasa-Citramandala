using System.Windows.Controls;
using NawayasaCitramandala.ViewModels;

namespace NawayasaCitramandala.Views;

public partial class HomeView : UserControl
{
    public HomeView(HomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
