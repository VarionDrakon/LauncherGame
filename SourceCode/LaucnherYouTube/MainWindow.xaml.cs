﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace LaucnherYouTube
{
    public partial class MainWindow : Window
    {
        private readonly string zipPath = @".\ChacheDownloadGame.zip";
        private readonly string appTemlPath = "tempDirectoryUnzip";
        private readonly string xmlFile = "Assets/version.xml";
        private string _stateLocateVersionXML;
        private string _stateServerVersionXML;
        private int? idProcessApp = null;
        private bool appIsStarting = false;
        private bool checkUpdate = true;
        private bool isStartUnzipUpdateFileApp = true;
        public static bool UserAllowUpdateApp { get; set; } = false;
        private DispatcherTimer dispatcherTimer;
        private Process processApp;
        private Point scrollPointMouse = new Point();
        private double speedScrollHorizontalOffset = 0.3;

        WebClient clientDownloadApp = new WebClient();

        public MainWindow()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExcepctionEventApp);
            InitializeComponent();

            UpdateUI();
            ServerXMLDownload();
            UpdateContentSever();
            LocateVersionXML();
        }
        #region XMLREAD
        private void LocateVersionXML()
        {
            XmlReader reader = XmlReader.Create(xmlFile);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "Main")
                    {
                        reader.ReadToFollowing("version");
                        _stateLocateVersionXML = reader.ReadElementContentAsString();
                    }
                }
            }
        }
        public async void ServerXMLDownload()
        {
            try
            {
                HttpClient serverClient = new HttpClient();
                var responseServerClient = await serverClient.GetAsync(new Uri("https://raw.githubusercontent.com"));
                if (responseServerClient.IsSuccessStatusCode)
                {
                    WebClient client = new WebClient();
                    client.DownloadFileCompleted += CompleteDownloadVersionXMLServer;
                    client.DownloadFileAsync(new Uri("https://raw.githubusercontent.com/VarionDrakon/LauncherGame/main/versionServer.xml"), "Assets/versionServer.xml");
                    ServerConnecting.Text = "Server online!";
                }
            }
            catch
            {
                ServerConnecting.Text = "Server offline!";
                FoundNewVersion.IsEnabled = false;
            }
        }
        private void CompleteDownloadVersionXMLServer(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                MessageBox.Show("Download error: " + e.Error);
            }
            else
            {
                ServerVersionXML();
            }
        }
        public void ServerVersionXML()
        {
            XmlTextReader reader = new XmlTextReader("Assets/versionServer.xml");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "Main")
                    {
                        reader.ReadToFollowing("version");
                        _stateServerVersionXML = reader.ReadElementContentAsString();
                    }
                }
            }
        }
        #endregion
        #region UIFUNC
        private void ButtonFoundNewVersion(object sender, RoutedEventArgs e)
        {
            ChildWindow.AllowUpdate allowUpdateWindow = new ChildWindow.AllowUpdate();
            allowUpdateWindow.Show();
        }
        private void UpdateUI()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(BackgroundUIFunction);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }
        private void ButtonUpdateDialogWindow(object sender, RoutedEventArgs rea)
        {
            ServerDownloadChacheGameAsync();
        }
        private void ButtonCancelDownloadApp(object sender, RoutedEventArgs e)
        {
            ButtonReinstallApp.IsEnabled = true;
            clientDownloadApp.CancelAsync();
        }
        private void ButtonLaunchGame(object sender, RoutedEventArgs rea)
        {
            ProcessStartInfo procInfoLaunchGame = new ProcessStartInfo();
            procInfoLaunchGame.FileName = @"Game\Steampunk Edge - IcePunk.exe";
            processApp = new Process();
            processApp.StartInfo = procInfoLaunchGame;
            processApp.Start();
            idProcessApp = processApp.Id;
        }
        private void ProgressDownloadServerGame(object sender, DownloadProgressChangedEventArgs dpcea)
        {
            ProgressBarExtractFile.Maximum = 100;
            DownloadAppState.Text = "Download: " + dpcea.BytesReceived + " of " + dpcea.TotalBytesToReceive + " . Process: " + dpcea.ProgressPercentage + "%";
            ProgressBarExtractFile.Value = dpcea.ProgressPercentage;
        }
        private void ButtonKillProcessApp(object sender, RoutedEventArgs e)
        {
            if (appIsStarting == true)
            {
                processApp.Kill();
            }
        } 
        private void ScrollViewerContent_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewerContent.ReleaseMouseCapture();
        }

        private void ScrollViewerContent_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scrollPointMouse = e.GetPosition(ScrollViewerContent);
            speedScrollHorizontalOffset = ScrollViewerContent.HorizontalOffset;
            ScrollViewerContent.CaptureMouse();
        }
        private void ScrollViewerContent_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewerContent.ScrollToHorizontalOffset(ScrollViewerContent.HorizontalOffset - e.Delta);
        }
        private void ScrollViewerContent_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (ScrollViewerContent.IsMouseCaptured)
            {
                ScrollViewerContent.ScrollToHorizontalOffset(speedScrollHorizontalOffset + (scrollPointMouse.X - e.GetPosition(ScrollViewerContent).X));
            }
        }
        #endregion
        #region BACKGROUNDFUNC
        public void BackgroundUIFunction(object sender, EventArgs ea)
        {
            if (_stateLocateVersionXML != _stateServerVersionXML & checkUpdate == true)
            {
                FoundNewVersion.IsEnabled = true;
                checkUpdate = false;
            }
            if (UserAllowUpdateApp == true)
            {
                FoundNewVersion.IsEnabled = false;
                ServerDownloadChacheGameAsync();
                UserAllowUpdateApp = false;
            }
            ProgressBarExtractFile.Minimum = 0;
            _textCurrentVersion.Text = "Current version: " + _stateLocateVersionXML;
            _textServerVersion.Text = "Server version: " + _stateServerVersionXML;
            Process[] processedUsers = Process.GetProcesses();
            foreach (Process allprocessed in processedUsers)
            {
                if (allprocessed.Id == idProcessApp)
                {
                    appIsStarting = true;
                    break;
                }
                else if (allprocessed.Id != idProcessApp) appIsStarting = false;
            }
            if (appIsStarting == false)
            {
                AppState.Text = "App is not starting!";
            }
            else
            {
                AppState.Text = "App is starting!";
            }
        }
        private static void ExcepctionEventApp(object sender, UnhandledExceptionEventArgs ueea)
        {
            Exception e = (Exception)ueea.ExceptionObject;
            LoggingProcessJobs("EXCEPTION" + e.Message.ToString());
        }
        private static void LoggingProcessJobs(string s)
        {
            if (Directory.Exists(@"Log/") == false)
            {
                Directory.CreateDirectory(@"Log/");
            }
            DateTime now = DateTime.Now;
            string todayTimeLog = now.ToString("mmHHddMMyyyyy");
            string nameFileLog = @"Log/" + "Log" + todayTimeLog + ".txt";

            using (StreamWriter sw = new StreamWriter(nameFileLog, false))
            {
                sw.WriteLineAsync(s);
            }
        }
        #endregion
        #region UPDATEFUNC
        public void ServerDownloadChacheGameAsync()
        {
            try
            {
                ButtonReinstallApp.IsEnabled = false;
                LaunchGame.IsEnabled = false;
                clientDownloadApp.DownloadFileCompleted += CompleteDownloadChacheGame;
                clientDownloadApp.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&confirm=no_antivirus&id=10g0Vd_GWyt7VwF392q77NVBNibfGzQLi"), zipPath);
                clientDownloadApp.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressDownloadServerGame);
            }
            catch (Exception e)
            {
                LoggingProcessJobs("EXCEPTION E: " + e.Message.ToString());
            }

        }
        private void CompleteDownloadChacheGame(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                ButtonReinstallApp.IsEnabled = true;
                DownloadAppState.Text = "Download error: " + e.Error + e.Cancelled;
                clientDownloadApp.Dispose();
            }
            else
            {
                using (ZipArchive zipFileServer = ZipFile.OpenRead(zipPath))
                {
                    ProgressBarExtractFile.Value = 0;
                    int zipFilesCount = zipFileServer.Entries.Count;
                    ProgressBarExtractFile.Maximum = zipFilesCount;
                    foreach (var zip in zipFileServer.Entries)
                    {
                        if (isStartUnzipUpdateFileApp == true)
                        {
                            zip.Archive.ExtractToDirectory(appTemlPath);
                            isStartUnzipUpdateFileApp = false;
                            break;
                        }
                    }
                    ProgressBarExtractFile.Value = zipFilesCount;
                }
                File.Delete(zipPath);
                foreach (string dgfse in Directory.GetFileSystemEntries(appTemlPath + "/Game"))
                {
                    FileAttributes attributes = File.GetAttributes(dgfse);
                    DirectoryInfo dirInf = new DirectoryInfo(dgfse);
                    FileInfo fileInf = new FileInfo(dgfse);
                    if (Directory.Exists(@"Game/") == false)
                    {
                        Directory.CreateDirectory("Game");
                    }
                    if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (Directory.Exists(@"Game/" + dirInf.Name) == true)
                        {
                            Directory.Delete(@"Game/" + dirInf.Name, true);
                        }
                        dirInf.MoveTo(@"Game/" + dirInf.Name);
                    }
                    else if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        if (File.Exists(@"Game/" + fileInf.Name) == true)
                        {
                            File.Delete(@"Game/" + fileInf.Name);
                        }
                        fileInf.MoveTo(@"Game/" + fileInf.Name);
                    }
                }
                DownloadAppState.Text = "Game installed!";
                LaunchGame.IsEnabled = true;
                ButtonReinstallApp.IsEnabled = true;
            }
        }
        #endregion
        #region WINDOWMANAGMENT 
        private void UnwrapApp_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void RollUpApp_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            Close();

        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        #endregion
        #region WEBCONTENT
        private void UpdateContentSever()
        {
            var brushContentServer = new ImageBrush();
            brushContentServer.ImageSource = new BitmapImage(new Uri("https://raw.githubusercontent.com/VarionDrakon/VarionDrakon.github.io/4c68931bdfa5acf7a6ef0cb56d13797b7493d523/otherFiles/img/electrods.png", UriKind.Absolute));
            broochButtonContentServer_Content1.Background = brushContentServer;
            broochButtonContentServer_Content2.Background = brushContentServer;
            broochButtonContentServer_Content3.Background = brushContentServer;
            broochButtonContentServer_Content4.Background = brushContentServer;
            broochButtonContentServer_Content5.Background = brushContentServer;
            broochButtonContentServer_Content6.Background = brushContentServer;
        }
        #endregion
    }
}