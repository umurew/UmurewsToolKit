using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using TaskManager.Classes;
using Windows.Services.Maps.LocalSearch;
using Windows.UI.Notifications;

namespace TaskManager.Pages
{
    public sealed partial class ProcessesPage : Page
    {
        private ObservableCollection<ProcessItemViewModel> Processes { get; set; }
        private DispatcherQueueTimer ProcessesRefreshTimer;
        private const int RefreshInterval = 1;

        public ProcessesPage()
        {
            InitializeComponent();

            ProcessesRefreshTimer = DispatcherQueue.CreateTimer();
            Processes = [];

            LoadProcesses();
            StartAutoRefresh();
        }

        private void LoadProcesses()
        {
            try
            {
                var CurrentProcesses = Process.GetProcesses().ToDictionary(Process => Process.Id);

                for (int Iteration = Processes.Count - 1; Iteration >= 0; Iteration--)
                {
                    if (!CurrentProcesses.ContainsKey(Processes[Iteration].ProcessId))
                        Processes.RemoveAt(Iteration);
                    else
                        Processes[Iteration].Refresh();
                }

                var ExistingPIDs = new HashSet<int>(Processes.Select(p => p.ProcessId));

                foreach (var process in CurrentProcesses.Values)
                {
                    if (!ExistingPIDs.Contains(process.Id))
                    {
                        try
                        {
                            Processes.Add(new ProcessItemViewModel(process));
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void StartAutoRefresh()
        {
            ProcessesRefreshTimer.Interval = TimeSpan.FromSeconds(RefreshInterval);
            ProcessesRefreshTimer.Tick += (s, e) => LoadProcesses();
            ProcessesRefreshTimer.Start();
        }

        private void ProcessListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is ListViewItem Item)
            {
                Item.RightTapped -= ListViewItem_RightTapped;
                Item.RightTapped += ListViewItem_RightTapped;
            }
        }

        private void ListViewItem_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is ListViewItem Item)
            {
                Item.IsSelected = true;
                Item.Focus(FocusState.Programmatic);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem? MenuFlyoutItem = sender as MenuFlyoutItem;
            ProcessItemViewModel? ProcessItemViewModel = ProcessListView.SelectedItem as ProcessItemViewModel;

            string? Action = MenuFlyoutItem?.Tag?.ToString();

            if (ProcessItemViewModel is null)
                throw new Exception();

            if (string.IsNullOrEmpty(Action))
                throw new Exception();

            switch (Action)
            {
                case "EndTask":
                    ProcessItemViewModel.EndTask(this.XamlRoot);
                    break;

                case "OpenFileLocation":
                    ProcessItemViewModel.OpenFileLocation(this.XamlRoot);
                    break;

                case "Properties":
                    ProcessItemViewModel.OpenProperties(this.XamlRoot);
                    break;
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton? AppBarButton = sender as AppBarButton;
            ProcessItemViewModel? ProcessItemViewModel = ProcessListView.SelectedItem as ProcessItemViewModel;

            string? Action = AppBarButton?.Tag?.ToString();

            if (string.IsNullOrEmpty(Action))
                return;

            if (ProcessItemViewModel is null)
            {
                if (Action != "RunNewTask")
                    return;
            }

            switch (Action)
            {
                case "RunNewTask":
                    Grid ContentGrid = new();
                    ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    TextBlock ContentBlock = new()
                    {
                        Text = "Type the name of a program, folder, document, or Internet resource, and Windows will open it for you.",
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(5)
                    };
                    Grid.SetRow(ContentBlock, 0);

                    Grid AddressGrid = new();
                    Grid.SetRow(AddressGrid, 1);
                    AddressGrid.Margin = new Thickness(5);
                    AddressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    AddressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });

                    TextBlock TextBlock = new()
                    {
                        Text = "Open:"
                    };
                    Grid.SetColumn(TextBlock, 0);

                    TextBox LocationBox = new()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetColumn(LocationBox, 1);

                    AddressGrid.Children.Add(TextBlock);
                    AddressGrid.Children.Add(LocationBox);

                    CheckBox CheckBox = new()
                    {
                        Content = "Create this task with administrative privileges.",
                        Margin = new Thickness(5)
                    };
                    Grid.SetRow(CheckBox, 2);

                    ContentGrid.Children.Add(ContentBlock);
                    ContentGrid.Children.Add(AddressGrid);
                    ContentGrid.Children.Add(CheckBox);

                    ContentDialog Dialog = new()
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "Create new task",
                        Content = ContentGrid,
                        DefaultButton = ContentDialogButton.Primary,
                        PrimaryButtonText = "OK",
                        SecondaryButtonText = "Browse",
                        CloseButtonText = "Cancel"
                    };

                    Dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick;
                    Dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;

                    await Dialog.ShowAsync();

                    break;

                case "EndTask":
                    ProcessItemViewModel?.EndTask(this.XamlRoot);
                    break;
            }
        }

