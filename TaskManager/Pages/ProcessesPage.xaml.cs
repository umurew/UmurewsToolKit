using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using TaskManager.Classes;

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

        private void MenuFlyout_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();

            /*MenuFlyoutItem? MenuFlyoutItem = sender as MenuFlyoutItem;
            ProcessItemViewModel? ProcessItemViewModel = (MenuFlyoutItem.DataContext as ListViewItem).DataContext as ProcessItemViewModel;

            string? Action = MenuFlyoutItem?.Tag?.ToString();

            if (ProcessItemViewModel is null || string.IsNullOrEmpty(Action))
                throw new Exception("Null");

            switch (Action)
            {
                case "Expand":
                    break;

                case "EndTask":
                    ProcessItemViewModel.EndTask(this.XamlRoot);
                    break;

                case "OpenFileLocation":
                    break;

                case "Properties":
                    break;
            }*/
        }
    }
}