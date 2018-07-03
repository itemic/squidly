﻿using HelloWorld.Utils;
using HelloWorld.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace HelloWorld
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public RecentsViewModel vm { get; set; }

        public Home()
        {
            this.InitializeComponent();
            this.vm = new RecentsViewModel();
            
        }

        private async void AsyncTest()
        {
            var local = Windows.Storage.ApplicationData.Current.LocalSettings;
            var mruToken = local.Values["mru"];
            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

            foreach (Windows.Storage.AccessCache.AccessListEntry entry in mru.Entries)
            {
                string token = entry.Token;
                Windows.Storage.IStorageItem item = await mru.GetItemAsync(token);
                Debug.WriteLine(item.Path);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), false);
        }

        private async void LoadClick(object sender, RoutedEventArgs e)
        {
            // really should use some view model thing
            
                this.Frame.Navigate(typeof(MainPage), true);
            
        }

        private async void TestSaveShow(object sender, RoutedEventArgs e)
        {
            NewProject newProject = new NewProject();
            await newProject.ShowAsync();
        }
    }
}
