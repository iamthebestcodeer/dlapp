using Avalonia.Controls;
using Avalonia.Platform.Storage;
using dlapp.ViewModels;
using System;
using System.Linq;

namespace dlapp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ShowOpenFolderDialog = async () =>
            {
                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Save Folder",
                    AllowMultiple = false
                };

                var result = await StorageProvider.OpenFolderPickerAsync(options);
                return result.FirstOrDefault()?.Path.LocalPath;
            };
        }
    }
}
