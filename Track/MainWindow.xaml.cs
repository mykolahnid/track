using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Track.Models;
using System.Windows.Media.Imaging;

// for hotkeys used http://www.codeproject.com/Tips/274003/Global-Hotkeys-in-WPF#

namespace Track
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Prefix = "track ";
        private const string DateFormat = "yyyy-MM-dd";
        private readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private readonly DispatcherTimer saveSessionTimer = new DispatcherTimer();
        private readonly List<Button> timeButtons = new List<Button>();
        private readonly List<Label> timeLabels = new List<Label>();
        private readonly Stopwatch totalStopwatch = new Stopwatch();
        private Session session;

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void session_TaskAdded(object sender, EventArgs e)
        {
            string taskName = ((Task) sender).Name;
            var newLabel = new Label {Tag = taskName};
            timeLabels.Add(newLabel);
            var newButton = new Button {Content = taskName};
            timeButtons.Add(newButton);
            newButton.Click += OnClick;
            Grid.Children.Add(newLabel);
            Grid.Children.Add(newButton);
            var columnsCount = (int) (Math.Ceiling(timeLabels.Count/2.0));
            if (Grid.ColumnDefinitions.Count < columnsCount + 1)
            {
                Grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            Grid.SetColumn(stopButton, columnsCount);
            Grid.SetColumn(addButton, columnsCount);
            Grid.SetColumn(HistoryButton, columnsCount);

            for (int i = 0; i < timeLabels.Count; i++)
            {
                Grid.SetColumn(timeLabels[i], i%(columnsCount));
                Grid.SetRow(timeLabels[i], 3*(i/columnsCount));
                Grid.SetColumn(timeButtons[i], i%(columnsCount));
                Grid.SetRow(timeButtons[i], 1 + (i/columnsCount));
            }

            if (timeButtons.Count <= 9)
            {
                var customHotKey = new HotKey(Key.D1 + timeButtons.Count - 1, ModifierKeys.Control);
                customHotKey.HotKeyPressed += customHotKey_HotKeyPressed;
                hotKeyHost.AddHotKey(customHotKey);
            }
        }

        void customHotKey_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            var task = session.Tasks[e.HotKey.Key - Key.D1];
            var taskName = task.Name;
            StartTask(taskName);
            var notification = new Notification { NotificationText = task.Name + " " + task.DurationHours };
            notification.Show();
        }

        private void OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var taskName = (sender as Button).Content.ToString();
            StartTask(taskName);
        }
        private void StartTask(string taskName)
        {
            session.Start(taskName);
            UpdateTaskbar(taskName);
            UpdateLabelsColor();
        }

        private void UpdateTaskbar(string text)
        {
            if (text == string.Empty)
            {
                TaskbarItemInfo.Overlay = null;
                return;
            }

            int iconWidth = 20;
            int iconHeight = 20;

            string countText = text.Trim();

            RenderTargetBitmap bmp =
                new RenderTargetBitmap(iconWidth, iconHeight, 96, 96, PixelFormats.Default);

            ContentControl root = new ContentControl();

            root.ContentTemplate = ((DataTemplate)Resources["OverlayIcon"]);
            root.Content = countText;

            root.Arrange(new Rect(0, 0, iconWidth, iconHeight));

            bmp.Render(root);

            TaskbarItemInfo.Overlay = (ImageSource)bmp;
        }

        private void UpdateLabelsColor()
        {
            for (int i = 0; i < timeLabels.Count; i++)
            {
                timeLabels[i].Foreground = session.Tasks[i].Active ? Brushes.Red : Brushes.Black;
            }
        }

        private void saveSessionTimer_Tick(object sender, EventArgs e)
        {
            SaveSession();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            double total = 0;
            for (int i = 0; i < timeLabels.Count; i++)
            {                
                timeLabels[i].Content = session.Tasks[i].DurationHours;
                total += session.Tasks[i].Duration;
            }
            Title = (total/60/60).ToString("0.0") + " / " + session.TotalDuration.TotalHours.ToString("0.0");
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            session.Stop();
            UpdateTaskbar("");
            UpdateLabelsColor();
        }

        private void SaveSession()
        {
            string sessionString = session.Serialize();

            string trackDirectory = GetTrackDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(trackDirectory, Prefix + session.Today.ToString(DateFormat) + ".txt"),
                    sessionString);
            }
            catch
            {
            }
        }

        private void LoadSession()
        {
            string trackDirectory = GetTrackDirectory();            
            string[] sessions = Directory.GetFiles(trackDirectory);
            var mostRecentTrackDate = new DateTime(1970, 1, 1);
            string mostRecentTrackFile = "";
            foreach (string s in sessions)
            {
                var dateTime = GetTrackDate(s);
                if (dateTime > mostRecentTrackDate)
                {
                    mostRecentTrackDate = dateTime;
                    mostRecentTrackFile = s;
                }
            }

            if (mostRecentTrackFile != "")
            {
                var deserialized = Session.Deserialize(File.ReadAllText(mostRecentTrackFile));
                if (deserialized != null)
                {
                    if (deserialized.Today.Date != DateTime.Today.Date)
                    {
                        foreach (Task task in deserialized.Tasks)
                        {
                            task.Duration = 0;
                        }
                        deserialized.Today = DateTime.Today;
                    }
                }
                session = deserialized;
            }
            if (session == null)
            {
                session = new Session();
            }
        }

        private static DateTime GetTrackDate(string s)
        {
            DateTime dateTime;
            DateTime.TryParseExact(s.Substring(s.IndexOf(Prefix) + Prefix.Length, DateFormat.Length), DateFormat,
                new CultureInfo("en-US"), DateTimeStyles.None, out dateTime);
            return dateTime;
        }

        private static string GetTrackDirectory()
        {
            string applicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string trackDirectory = Path.Combine(applicationDataDirectory, "Track");
            if (!Directory.Exists(trackDirectory))
            {
                Directory.CreateDirectory(trackDirectory);
            }
            return trackDirectory;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveSession();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var addTask = new AddTask();
            if (addTask.ShowDialog().GetValueOrDefault())
            {
                session.AddTask(addTask.TaskName);
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            string trackDirectory = GetTrackDirectory();

            string[] sessions = Directory.GetFiles(trackDirectory);
            var tracks = new SortedDictionary<DateTime, string>();
            foreach (string s in sessions)
            {
                var dateTime = GetTrackDate(s);
                var deserialized = Session.Deserialize(File.ReadAllText(s));
                if (deserialized != null)
                {
                    tracks.Add(dateTime, deserialized.ToString());
                }
            }

            var historyBuilder = new StringBuilder();
            foreach (var date in tracks.Keys)
            {
                historyBuilder.AppendLine(date.ToString("ddd dd MMM") + " " + tracks[date]);
            }
            historyBuilder.AppendLine();
            historyBuilder.Append("Would you like to delete the history?");
            var messageBoxResult = MessageBox.Show(historyBuilder.ToString(),"History", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                foreach (var s in sessions)
                {
                    File.Delete(s);
                }
            }
        }

        private HotKeyHost hotKeyHost;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hotKeyHost = new HotKeyHost((HwndSource)HwndSource.FromVisual(App.Current.MainWindow));
            var customHotKey = new HotKey(Key.D0, ModifierKeys.Control);
            customHotKey.HotKeyPressed += stopHotKey_HotKeyPressed;
            hotKeyHost.AddHotKey(customHotKey);

            LoadSession();
            session.TaskAdded += session_TaskAdded;
            foreach (Task task in session.Tasks)
            {
                session_TaskAdded(task, null);
            }

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            saveSessionTimer.Tick += saveSessionTimer_Tick;
            saveSessionTimer.Interval = TimeSpan.FromMinutes(1);
            saveSessionTimer.Start();

            totalStopwatch.Start();
        }

        private void stopHotKey_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            StopButton_Click(stopButton, null);
            var notification = new Notification {NotificationText = "Stop"};
            notification.Show();
        }
    }
}