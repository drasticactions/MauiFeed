using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.Translations;
using MauiFeed.WinUI.Tools;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiFeed.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    private IErrorHandlerService errorHandler;
    private IAppDispatcher dispatcher;
    private ApplicationSettingsService applicationSettingsService;
    private Window window;
    private OpmlFeedListItemFactory opmlFeedListItemFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    /// <param name="mainWindow">Main Window.</param>
    public SettingsPage(Window mainWindow)
    {
        this.InitializeComponent();
        this.window = mainWindow;
        this.DataContext = this;
        this.errorHandler = Ioc.Default.GetService<IErrorHandlerService>()!;
        this.dispatcher = Ioc.Default.GetService<IAppDispatcher>()!;
        this.applicationSettingsService = Ioc.Default.GetService<ApplicationSettingsService>()!;
        this.opmlFeedListItemFactory = Ioc.Default.GetService<OpmlFeedListItemFactory>()!;
        this.ImportDatabaseCommand = new AsyncCommand(this.ImportDatabaseAsync, null, this.dispatcher, this.errorHandler);
        this.ExportDatabaseCommand = new AsyncCommand(this.ExportDatabaseAsync, null, this.dispatcher, this.errorHandler);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the current version of the app.
    /// </summary>
    public string Version => WinUIExtensions.GetAppVersion();

    /// <summary>
    /// Gets or sets the app theme.
    /// </summary>
    public AppTheme AppTheme
    {
        get
        {
            return this.applicationSettingsService.ApplicationElementTheme;
        }

        set
        {
            this.applicationSettingsService.ApplicationElementTheme = value;
        }
    }

    /// <summary>
    /// Gets or sets the element theme.
    /// </summary>
    public LanguageSetting LanguageSetting
    {
        get
        {
            return this.applicationSettingsService.ApplicationLanguageSetting;
        }

        set
        {
            this.applicationSettingsService.ApplicationLanguageSetting = value;
        }
    }

    /// <summary>
    /// Gets the ImportDatabaseCommand.
    /// </summary>
    public AsyncCommand ImportDatabaseCommand { get; private set; }

    /// <summary>
    /// Gets the ExportDatabaseCommand.
    /// </summary>
    public AsyncCommand ExportDatabaseCommand { get; private set; }

    /// <summary>
    /// Gets the Languages.
    /// </summary>
    public List<Tuple<string, AppTheme>> AppThemes { get; } = new List<Tuple<string, AppTheme>>()
        {
            new Tuple<string, AppTheme>(Common.DefaultThemeLabel, AppTheme.Default),
            new Tuple<string, AppTheme>(Common.DarkThemeLabel, AppTheme.Dark),
            new Tuple<string, AppTheme>(Common.LightThemeLabel, AppTheme.Light),
        };

    /// <summary>
    /// Gets the Languages.
    /// </summary>
    public List<Tuple<string, LanguageSetting>> Languages { get; } = new List<Tuple<string, LanguageSetting>>()
        {
            new Tuple<string, LanguageSetting>(Common.DefaultLanguage, LanguageSetting.Default),
            new Tuple<string, LanguageSetting>(Common.EnglishLanguage, LanguageSetting.English),
            new Tuple<string, LanguageSetting>(Common.JapaneseLanguage, LanguageSetting.Japanese),
        };

    private async Task ImportDatabaseAsync()
    {
        var filePicker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this.window);
        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
        filePicker.FileTypeFilter.Add(".db");
        var file = await filePicker.PickSingleFileAsync();
    }

    private async Task ExportDatabaseAsync()
    {
        var opml = this.opmlFeedListItemFactory.GenerateOpmlFeed();
        var filePicker = new FileSavePicker();
        var hwnd = this.window.GetWindowHandle();
        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
        filePicker.SuggestedFileName = "mauifeed";
        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        filePicker.SettingsIdentifier = "settingsIdentifier";
        filePicker.FileTypeChoices.Add("opml", new List<string>() { ".opml" });
        filePicker.DefaultFileExtension = ".opml";
        var file = await filePicker.PickSaveFileAsync();
        if (file is not null)
        {
            Windows.Storage.CachedFileManager.DeferUpdates(file);

            await Windows.Storage.FileIO.WriteTextAsync(file, opml.ToString());

            Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        }
    }

    /// <summary>
    /// On Property Changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        var changed = this.PropertyChanged;
        if (changed == null)
        {
            return;
        }

        changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#pragma warning disable SA1600 // Elements should be documented
    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
#pragma warning restore SA1600 // Elements should be documented
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        onChanged?.Invoke();
        this.OnPropertyChanged(propertyName);
        return true;
    }

    private void LanguageComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        this.LanguageComboBox.SelectedIndex = this.Languages.IndexOf(this.Languages.First(n => n.Item2 == this.LanguageSetting));
    }

    private void LanguageComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        this.LanguageSetting = this.Languages[this.LanguageComboBox.SelectedIndex].Item2;
    }

    private void ThemeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        this.AppTheme = this.AppThemes[this.ThemeComboBox.SelectedIndex].Item2;
    }

    private void ThemeComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        this.ThemeComboBox.SelectedIndex = this.AppThemes.IndexOf(this.AppThemes.First(n => n.Item2 == this.AppTheme));
    }
}
