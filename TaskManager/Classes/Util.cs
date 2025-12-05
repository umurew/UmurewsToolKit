using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TaskManager.Classes
{
    public class Util
    {
        public async void ShowContentDialog(string Title, string Content, XamlRoot XamlRoot)
        {
            ContentDialog Dialog = new ContentDialog
            {
                Title = Title,
                Content = Content,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await Dialog.ShowAsync();
        }

        public async Task<bool> ShowConfirmationDialog(string Title, string Content, XamlRoot XamlRoot)
        {
            ContentDialog Dialog = new ContentDialog
            {
                Title = Title,
                Content = Content,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = XamlRoot
            };

            ContentDialogResult DialogResult = await Dialog.ShowAsync();
            return DialogResult == ContentDialogResult.Primary;
        }
    }
}
