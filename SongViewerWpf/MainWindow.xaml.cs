
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using HidLibrary;

using SongViewerWpf.DataClasses;

namespace SongViewerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _songIndex = 0;

        private Random _rand;

        public MainWindow()
        {
            InitializeComponent();
            _rand = new Random();
            Task.Run(() => MonitorHidDevice());
        }

        private void MonitorHidDevice()
        {
            var devices = HidDevices.Enumerate().ToList();
            var device = devices.FirstOrDefault(d => d.Description.Contains("FS-1 WL"));

            if (device != null)
            {
                device.OpenDevice();

                device.MonitorDeviceEvents = true;
                device.ReadReport(OnReport);

                device.CloseDevice();
            }
        }

        private void OnReport(HidReport report)
        {
            var data = report.Data;
            // Process the HID report data
            //Dispatcher.Invoke(() => MessageBox.Show($"HID Data: {BitConverter.ToString(data)}"));
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    var ext = System.IO.Path.GetExtension(file);
                    if (ext.ToLower().EndsWith("pdf"))
                    {
                        AddFile(file);
                    }
                    else if (ext.ToLower().EndsWith("json"))
                    {
                        LoadPlaylist(file);
                        return; // We don't want to load several playlists
                    }
                }
            }
        }

        private void AddFile(string file)
        {
            ListViewItem newItem = new();
            newItem.Content = (SongList.Items.Count + 1).ToString("00") + " - " + System.IO.Path.GetFileNameWithoutExtension(file);
            newItem.Tag = file;

            SongList.Items.Add(newItem);
        }

        private void LoadPlaylist(string file)
        {
            SongList.Items.Clear();

            string fileContent = File.ReadAllText(file);

            List<JsonData> jsonList = JsonSerializer.Deserialize<List<JsonData>>(fileContent);
            foreach (var item in jsonList)
            {
                ListViewItem listViewItem = new ListViewItem();
                listViewItem.Content = item.Name;
                listViewItem.Tag = item.Path;

                SongList.Items.Add(listViewItem);
            }
            _songIndex = 0;
        }

        private void ChangeSong(int index)
        {
            PdfViewer.PdfPath = (string)((ListViewItem)SongList.Items[index]).Tag;
            PdfViewer.MainScroll.ScrollToLeftEnd();

            int prevIndex = index - 1;
            if(prevIndex < 0)
            {
                prevIndex = SongList.Items.Count - 1;
            }
            PrevBtn.Content = (string)((ListViewItem)SongList.Items[prevIndex]).Content;

            int nextIndex = index + 1;
            if (nextIndex >= SongList.Items.Count)
            {
                nextIndex = 0;
            }
            NextBtn.Content = (string)((ListViewItem)SongList.Items[nextIndex]).Content;
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            MainTab.SelectedIndex = 1;

            if(SongList.Items.Count > 0)
            {
                if (SongList.SelectedItem != null)
                {
                    _songIndex = SongList.SelectedIndex;
                }

                ChangeSong(_songIndex);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Tab)
            {
                _songIndex++;
                if(_songIndex >= SongList.Items.Count)
                {
                    _songIndex = 0;
                }

                ChangeSong(_songIndex);
                e.Handled = true;
            }
            else if(e.Key == Key.Up)
            {
                PdfViewer.MainScroll.ScrollToRightEnd();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                PdfViewer.MainScroll.ScrollToLeftEnd();
                e.Handled = true;
            }
            else if( e.Key == Key.Escape)
            {
                MainTab.SelectedIndex = 0;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Json(*.json)|*.json|All(*.*)|*"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<JsonData> jsonList = new List<JsonData>();
                int i = 0;
                foreach (ListViewItem item in SongList.Items)
                {
                    JsonData jsonData = new JsonData();
                    jsonData.Index = i;
                    jsonData.Name = (string)item.Content;
                    jsonData.Path = (string)item.Tag;

                    jsonList.Add(jsonData);
                    i++;
                }

                string jsonString = JsonSerializer.Serialize(jsonList);

                File.WriteAllText(dialog.FileName, jsonString);
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "Json(*.json)|*.json|Pdf(*.pdf)|*.pdf|All(*.*)|*"
            };

            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var file = dialog.FileName;
                var ext = System.IO.Path.GetExtension(file);

                if (ext.ToLower().EndsWith("json"))
                {
                    LoadPlaylist(file);
                }
                else if (ext.ToLower().EndsWith("pdf"))
                {
                    AddFile(file);
                }
            }
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            _songIndex--;
            if (_songIndex < 0)
            {
                _songIndex = SongList.Items.Count - 1;
            }

            ChangeSong(_songIndex);
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _songIndex++;
            if (_songIndex >= SongList.Items.Count)
            {
                _songIndex = 0;
            }

            ChangeSong(_songIndex);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine(e.Key.ToString());
        }
         
        private void LoadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = folderBrowser.SelectedPath;

                SongList.Items.Clear();
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    if (System.IO.Path.GetExtension(file).ToLower().EndsWith("pdf"))
                    {
                        AddFile(file);
                    }
                }
                _songIndex = 0;
            }
        }

        private void RandBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SongList.Items.Count == 0)
            {
                return;
            }

            _songIndex = _rand.Next(0, SongList.Items.Count);
            ChangeSong(_songIndex);
        }
    }
}