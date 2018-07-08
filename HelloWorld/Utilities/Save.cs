using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HelloWorld;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace HelloWorld.Utilities
{
    public static class Save
    {
        public static async void saveInk(InkCanvas inkCanvas)
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

        public static async void LoadComments()
        {

        }

        public static async void SaveComments(List<Rectangle> postits)
        {
            if (postits.Count > 0)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("Comments", new List<string> { ".ptcx" });

                var file = await savePicker.PickSaveFileAsync();

                if (null != file)
                {

                    using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        using (var dataWriter = new DataWriter(stream))
                        {
                            foreach (Rectangle rectangle in postits)
                            {
                                dataWriter.WriteString($"{Serialize(rectangle)}\n");
                            }
                            await dataWriter.StoreAsync();
                            await stream.FlushAsync();
                        }

                    }

                }

            }


        }

        // source: Protocol
        private static string Serialize<T>(this T obj)
        {
            var ms = new MemoryStream();

            using (var writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, ownsStream: false))
            {
                var ser = new DataContractSerializer(typeof(T));
                ser.WriteObject(writer, obj);
            }
            using (var reader = new StreamReader(ms, Encoding.UTF8))
            {
                ms.Position = 0;
                return reader.ReadToEnd();
            }
        }

        // source: Protocol
        private static T Deserialize<T>(this string xml)
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
    }
}
