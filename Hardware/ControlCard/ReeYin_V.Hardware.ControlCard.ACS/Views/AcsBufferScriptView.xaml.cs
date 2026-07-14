using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.ACS.Views;

public partial class AcsBufferScriptView : Window
{
    public AcsBufferScriptView()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
