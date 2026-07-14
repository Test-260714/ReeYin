using ReeYin_V.Hardware.ControlCard.ACS.ViewModels;
using System;
using System.Windows;

namespace ReeYin_V.Hardware.ControlCard.ACS.Views;

/// <summary>
/// ACS controller configuration dialog.
/// </summary>
public partial class AcsControlCardConfigView : Window
{
    public AcsControlCardConfigView(object card)
    {
        InitializeComponent();

        var viewModel = new AcsControlCardConfigViewModel();
        if (!viewModel.SetCard(card))
        {
            throw new ArgumentException("当前调试对象不是 ACS 控制卡。", nameof(card));
        }

        DataContext = viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
