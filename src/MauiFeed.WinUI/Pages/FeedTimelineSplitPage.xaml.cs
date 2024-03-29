using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Drastic.Services;
using Drastic.Tools;
using MauiFeed.Models;
using MauiFeed.Services;
using MauiFeed.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Realms;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiFeed.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedTimelineSplitPage : Page, INotifyPropertyChanged
    {
        private bool isRefreshing;
        private FeedSidebarItem? selectedSidebarItem;
        private FeedItem? selectedItem;
        private Realm context;
        private MainWindow sidebar;
        private IErrorHandlerService errorHandler;
        private IAppDispatcher dispatcher;
        private ITemplateService template;
        private FeedService rssFeedCacheService;
        private ThemeSelectorService themeService;
        private WindowsPlatformService platform;
        private Progress<RssCacheFeedUpdate> refreshProgress;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedTimelineSplitPage"/> class.
        /// </summary>
        /// <param name="sidebar">Sidebar View.</param>
        public FeedTimelineSplitPage(MainWindow sidebar)
        {
            this.InitializeComponent();
            this.sidebar = sidebar;
            this.DataContext = this;
            this.ArticleList.DataContext = this;
            this.refreshProgress = Ioc.Default.GetService<Progress<RssCacheFeedUpdate>>()!;
            this.refreshProgress.ProgressChanged += this.RefreshProgressProgressChanged;
            this.context = Realm.GetInstance(Ioc.Default.GetRequiredService<RealmConfigurationBase>())!;
            this.rssFeedCacheService = Ioc.Default.GetService<FeedService>()!;
            this.errorHandler = Ioc.Default.GetService<IErrorHandlerService>()!;
            this.template = Ioc.Default.GetService<ITemplateService>()!;
            this.dispatcher = Ioc.Default.GetService<IAppDispatcher>()!;
            this.themeService = Ioc.Default.GetService<ThemeSelectorService>()!;
            this.themeService.ThemeChanged += this.ThemeServiceThemeChanged;
            this.platform = Ioc.Default.GetService<WindowsPlatformService>()!;
            this.MarkAsFavoriteCommand = new AsyncCommand<FeedItem>(this.MarkAsFavoriteAsync, (x) => true, this.errorHandler);
            this.OpenInBrowserCommand = new AsyncCommand<FeedItem>(this.OpenInBrowserAsync, (x) => true, this.errorHandler);
            this.OpenShareCommand = new AsyncCommand<FeedItem>(this.OpenShareAsync, (x) => true, this.errorHandler);
            this.RefreshCommand = new AsyncCommand(this.RefreshAsync, null, this.dispatcher, this.errorHandler);
            this.SetHideUnreadItemsCommand = new AsyncCommand<FeedSidebarItem>(
                (t) =>
                {
                    t.HideUnreadItems = !t.HideUnreadItems;
                    this.UpdateFeed();
                    return Task.CompletedTask;
                },
                (x) => true,
                this.errorHandler);
            this.MarkAsReadCommand = new AsyncCommand<FeedItem>((x) => this.MarkAllAsReadAsync(new List<FeedItem>() { x }), (x) => true, this.errorHandler);
            this.MarkAllAsReadCommand = new AsyncCommand<FeedSidebarItem>((x) => this.MarkAllAsReadAsync(x.Query.ToList()), (x) => true, this.errorHandler);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the app logo path.
        /// </summary>
        public string AppLogo => "Icon.logo.png";

        /// <summary>
        /// Gets the Open In Browser command.
        /// </summary>
        public AsyncCommand<FeedItem> OpenInBrowserCommand { get; private set; }

        /// <summary>
        /// Gets the Open Share command.
        /// </summary>
        public AsyncCommand<FeedItem> OpenShareCommand { get; private set; }

        /// <summary>
        /// Gets the Mark as Read command.
        /// </summary>
        public AsyncCommand<FeedItem> MarkAsReadCommand { get; private set; }

        /// <summary>
        /// Gets the refresh command.
        /// </summary>
        public AsyncCommand RefreshCommand { get; private set; }

        /// <summary>
        /// Gets the Set Hide Unread Items Command.
        /// </summary>
        public AsyncCommand<FeedSidebarItem> SetHideUnreadItemsCommand { get; private set; }

        /// <summary>
        /// Gets the Mark as Favorite command.
        /// </summary>
        public AsyncCommand<FeedItem> MarkAsFavoriteCommand { get; private set; }

        /// <summary>
        /// Gets the Mark All As Read Command.
        /// </summary>
        public AsyncCommand<FeedSidebarItem> MarkAllAsReadCommand { get; private set; }

        /// <summary>
        /// Gets the list of items.
        /// </summary>
        public ObservableCollection<FeedItem> Items { get; private set; } = new ObservableCollection<FeedItem>();

        /// <summary>
        /// Gets or sets the selected sidebar item.
        /// </summary>
        public FeedSidebarItem? SelectedSidebarItem
        {
            get
            {
                return this.selectedSidebarItem;
            }

            set
            {
                this.SetProperty(ref this.selectedSidebarItem, value);
                this.UpdateFeed();
            }
        }

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public FeedItem? SelectedItem
        {
            get
            {
                return this.selectedItem;
            }

            set
            {
                this.SetProperty(ref this.selectedItem, value);
                this.UpdateSelectedFeedItemAsync().FireAndForgetSafeAsync(this.errorHandler);
            }
        }

        /// <summary>
        /// Gets a value indicating whether to show the icon.
        /// </summary>
        public bool ShowIcon
        {
            get
            {
                // If it's a smart filter or folder, always show the icon.
                if (this.selectedSidebarItem?.SidebarItemType != SidebarItemType.FeedListItem)
                {
                    return true;
                }

                var feed = this.Items.Select(n => n.Feed).Distinct();
                return feed.Count() > 1;
            }
        }

        /// <summary>
        /// Update the selected feed item, if not null.
        /// </summary>
        public void UpdateFeed()
        {
            this.Items.Clear();

            var items = this.selectedSidebarItem?.Query ?? new List<FeedItem>();

            foreach (var item in items)
            {
                this.Items.Add(item);
            }

            this.OnPropertyChanged(nameof(this.ShowIcon));
        }

        /// <summary>
        /// Update the selected feed item, if it exists.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task UpdateSelectedFeedItemAsync()
        {
            if (this.SelectedItem is null)
            {
                return;
            }

            //this.context.FeedItems!.Update(this.SelectedItem);
            //await this.context.SaveChangesAsync();

            //this.sidebar.UpdateSidebar();
        }

        private async Task MarkAllAsReadAsync(List<FeedItem> items)
        {
            await this.context.WriteAsync(() => {
                var allRead = items.All(n => n.IsRead);

                foreach (var item in items)
                {
                    item.IsRead = !allRead;
                }
            });

            //this.sidebar.UpdateSidebar();
        }

        private async Task RefreshAsync()
        {
            if (this.selectedSidebarItem?.FeedListItem is not null && !this.isRefreshing)
            {
                await this.rssFeedCacheService.ReadFeedAsync(this.selectedSidebarItem.FeedListItem);
                this.UpdateFeed();
               // this.sidebar.UpdateSidebar();
            }
        }

        private async Task MarkAsFavoriteAsync(FeedItem item)
        {
            await this.context.WriteAsync(() => {
                item.IsFavorite = !item.IsFavorite;
            });
           // this.sidebar.UpdateSidebar();
        }

        private async Task OpenInBrowserAsync(FeedItem item)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(item.Link!));
        }

        private Task OpenShareAsync(FeedItem item)
        {
            this.platform.ShareUrlAsync(item.Link!).FireAndForgetSafeAsync();
            return Task.CompletedTask;
        }

        private async Task RenderFeedAsync(FeedItem item)
        {
            var result = await this.template.RenderFeedItemAsync(item, this.themeService.IsDark);
            this.LocalRssWebview.SetSource(result);
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

        private void ArticleListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.FirstOrDefault() as FeedItem;
            if (selected is null)
            {
                return;
            }

            selected.IsRead = true;
            this.SelectedItem = selected;

            this.RenderFeedAsync(selected).FireAndForgetSafeAsync();
        }

        private void ThemeServiceThemeChanged(object? sender, EventArgs e)
        {
            if (this.SelectedItem is not null)
            {
                this.RenderFeedAsync(this.SelectedItem).FireAndForgetSafeAsync();
            }
        }

        private void RefreshProgressProgressChanged(object? sender, RssCacheFeedUpdate e)
        {
            this.isRefreshing = !e.IsDone;
        }
    }
}
