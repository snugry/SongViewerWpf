using Microsoft.Win32;
using SongViewerWpf.DataClasses;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SongViewerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _songIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    AddFile(file);
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

        private void ChangeSong(int index)
        {
            PdfViewer.PdfPath = (string)((ListViewItem)SongList.Items[index]).Tag;
            PdfViewer.MainScroll.ScrollToRightEnd();

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

            if (dialog.ShowDialog() == true)
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
                Filter = "Json(*.json)|*.json|All(*.*)|*"
            };

            if(dialog.ShowDialog() == true)
            {
                SongList.Items.Clear();

                string fileContent = File.ReadAllText(dialog.FileName);

                List<JsonData> jsonList = JsonSerializer.Deserialize<List<JsonData>>(fileContent);
                foreach(var item in jsonList)
                {
                    ListViewItem listViewItem = new ListViewItem();
                    listViewItem.Content = item.Name;
                    listViewItem.Tag = item.Path;

                    SongList.Items.Add(listViewItem);
                }
                _songIndex = 0;
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
    }
}