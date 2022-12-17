using LaucnherYouTube.ChildWindow;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
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
        private bool isStartUnzipUpdateFileApp = true;
        private DispatcherTimer dispatcherTimer;
        public MainWindow()
        {
            ServerXMLDownload();
            ServerVersionXML();

            InitializeComponent();

            LocateVersionXML();
            CheckAppLaunchTimer();
            ServerDownloadChacheGame();
            ArgumentsUIValue();
        }
        #region XMLREAD
        private static WebClient client;
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
        public void ServerXMLDownload()
        {
            if (client == null || !client.IsBusy)
            {

                client = new WebClient();
                client.DownloadFileCompleted += CompleteDownloadVersionXMLServer;
                client.DownloadFileAsync(new Uri("https://raw.githubusercontent.com/VarionDrakon/LauncherGame/main/versionServer.xml"), "Assets/versionServer.xml");
 

            }
            if (client != null)
            {
              
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
                MessageBox.Show("Download complete!");
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
        private void ArgumentsUIValue()
        {
            ProgressBarExtractFile.Minimum = 0;
            _textCurrentVersion.Text += _stateLocateVersionXML;
            _textServerVersion.Text += _stateServerVersionXML;
        }
        private void ButtonUpdateDialogWindow(object sender, RoutedEventArgs rea)
        {
            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.ShowDialog();
        }
        private void ButtonLaunchGame(object sender, RoutedEventArgs rea)
        {
            ProcessStartInfo procInfoLaunchGame = new ProcessStartInfo();
            procInfoLaunchGame.FileName = @"Game\Steampunk Edge - IcePunk.exe";
            Process processApp = new Process();
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
        #endregion
        #region BACKGROUNDFUNC
        private void CheckAppLaunchTimer()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(SingleAppTimerCheckMethod);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }
        public void SingleAppTimerCheckMethod(object sender, EventArgs ea)
        {
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
        #endregion
        #region UPDATEFUNC
        public void ServerDownloadChacheGame()
        {
            if (client == null || !client.IsBusy)
            {
                client = new WebClient();
                client.DownloadFileCompleted += CompleteDownloadChacheGame;
                MessageBox.Show("Start Download");
                client.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&confirm=no_antivirus&id=10g0Vd_GWyt7VwF392q77NVBNibfGzQLi"), zipPath);
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressDownloadServerGame);
            }
            if (client != null)
            {
                MessageBox.Show("Debug");
            }
        }
        private void CompleteDownloadChacheGame(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                DownloadAppState.Text = "Download error: " + e.Error;
            }
            else
            {
                ProgressBarExtractFile.Value = 0;
                ZipArchive zipFileServer = ZipFile.OpenRead(zipPath);
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
                ProgressBarExtractFile.Value = zipFilesCount;
                DownloadAppState.Text = "Game installed!";
            }
        }
        #endregion
    }
}

