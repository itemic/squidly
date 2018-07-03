﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace HelloWorld.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewProject : ContentDialog
    {

        public ContentDialogResult result { get; set; }
        public StorageFolder selectedFolder;

        public NewProject()
        {
            this.InitializeComponent();
        }

        private async void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            result = ContentDialogResult.Primary;
            if (!projectName.Text.Equals(""))
            {
                if (selectedFolder != null)
                {
                    newProjectDialog.Hide();
                } else
                {
                    // yikes no folder selected
                }
            } else
            {
                // yikes no name given
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            newProjectDialog.Hide();
        }

        private async void ChooseDirectory(object sender, RoutedEventArgs e)
        {
            FolderPicker fp = new FolderPicker();
            fp.FileTypeFilter.Add("*");

            StorageFolder folder = await fp.PickSingleFolderAsync();
            if (folder != null)
            {
                selectedFolder = folder;
                filePath.Text = folder.Path;
            } else
            {
                selectedFolder = null;
            }
        }
    }
}
