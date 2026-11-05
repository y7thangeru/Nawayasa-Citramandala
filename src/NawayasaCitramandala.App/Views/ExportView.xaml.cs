using System.Windows.Controls;
using NawayasaCitramandala.ViewModels;

namespace NawayasaCitramandala.Views;

public partial class ExportView : UserControl
{
    public ExportView(ExportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
