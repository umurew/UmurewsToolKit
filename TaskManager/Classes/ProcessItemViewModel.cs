using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TaskManager.Classes
{
    public class ProcessItemViewModel : INotifyPropertyChanged
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        private const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;
        private const int SW_SHOW = 5;

        public event PropertyChangedEventHandler? PropertyChanged;
        private Process _Process;
        private DateTime _LastCPUCheck;
        private TimeSpan _LastTotalProcessorTime;
        private double _CPUUsage;

        public ProcessItemViewModel(Process Process)
        {
            _Process = Process ?? throw new ArgumentNullException(nameof(Process));

            try
            {
                _LastCPUCheck = DateTime.UtcNow;
                _LastTotalProcessorTime = _Process.TotalProcessorTime;
            }
            catch { _LastCPUCheck = DateTime.MinValue; }
        }

        public Process Process
        {
            get => _Process;
            set
            {
                _Process = value ?? throw new ArgumentNullException(nameof(value));

                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(ProcessId));
                OnPropertyChanged(nameof(MemoryUsage));
                OnPropertyChanged(nameof(CPUUsage));
                OnPropertyChanged(nameof(CPUColor));
                OnPropertyChanged(nameof(Status));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? PropertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        public string DisplayName
        {
            get
            {
                try
                {
                    string? FileName = Process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(FileName) && File.Exists(FileName))
                    {
                        FileVersionInfo FileVersionInfo = FileVersionInfo.GetVersionInfo(FileName);

                        if (!string.IsNullOrWhiteSpace(FileVersionInfo.FileDescription))
                            return FileVersionInfo.FileDescription;
                    }
                }
                catch { }

                return Process.ProcessName;
            }
        }

        public int ProcessId => Process.Id;

        public string CPUColor
        {
            get
            {
                if (_CPUUsage > 80)
                    return "#E74856";  // Kırmızı

                if (_CPUUsage > 50)
                    return "#FFA900";  // Turuncu

                return "#6CB86C";  // Yeşil
            }
        }

        public string CPUUsage
        {
            get
            {
                try
                {
                    var CurrentTime = DateTime.UtcNow;
                    var CurrentTotalProcessorTime = _Process.TotalProcessorTime;

                    var TimeDiff = (CurrentTime - _LastCPUCheck).TotalMilliseconds;
                    var ProcessorTimeDiff = (CurrentTotalProcessorTime - _LastTotalProcessorTime).TotalMilliseconds;

                    if (TimeDiff > 0)
                    {
                        _CPUUsage = (ProcessorTimeDiff / (Environment.ProcessorCount * TimeDiff)) * 100;
                        _CPUUsage = Math.Max(0, Math.Min(100, _CPUUsage));
                    }

                    _LastCPUCheck = CurrentTime;
                    _LastTotalProcessorTime = CurrentTotalProcessorTime;
                }
                catch { _CPUUsage = 0; }

                return $"%{_CPUUsage:F1}";
            }
        }

        public string MemoryUsage
        {
            get
            {
                try
                {
                    return $"{Process.WorkingSet64 / (1024.0 * 1024.0):F2} MB";
                }
                catch { return "N/A"; }
            }
        }

        public string Status => "Running";

        public void Refresh()
        {
            try
            {
                _Process.Refresh();
                OnPropertyChanged(nameof(MemoryUsage));
            }
            catch { }
        }

        public void EndTask(XamlRoot XamlRoot)
        {
            try
            {
                _Process.Kill();
            }
            catch (Exception Exception)
            {
                AppNotification Notification = new AppNotificationBuilder()
                    .AddText($"{typeof(Exception).ToString()} Occured")
                    .AddText($"{Exception.Message}")
                    .BuildNotification();

                AppNotificationManager.Default.Show(Notification);
            }
        }

        public void OpenFileLocation(XamlRoot XamlRoot)
        {
            try
            {
                if (_Process.MainModule is not null)
                    Process.Start("explorer.exe", $"/select,\"{_Process.MainModule.FileName}\"");
                else
                {
                    AppNotification Notification = new AppNotificationBuilder()
                    .AddText($"Exception Occured")
                    .AddText($"MainModule was null.")
                    .BuildNotification();

                    AppNotificationManager.Default.Show(Notification);
                }

            }
            catch (Exception Exception)
            {
                AppNotification Notification = new AppNotificationBuilder()
                    .AddText($"{typeof(Exception).ToString()} Occured")
                    .AddText($"{Exception.Message}")
                    .BuildNotification();

                AppNotificationManager.Default.Show(Notification);
            }
        }

        public void OpenProperties(XamlRoot XamlRoot)
        {
            try
            {
                if (_Process.MainModule is not null)
                {
                    SHELLEXECUTEINFO ShellExecuteInfo = new SHELLEXECUTEINFO();
                    ShellExecuteInfo.cbSize = Marshal.SizeOf(ShellExecuteInfo);
                    ShellExecuteInfo.lpVerb = "properties";
                    ShellExecuteInfo.lpFile = _Process.MainModule.FileName;
                    ShellExecuteInfo.nShow = SW_SHOW;
                    ShellExecuteInfo.fMask = SEE_MASK_INVOKEIDLIST;

                    if (!ShellExecuteEx(ref ShellExecuteInfo))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
                else
                {
                    AppNotification Notification = new AppNotificationBuilder()
                    .AddText($"Exception Occured")
                    .AddText($"MainModule was null.")
                    .BuildNotification();

                    AppNotificationManager.Default.Show(Notification);
                }
            }
            catch (Exception Exception)
            {
                AppNotification Notification = new AppNotificationBuilder()
                .AddText($"{typeof(Exception).ToString()} Occured")
                .AddText($"{Exception.Message}")
                .BuildNotification();

                AppNotificationManager.Default.Show(Notification);
            }
        }
    }
}
