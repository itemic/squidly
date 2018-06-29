using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace HelloWorld.Utils
{
    public class Save
    {
        public async static void SaveInk(InkCanvas inkCanvas)
        {
            if (inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("Gif with embedded ISF", new List<string> { ".gif" });

                var file = await savePicker.PickSaveFileAsync();

                if (null != file)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
            }
        }

        public async static void SaveComments(Canvas canvas)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                var file = await folder.CreateFileAsync("comment", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                string buffer = "";
                foreach (UIElement comment in canvas.Children)
                {
                    buffer += Canvas.GetTop(comment).ToString() + ";" + Canvas.GetLeft(comment).ToString() + "\n";
                    
                }

                using (var outputStream = stream.GetOutputStreamAt(0))
                {
                    using (var dataWriter = new DataWriter(outputStream))
                    {
                        dataWriter.WriteString(buffer);
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }


                }
            }
        }

        public async static void LoadComments(Canvas canvas)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                var files = await folder.GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    string text = await FileIO.ReadTextAsync(file);
                    string[] components = text.Split(';');

                    var rectangle = new Rectangle();
                    rectangle.Width = 25;
                    rectangle.Height = 25;
                    double top = double.Parse(components[0]);
                    double left = double.Parse(components[1]);
                    Canvas.SetTop(rectangle, top);
                    Canvas.SetLeft(rectangle, left);
                    rectangle.Fill = new SolidColorBrush(Windows.UI.Colors.SteelBlue);
                    Debug.WriteLine("hey");

                    canvas.Children.Add(rectangle);
                }
            }
        }

        public async static void LoadInk(InkCanvas inkCanvas)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".gif");

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                using (var inputStream = stream.GetInputStreamAt(0))
                {
                    await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                }
                stream.Dispose();
            }
        }
    }
}
