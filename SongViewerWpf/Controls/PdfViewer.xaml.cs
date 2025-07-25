﻿using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Image = System.Windows.Controls.Image;

namespace SongViewerWpf.Controls
{
    /// <summary>
    /// Interaktionslogik für PdfViewer.xaml
    /// </summary>
    public partial class PdfViewer : System.Windows.Controls.UserControl
    {

        #region Bindable Properties

        public string PdfPath
        {
            get { return (string)GetValue(PdfPathProperty); }
            set { SetValue(PdfPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PdfPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PdfPathProperty =
            DependencyProperty.Register("PdfPath", typeof(string), typeof(PdfViewer), new PropertyMetadata(null, propertyChangedCallback: OnPdfPathChanged));

        private static void OnPdfPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pdfDrawer = (PdfViewer)d;

            if (!string.IsNullOrEmpty(pdfDrawer.PdfPath))
            {
                //making sure it's an absolute path
                var path = System.IO.Path.GetFullPath(pdfDrawer.PdfPath);

                StorageFile.GetFileFromPathAsync(path).AsTask()
                  //load pdf document on background thread
                  .ContinueWith(t => PdfDocument.LoadFromFileAsync(t.Result).AsTask()).Unwrap()
                  //display on UI Thread
                  .ContinueWith(t2 => PdfToImages(pdfDrawer, t2.Result), TaskScheduler.FromCurrentSynchronizationContext());
            }

        }

        #endregion


        public PdfViewer()
        {
            InitializeComponent();
        }

        private async static Task PdfToImages(PdfViewer pdfViewer, PdfDocument pdfDoc)
        {
            var items = pdfViewer.PagesContainer.Items;
            items.Clear();

            if (pdfDoc == null) return;

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using (var page = pdfDoc.GetPage(i))
                {
                    var bitmap = await PageToBitmapAsync(page);
                    var image = new Image
                    {
                        Source = bitmap,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        Margin = new Thickness(4, 4, 4, 4),
                        MaxWidth = 1920
                    };
                    items.Add(image);
                }
            }
        }

        private static async Task<BitmapImage> PageToBitmapAsync(PdfPage page)
        {
            BitmapImage image = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                PdfPageRenderOptions renderOptions = new PdfPageRenderOptions();
                renderOptions.DestinationWidth = 1080;
                renderOptions.DestinationHeight = 1920;
                await page.RenderToStreamAsync(stream, renderOptions);

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream.AsStreamForRead();
                image.Rotation = Rotation.Rotate270;
                image.EndInit();
            }

            return image;
        }

    }
}
