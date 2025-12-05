using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TaskManager.Pages;

namespace TaskManager
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(TitleBar);

            ContentFrame.Navigate(typeof(ProcessesPage));
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
                ContentFrame.Navigate(typeof(SettingsPage));
            else if (args.SelectedItem is NavigationViewItem Item)
            {
                switch (Item.Tag.ToString())
                {
                    case "ProcessesPage":
                        ContentFrame.Navigate(typeof(ProcessesPage));
                        break;
                    case "StartupPage":
                        ContentFrame.Navigate(typeof(StartupPage));
                        break;
                }
            }
        }
    }
}