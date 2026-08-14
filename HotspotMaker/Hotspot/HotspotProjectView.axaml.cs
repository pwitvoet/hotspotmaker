using Avalonia.Controls;
using Avalonia.Input;
using HotspotMaker.Controls;
using HotspotMaker.Util.UI;
using System;

namespace HotspotMaker.Hotspot;

public partial class HotspotProjectView : UserControl
{
    private HotspotProjectVM? HotspotProjectVM { get; set; }


    public HotspotProjectView()
    {
        InitializeComponent();
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        EditorView.HandleKeyDown(e);
    }


    protected override void OnDataContextChanged(EventArgs e)
    {
        if (HotspotProjectVM != null)
            HotspotProjectVM.HotspotRectangleLabelsFocusRequested -= HotspotProjectVM_HotspotRectangleLabelsFocusRequested;

        base.OnDataContextChanged(e);

        HotspotProjectVM = DataContext as HotspotProjectVM;
        if (HotspotProjectVM != null)
            HotspotProjectVM.HotspotRectangleLabelsFocusRequested += HotspotProjectVM_HotspotRectangleLabelsFocusRequested;
    }

    private void HotspotProjectVM_HotspotRectangleLabelsFocusRequested()
    {
        HotspotRectangleLabelsTextBox.Focus(NavigationMethod.Tab);
    }

    private void TextBox_LostFocus(object? sender, FocusChangedEventArgs e)
    {
        switch (sender)
        {
            case TextBox textBox:
                FocusTracking.ReportFocusLoss(textBox, TextBox.TextProperty);
                break;

            case LabelsTextBox labelsTextBox:
                FocusTracking.ReportFocusLoss(labelsTextBox, LabelsTextBox.LabelsProperty);
                break;
        }
    }
}