        private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ContentDialog ContentDialog = sender as ContentDialog;

            Grid? ContentGrid = ContentDialog.Content as Grid;
            Grid? AddressGrid = ContentGrid?.Children.OfType<Grid>().FirstOrDefault();
            TextBox? LocationBox = AddressGrid?.Children.OfType<TextBox>().FirstOrDefault();
            CheckBox? UACCheckBox = ContentGrid?.Children.OfType<CheckBox>().FirstOrDefault();

            if (LocationBox is null || UACCheckBox is null)
            {
                AppNotification Notification = new AppNotificationBuilder()
                .AddText($"NullArgumentException Prevented")
                .AddText($"Either one or two of required elements were null.")
                .BuildNotification();

                AppNotificationManager.Default.Show(Notification);
                return;
            }

            /*if (!File.Exists(LocationBox.Text))
            {
                AppNotification Notification = new AppNotificationBuilder()
                .AddText($"FormatException Prevented")
                .AddText($"Path is in bad format.")
                .BuildNotification();

                AppNotificationManager.Default.Show(Notification);
                return;
            }*/

            try
            {
                string ExpandedPath = Environment.ExpandEnvironmentVariables(LocationBox.Text);
                bool RunAsAdministartor = false;

                if (UACCheckBox.IsChecked == true)
                    RunAsAdministartor = true;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ExpandedPath,
                    UseShellExecute = true,
                    Verb = RunAsAdministartor ? "runas" : string.Empty
                };
            }
            catch (Win32Exception Exception)
            {
                if (Exception.NativeErrorCode == 1223)
                {
                    Debug.WriteLine("User cancelled the UAC elevation.");
                }
                else
                {
                    Debug.WriteLine($"Win32 Error: {Exception.Message}");
                }
            }
            catch (Exception Exception)
            {
                Debug.WriteLine($"Failed to launch task: {Exception.Message}");
            }
        }

        private async void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            ContentDialog ContentDialog = sender as ContentDialog;
            ContentDialog.IsSecondaryButtonEnabled = false;

            var ContentGrid = sender.Content as Grid;
            var AddressGrid = ContentGrid?.Children.OfType<Grid>().FirstOrDefault();
            var LocationBox = AddressGrid?.Children.OfType<TextBox>().FirstOrDefault();

            if (LocationBox is null)
                return;

            var Picker = new FileOpenPicker(ContentDialog.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = "Open",
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };

            PickFileResult? Result = await Picker.PickSingleFileAsync();

            if (Result is not null)
                LocationBox.Text = Result.Path;

            ContentDialog.IsSecondaryButtonEnabled = false;
        }

        private void ProcessListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProcessItemViewModel? ProcessItemViewModel = ProcessListView.SelectedItem as ProcessItemViewModel;

            if (ProcessItemViewModel is null)
                return;

            try
            {
                var MainModule = ProcessItemViewModel.Process.MainModule;
                string WindowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

                if (MainModule is null)
                {
                    EndTaskButton.IsEnabled = false;
                    EfficiencyModeButton.IsEnabled = false;
                    return;
                }

                if (MainModule.FileName.StartsWith(WindowsPath, StringComparison.OrdinalIgnoreCase))
                {
                    EndTaskButton.IsEnabled = false;
                    EfficiencyModeButton.IsEnabled = false;
                    return;
                }

                EndTaskButton.IsEnabled = true;
                EfficiencyModeButton.IsEnabled = true;
            }
            catch (Exception)
            {
                EndTaskButton.IsEnabled = false;
                EfficiencyModeButton.IsEnabled = false;
            }
        }
    }
}