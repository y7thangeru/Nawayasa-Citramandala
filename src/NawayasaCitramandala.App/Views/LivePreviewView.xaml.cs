using System.Windows.Controls;
using NawayasaCitramandala.ViewModels;

namespace NawayasaCitramandala.Views;

public partial class LivePreviewView : UserControl
{
    public LivePreviewView(LivePreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
