using System.IO;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed partial class OslNoticeDialog : ContentDialog
    {
        public OslNoticeDialog()
        {
            this.InitializeComponent();
        }

        private async void OslNoticeDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            string notice = null;
            using (var reader = new StreamReader("OSL"))
            {
                notice = await reader.ReadToEndAsync();
            }
            LicenseNoticeTextBox.Text = notice;
        }
    }
}
