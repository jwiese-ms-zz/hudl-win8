using Caliburn.Micro;
using HudlRT.Common;
using HudlRT.Models;
using HudlRT.Parameters;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;

namespace HudlRT.ViewModels
{
    public class SectionViewModel : ViewModelBase
    {
        INavigationService navigationService;
        public string Parameter { get; set; }       //Passed in from hub page - contains the game Id.
        private string _gameId;     //Used to tell if the page needs to be reloaded

        private bool appBarOpen;
        public bool AppBarOpen
        {
            get { return appBarOpen; }
            set
            {
                appBarOpen = value;
                NotifyOfPropertyChange(() => AppBarOpen);
            }
        }

        private Visibility downloading_Visibility;
        public Visibility Downloading_Visibility
        {
            get { return downloading_Visibility; }
            set
            {
                downloading_Visibility = value;
                NotifyOfPropertyChange(() => Downloading_Visibility);
            }
        }

        private Visibility downloadButton_Visibility;
        public Visibility DownloadButton_Visibility
        {
            get { return downloadButton_Visibility; }
            set
            {
                downloadButton_Visibility = value;
                NotifyOfPropertyChange(() => DownloadButton_Visibility);
            }
        }

        private BindableCollection<CategoryViewModel> _categories;
        public BindableCollection<CategoryViewModel> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value;
                NotifyOfPropertyChange(() => Categories);
            }
        }

        public SectionViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            this.navigationService = navigationService;
            Categories = new BindableCollection<CategoryViewModel>();
        }

        protected override void OnActivate()
        {
            SettingsPane.GetForCurrentView().CommandsRequested += CharmsData.SettingCharmManager_HubCommandsRequested;
            //To insure the data shown is fetched if coming from the hub page to a new game
            //But that it doesn't fetch the data again if coming back from the video page.
            if (Parameter != _gameId)
            {
                _gameId = Parameter;
                GetGameCategories(_gameId);

            }
            base.OnActivate();
        }

        private void CategoriesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridView catgegoriesGrid = (GridView)sender;
            var playlistsSelected = catgegoriesGrid.SelectedItems.ToList();

            //var playlistsAdded = e.AddedItems.ToList();
        }

        public async Task GetGameCategories(string gameID)
        {
            //if(CachedParameter.gameId == )
            Categories = null;
            CategoryResponse response = await ServiceAccessor.GetGameCategories(gameID);
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                BindableCollection<CategoryViewModel> cats = new BindableCollection<CategoryViewModel>();
                foreach (Category category in response.categories)
                {
                    CategoryViewModel cat = new CategoryViewModel(category);
                    cats.Add(cat);
                    await AddPlaylistsForCategory(cat);
                }
                Categories = cats;
            }
            else
            {
                Categories = null;
            }
        }

        public async Task AddPlaylistsForCategory(CategoryViewModel category)
        {
            PlaylistResponse response = await ServiceAccessor.GetCategoryPlaylists(category.CategoryModel.categoryId);
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                category.Playlists = new BindableCollection<PlaylistViewModel>();
                foreach (Playlist playlist in response.playlists)
                {
                    category.Playlists.Add(new PlaylistViewModel(playlist));
                    AddClipsAndHeadersForPlaylist(playlist);
                }
            }
            else
            {
                //What should go here?
            }
        }

        public async Task AddClipsAndHeadersForPlaylist(Playlist playlist)
        {
            playlist.clips = new BindableCollection<Clip>();
            ClipResponse response = await ServiceAccessor.GetPlaylistClipsAndHeaders(playlist.playlistId);
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                playlist.clips = response.clips;
                playlist.displayColumns = response.DisplayColumns;
            }
            else
            {
            }
        }

        public void PlaylistSelected(ItemClickEventArgs eventArgs)
        {
            navigationService.NavigateToViewModel<VideoPlayerViewModel>(((PlaylistViewModel)eventArgs.ClickedItem).PlaylistModel);

        }
    }
}
