using Microsoft.UI.Xaml.Controls;

namespace Pwrschdlr.Dialogs;

/// <summary>Explains what Pwrschdlr does when it starts, until the user opts out.</summary>
public sealed partial class WelcomeDialog : ContentDialog
{
    public WelcomeDialog() => InitializeComponent();

    public bool DontShowAgain => DontShowAgainBox.IsChecked == true;
}
