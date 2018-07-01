﻿using System;
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
    public static class Save
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

        public static async void SaveComments(Canvas canvas, CommentModel comments)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                var file = await folder.CreateFileAsync("comment", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                using (var outputStream = stream.GetOutputStreamAt(0))
                {
                    using (var dataWriter = new DataWriter(outputStream))
                    {
                        foreach (Comment comment in comments.comments)
                        {
                            Debug.WriteLine("hey" + comments.comments.Count);
                            dataWriter.WriteString($"{Serialize(comment)}\n");
                        }
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }           
                stream.Dispose();
                
            }
        }

        public static string Serialize<T>(this T obj)

        {
            var ms = new MemoryStream();
            // Write an object to the Stream and leave it opened
            using (var writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, ownsStream: false))
            {
                var ser = new DataContractSerializer(typeof(T));
                ser.WriteObject(writer, obj);
            }
            // Read serialized string from Stream and close it
            using (var reader = new StreamReader(ms, Encoding.UTF8))
            {
                ms.Position = 0;
                return reader.ReadToEnd();
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
                    if (file.Name.Equals("comment"))
                    {
                        string text = await FileIO.ReadTextAsync(file);
                        string[] components = text.Split('\n');

                        foreach (string component in components)
                        {
                            if (component.Length > 0)
                            {
                                Comment c = Deserialize<Comment>(component);
                                var rectangle = new Rectangle();
                                rectangle.Width = c.width;
                                rectangle.Height = c.height;
                                Canvas.SetTop(rectangle, c.top);
                                Canvas.SetLeft(rectangle, c.left);
                                rectangle.Fill = new SolidColorBrush(c.fill);

                                var rotation = new RotateTransform();
                                rotation.Angle = c.angle;
                                rectangle.RenderTransform = rotation;
                                canvas.Children.Add(rectangle);
                            }
                        }
                    }
                    

                   
                }
            }
        }

        public static T Deserialize<T>(this string xml)
        {
            var ms = new MemoryStream();
            // Write xml content to the Stream and leave it opened
            using (var writer = new StreamWriter(ms, Encoding.UTF8, 512, leaveOpen: true))
            {
                writer.Write(xml);
                writer.Flush();
                ms.Position = 0;
            }
            // Read Stream to the Serializer and Deserialize and close it
            using (var reader = XmlDictionaryReader.CreateTextReader(ms, Encoding.UTF8, new XmlDictionaryReaderQuotas(), null))
            {
                var ser = new DataContractSerializer(typeof(T));
                return (T)ser.ReadObject(reader);
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
