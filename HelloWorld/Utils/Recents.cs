using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace HelloWorld.Utils
{
    public class Recents
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public StorageFolder Folder { get; set; }

        public Recents()
        {
            
        }

    }

    public class RecentsViewModel
    {
        private Recents defaultRecents = new Recents();
        public Recents DefaultRecents { get { return this.defaultRecents; } }
        private ObservableCollection<Recents> recents = new ObservableCollection<Recents>();
        public ObservableCollection<Recents> Recents { get { return this.recents; } }

        public RecentsViewModel() => LoadRecents();

        private async void LoadRecents()
        {
            var local = Windows.Storage.ApplicationData.Current.LocalSettings;
            var mruToken = local.Values["mru"];
            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

            foreach (Windows.Storage.AccessCache.AccessListEntry entry in mru.Entries)
            {
                string token = entry.Token;

                try
                {
                    Windows.Storage.StorageFolder folder = await mru.GetFolderAsync(token);
                    this.recents.Add(new Recents() { Name = folder.Name, Path = folder.Path, Folder = folder});

                }
                catch (System.IO.FileNotFoundException fe)
                {
                    
                    // do nothing?

                }
            }


        }

    }
}
