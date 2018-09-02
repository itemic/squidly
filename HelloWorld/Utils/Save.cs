using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Protocol2.Utils
{
    public class Save
    {
        private StorageFolder projectFolder;


        public void SetFolder(StorageFolder topLevel)
        {
            projectFolder = topLevel;
        }

        public async Task CreateFolder(StorageFolder topLevel, String fileName)
        {
           

            projectFolder = await topLevel.CreateFolderAsync(fileName, CreationCollisionOption.ReplaceExisting);

            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            var token = mru.Add(projectFolder);

            var local = Windows.Storage.ApplicationData.Current.LocalSettings;
            local.Values["mru"] = token;
        }

        public async Task CreateFolder(String toplevelName)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var topLevel = await folderPicker.PickSingleFolderAsync();

            projectFolder = await topLevel.CreateFolderAsync(toplevelName, CreationCollisionOption.ReplaceExisting);

            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            var token = mru.Add(projectFolder);

            var local = Windows.Storage.ApplicationData.Current.LocalSettings;
            local.Values["mru"] = token;
        }

        public async Task CreateFolder(/*StorageFolder topLevel*/)
        {
            await CreateFolder("DefaultProject");
        }


        public async Task SaveAll(InkCanvas inkCanvas, CommentModel comments, AnimationModel animations)
        {

            // delete everything first
            var files = await projectFolder.GetFilesAsync();
            foreach (StorageFile file in files)
            {
                await file.DeleteAsync(StorageDeleteOption.Default);
            }

            // save ink
            var inkFile = await projectFolder.CreateFileAsync("InkFile.gif", CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream streamX = await inkFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await inkCanvas.InkPresenter.StrokeContainer.SaveAsync(streamX);
            }

            // save animations
            var animationsFile = await projectFolder.CreateFileAsync("animations.txt", CreationCollisionOption.ReplaceExisting);
            var animationStream = await animationsFile.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = animationStream.GetOutputStreamAt(0))
            {
                using (var dataWriter = new DataWriter(outputStream))
                {
                    foreach (Animation animation in animations.GetAnimations())
                    {
                        dataWriter.WriteString($"{Serialize(animation)}\n");
                    }
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }
            animationStream.Dispose();

            // save comments
            var commentsFile = await projectFolder.CreateFileAsync("comments.txt", CreationCollisionOption.ReplaceExisting);
            var stream = await commentsFile.OpenAsync(FileAccessMode.ReadWrite);

            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                using (var dataWriter = new DataWriter(outputStream))
                {
                    foreach (Comment comment in comments.GetComments())
                    {
                        dataWriter.WriteString($"{Serialize(comment)}\n");
                    }
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }

            stream.Dispose();

            var iterator = 0;

            foreach (Comment comment in comments.GetComments())
            {
                var inkComment = await projectFolder.CreateFileAsync("CommentInk" + iterator + ".gif", CreationCollisionOption.ReplaceExisting);
                using (IRandomAccessStream s = await inkComment.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await comment.ic.SaveAsync(s);
                }
                iterator++;
            }
        }

   
        public string Serialize<T>(T obj)

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

        public async Task LoadNew(InkCanvas inkCanvas, CommentModel commentModel, AnimationModel animationModel)
        {
            if (projectFolder != null)
            {
                var files = await projectFolder.GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    if (file.Name.Equals("InkFile.gif"))
                    {
                        IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                        using (var inputStream = stream.GetInputStreamAt(0))
                        {
                            await inkCanvas.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                        }
                        stream.Dispose();
                    }
                    else if (file.Name.Equals("comments.txt"))
                    {
                        string text = await FileIO.ReadTextAsync(file);
                        string[] components = text.Split('\n');
                        foreach (string component in components)
                        {
                            if (component.Length > 0)
                            {
                                Comment c = Deserialize<Comment>(component);
                                commentModel.Add(c);
                                Debug.WriteLine(commentModel.GetComments().Count());
                            }
                        }
                    } else if (file.Name.Equals("animations.txt"))
                    {
                        string text = await FileIO.ReadTextAsync(file);
                        string[] components = text.Split('\n');
                        foreach (string component in components)
                        {
                            if (component.Length > 0)
                            {
                                Animation a = Deserialize<Animation>(component);
                                animationModel.Add(a);
                                Debug.WriteLine(animationModel.GetAnimations().Count);
                            }
                        }
                    }
                }
               
                    foreach (StorageFile file in files)
                    {
                        if (file.Name.StartsWith("CommentInk"))
                        {
                            IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                            using (var inputStream = stream.GetInputStreamAt(0))
                            {
                                // first get the # of the ink
                                Regex re = new Regex(@"\d+");
                                Match m = re.Match(file.Name);
                                int inkPos = int.Parse(m.Value); // we will need to have better error handling

                                // then set it
                                commentModel.GetComments()[inkPos].ic = new Windows.UI.Input.Inking.InkStrokeContainer();

                                await commentModel.GetComments()[inkPos].ic.LoadAsync(inputStream);

                            }
                        }
                    }
                
                


            }
        }

        public async Task LoadAll(InkCanvas inkCanvas, CommentModel commentModel, AnimationModel animationModel)
        {
            commentModel.GetComments().Clear();
            animationModel.GetAnimations().Clear();

            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            projectFolder = await folderPicker.PickSingleFolderAsync();

            await LoadNew(inkCanvas, commentModel, animationModel);
            
        }
        
        public T Deserialize<T>(string xml)
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
