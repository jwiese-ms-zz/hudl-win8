﻿using Caliburn.Micro;
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
        private const int SNAPPED_FONT_SIZE = 24;
        private const int FONT_SIZE = 28;

        private const Visibility SNAPPED_VISIBILITY = Visibility.Collapsed;
        private const Visibility FULL_VISIBILITY = Visibility.Visible;

        private readonly INavigationService navigationService;

        private ConcurrentDictionary<string, Task<ClipResponse>> CachedCutupCalls;
        private List<CutupViewModel> CachedCutups;

        private BindableCollection<GameViewModel> _schedule { get; set; }
        public BindableCollection<GameViewModel> Schedule
        {
            get { return _schedule; }
            set
            {
                _schedule = value;
                NotifyOfPropertyChange(() => Schedule);
            }
        }

        private BindableCollection<CategoryViewModel> _categories { get; set; }
        public BindableCollection<CategoryViewModel> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value;
                NotifyOfPropertyChange(() => Categories);
            }
        }
        
        private BindableCollection<CutupViewModel> _cutups { get; set; }
        public BindableCollection<CutupViewModel> Cutups
        {
            get { return _cutups; }
            set
            {
                _cutups = value;
                NotifyOfPropertyChange(() => Cutups);
            }
        }

        // Maps to the selected game in the game list
        private GameViewModel selectedGame;
        public GameViewModel SelectedGame
        {
            get { return selectedGame; }
            set
            {
                selectedGame = value;
                NotifyOfPropertyChange(() => SelectedGame);
            }
        }

        private CategoryViewModel selectedCategory;
        public CategoryViewModel SelectedCategory
        {
            get { return selectedCategory; }
            set
            {
                selectedCategory = value;
                NotifyOfPropertyChange(() => SelectedCategory);
            }
        }

        private Season selectedSeason;
        public Season SelectedSeason
        {
            get { return selectedSeason; }
            set
            {
                selectedSeason = value;
                NotifyOfPropertyChange(() => SelectedSeason);
            }
        }

        private Visibility _visibility;
        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                _visibility = value;
                NotifyOfPropertyChange(() => Visibility);
            }
        }

        private Visibility _scheduleProgressRing_Visibility;
        public Visibility ScheduleProgressRing_Visibility
        {
            get { return _scheduleProgressRing_Visibility; }
            set
            {
                _scheduleProgressRing_Visibility = value;
                NotifyOfPropertyChange(() => ScheduleProgressRing_Visibility);
            }
        }

        private Visibility _headerProgressRing_Visibility;
        public Visibility HeaderProgressRing_Visibility
        {
            get { return _headerProgressRing_Visibility; }
            set
            {
                _headerProgressRing_Visibility = value;
                NotifyOfPropertyChange(() => HeaderProgressRing_Visibility);
            }
        }

        private Visibility _cutupsProgressRing_Visibility;
        public Visibility CutupsProgressRing_Visibility
        {
            get { return _cutupsProgressRing_Visibility; }
            set
            {
                _cutupsProgressRing_Visibility = value;
                NotifyOfPropertyChange(() => CutupsProgressRing_Visibility);
            }
        }

        private Visibility _scheduleVisibility;
        public Visibility ScheduleVisibility
        {
            get { return _scheduleVisibility; }
            set
            {
                _scheduleVisibility = value;
                NotifyOfPropertyChange(() => ScheduleVisibility);
            }
        }

        private Visibility _headersVisibility;
        public Visibility HeadersVisibility
        {
            get { return _headersVisibility; }
            set
            {
                _headersVisibility = value;
                NotifyOfPropertyChange(() => HeadersVisibility);
            }
        }

        private Visibility _cutupsVisibility;
        public Visibility CutupsVisibility
        {
            get { return _cutupsVisibility; }
            set
            {
                _cutupsVisibility = value;
                NotifyOfPropertyChange(() => CutupsVisibility);
            }
        }

        private BindableCollection<Season> seasonsForDropDown;
        public BindableCollection<Season> SeasonsDropDown
        {
            get { return seasonsForDropDown; }
            set
            {
                seasonsForDropDown = value;
                NotifyOfPropertyChange(() => SeasonsDropDown);
            }
        }

        public SectionViewModel(INavigationService navigationService) : base(navigationService)
        {
            this.navigationService = navigationService;
            CharmsData.navigationService = navigationService;
            SettingsPane.GetForCurrentView().CommandsRequested += CharmsData.SettingCharmManager_HubCommandsRequested;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ScheduleProgressRing_Visibility = Visibility.Collapsed;
            HeaderProgressRing_Visibility = Visibility.Collapsed;
            CutupsProgressRing_Visibility = Visibility.Collapsed;
            Visibility = Visibility.Collapsed;

            // Get the team and season ID
            string teamID;
            string seasonID;
            try
            {
                TeamContextResponse response = AppDataAccessor.GetTeamContext();
                teamID = response.teamID;
                seasonID = response.seasonID;
            }
            catch (Exception ex)
            {
                teamID = null;
                seasonID = null;
            }

			CachedCutups = new List<CutupViewModel>();
            CachedCutupCalls = new ConcurrentDictionary<string, Task<ClipResponse>>();

            if (CachedParameter.isInitialized)
            {
                SeasonsDropDown = CachedParameter.seasonsDropDown;
                SelectedSeason = CachedParameter.seasonSelected;
                Cutups = CachedParameter.sectionViewCutups;
                if (CachedParameter.categoryId != null && CachedParameter.gameId != null)
                {
                    LoadPageFromParameter(SelectedSeason.seasonID, SelectedSeason.owningTeam.teamID, CachedParameter.gameId, CachedParameter.categoryId, CachedParameter.sectionViewGames);
                }
                else
                {
                    LoadPageFromDefault(SelectedSeason.seasonID, SelectedSeason.owningTeam.teamID, CachedParameter.sectionViewGames);
                }
            }
            if (Cutups != null)
            {
                var currentViewState = ApplicationView.Value;
                if (currentViewState == ApplicationViewState.Snapped)
                {
                    foreach (var cutup in Cutups)
                    {
                        cutup.Name_Visibility = SNAPPED_VISIBILITY;
                        cutup.Thumbnail_Visibility = SNAPPED_VISIBILITY;
                        cutup.Width = new GridLength(0);
                        cutup.FontSize = SNAPPED_FONT_SIZE;
                    }
                }
                else
                {
                    foreach (var cutup in Cutups)
                    {
                        cutup.Name_Visibility = FULL_VISIBILITY;
                        cutup.Thumbnail_Visibility = FULL_VISIBILITY;
                        cutup.Width = new GridLength(180);
                        cutup.FontSize = FONT_SIZE;
                    }
                }
            }
        }

        private async void LoadPageFromParameter(string seasonID, string teamID, string gameID, string categoryID, BindableCollection<GameViewModel> games)
        {
            Cutups = null;
            if (games != null)
            {
                Schedule = games;
                foreach (var g in Schedule.ToList())
                {
                    g.TextColor = "#E0E0E0";
                }
            }
            else
            {
                await GetGames(teamID, seasonID);
            }

            // Make sure there are game entries for the season.
            if (Schedule.Any())
            {
                // Find the passed in game
                SelectedGame = Schedule.FirstOrDefault(game => game.GameId == gameID);

                // If the game isn't found set the first one as the default
                if (SelectedGame == null)
                {
                    SelectedGame = Schedule.FirstOrDefault();
                }
                await GetGameCategories(SelectedGame);

                // Make sure there are categories for the selected game
                if (Categories.Any())
                {
                    // Find the selected category
                    SelectedCategory = Categories.FirstOrDefault(cat => cat.CategoryId == categoryID);

                    // If the category isn't found set the first as the default
                    if (SelectedCategory == null)
                    {
                        SelectedCategory = Categories.FirstOrDefault();
                    }
                }
                else
                {
                    Categories = null;
                }
            }
            else
            {
                Schedule = null;
            }
        }

        private async void LoadPageFromDefault(string seasonID, string teamID, BindableCollection<GameViewModel> games)
        {
            Cutups = null;
            if (games != null)
            {
                Schedule = games;
                foreach (var g in Schedule.ToList())
                {
                    g.TextColor = "#E0E0E0";
                }
            }
            else
            {
                await GetGames(teamID, seasonID);
            }
            if (Schedule.Any())
            {
                if (Schedule.Contains(CachedParameter.sectionViewGameSelected))
                {
                    SelectedGame = CachedParameter.sectionViewGameSelected;
                    SelectedGame.TextColor = "#0099FF";
                    Categories = CachedParameter.sectionViewCategories;
                    SelectedCategory = CachedParameter.sectionViewCategorySelected;
                    Cutups = CachedParameter.sectionViewCutups;
                }
                else
                {
                    SelectedGame = Schedule.First();
                    await GetGameCategories(SelectedGame);
                    if (Categories.Any())
                    {
                        SelectedCategory = Categories.First();
                    }
                    else
                    {
                        Categories = null;
                    }
                }
            }
            else
            {
                Schedule = null;
                HeaderProgressRing_Visibility = Visibility.Collapsed;
            }
        }

        private async Task<ClipResponse> LoadCutup(CutupViewModel cutup)
        {
            CachedCutups.Add(cutup);
            return await ServiceAccessor.GetCutupClips(cutup);
        }

        public async Task GetGames(string teamID, string seasonID)
        {
            ScheduleVisibility = Visibility.Collapsed;
            ScheduleProgressRing_Visibility = Visibility.Visible;
            GameResponse response = await ServiceAccessor.GetGames(teamID.ToString(), seasonID.ToString());
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                var schedule = new BindableCollection<GameViewModel>();
                foreach (Game game in response.games)
                {
                    schedule.Add(GameViewModel.FromGame(game));
                }
                Schedule = new BindableCollection<GameViewModel>();
                for (int i = schedule.Count() - 1; i >= 0; i--)
                {
                    Schedule.Add(schedule[i]);
                }
            }
            else
            {
                Schedule = null;
            }
            ScheduleProgressRing_Visibility = Visibility.Collapsed;
            ScheduleVisibility = Visibility.Visible;
        }

        public async Task GetGameCategories(GameViewModel game)
        {
            HeadersVisibility = Visibility.Collapsed;
            HeaderProgressRing_Visibility = Visibility.Visible;
            Categories = null;
            game.TextColor = "#0099FF";
            CategoryResponse response = await ServiceAccessor.GetGameCategories(game.GameId.ToString());
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                var cats = new BindableCollection<CategoryViewModel>();
                foreach (Category category in response.categories)
                {
                    cats.Add(CategoryViewModel.FromCategory(category));
                }
                Categories = cats;
            }
            else
            {
                Categories = null;
            }
            HeaderProgressRing_Visibility = Visibility.Collapsed;
            HeadersVisibility = Visibility.Visible;
        }

        public async Task GetCutupsByCategory(CategoryViewModel category)
        {
            Visibility = Visibility.Collapsed;
            CutupsVisibility = Visibility.Collapsed;
            CutupsProgressRing_Visibility = Visibility.Visible;
            Cutups = null;
            SelectedCategory.TextColor = "#0099FF";
            CutupResponse response = await ServiceAccessor.GetCategoryCutups(category.CategoryId.ToString());
            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                //var cuts = new BindableCollection<CutupViewModel>();
                Cutups = new BindableCollection<CutupViewModel>();
                foreach (Cutup cutup in response.cutups)
                {
                    Cutups.Add(CutupViewModel.FromCutup(cutup));
                    Task<ClipResponse> tempResponse = LoadCutup(CutupViewModel.FromCutup(cutup));
                    CachedCutupCalls.TryAdd(cutup.cutupId, tempResponse);
                }
                //Cutups = cuts;
            }
            var currentViewState = ApplicationView.Value;
            if (currentViewState == ApplicationViewState.Snapped)
            {
                foreach (var cutup in Cutups)
                {
                    cutup.Name_Visibility = SNAPPED_VISIBILITY;
                    cutup.Thumbnail_Visibility = SNAPPED_VISIBILITY;
                    cutup.Width = new GridLength(0);
                    cutup.FontSize = SNAPPED_FONT_SIZE;
                }
            }
            if (Cutups == null || Cutups.Count == 0)
            {
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
            CutupsProgressRing_Visibility = Visibility.Collapsed;
            CutupsVisibility = Visibility.Visible;

        }

        public async Task GetClipsByCutup(CutupViewModel cutup)
        {
            ClipResponse response;
            if (CachedCutupCalls.ContainsKey(cutup.CutupId))
            {
                // Don't need to check if it exists b/c the addition to cached cutups is in the same place as cached cutup calls
                int cutCacheIndex = CachedCutups.FindIndex(cut => cut.CutupId == cutup.CutupId);
                cutup = CachedCutups[cutCacheIndex];
                response = await CachedCutupCalls[cutup.CutupId];
            }
            else
            {
                response = await ServiceAccessor.GetCutupClips(cutup);
            }



            if (response.status == SERVICE_RESPONSE.SUCCESS)
            {
                cutup.Clips = response.clips;
                string[] clipCount = cutup.ClipCount.ToString().Split(' ');
                UpdateCachedParameter();
                CachedParameter.selectedCutup = new Cutup { cutupId = cutup.CutupId, clips = cutup.Clips, displayColumns = cutup.DisplayColumns, clipCount = Int32.Parse(clipCount[0]), name = cutup.Name };
                CachedParameter.sectionViewCutupSelected = cutup;

                //disable UI
                navigationService.NavigateToViewModel<VideoPlayerViewModel>();
            }
            else
            {
                Common.APIExceptionDialog.ShowGeneralExceptionDialog(null, null);
            }
        }

        public async void GameSelected(ItemClickEventArgs eventArgs)
        {
            if (Schedule != null)
            {
                var game = (GameViewModel)eventArgs.ClickedItem;
                SelectedGame = game;
                ListView x = (ListView)eventArgs.OriginalSource;
                x.SelectedItem = game;
                Cutups = null;
                foreach (var g in Schedule.ToList())
                {
                    g.TextColor = "#E0E0E0";
                }
                await GetGameCategories(game);

                if (Categories.Any())
                {
                    SelectedCategory = Categories.First();
                }
                else
                {
                    Categories = null;
                }
            }
        }

        public void CategorySelected(SelectionChangedEventArgs eventArgs)
        {
            if (Categories != null)
            {
                var category = (CategoryViewModel)eventArgs.AddedItems.FirstOrDefault();
                List<CategoryViewModel> categories = Categories.ToList();
                foreach (var cat in categories)
                {
                    cat.TextColor = "#E0E0E0";
                }

                SelectedCategory = category;
                GetCutupsByCategory(category);
            }
        }

        public async void CutupSelected(ItemClickEventArgs eventArgs)
        {
            var cutup = (CutupViewModel)eventArgs.ClickedItem;
            await GetClipsByCutup(cutup);
        }

        internal void SeasonSelected(object p)
        {
            Schedule = null;
            var selectedSeason = (Season)p;
            AppDataAccessor.SetTeamContext(selectedSeason.seasonID, selectedSeason.owningTeam.teamID);
            UpdateParameterOnSeasonChange();
            Categories = null;
            LoadPageFromDefault(selectedSeason.seasonID, selectedSeason.owningTeam.teamID, null);
        }

        public void GoBack()
        {
            UpdateCachedParameter();
            navigationService.GoBack();
        }

        public void LogOut()
        {
            navigationService.NavigateToViewModel<LoginViewModel>();
        }

        public void UpdateCachedParameter()
        {
            CachedParameter.seasonsDropDown = SeasonsDropDown;
            CachedParameter.seasonSelected = SelectedSeason;
            CachedParameter.sectionViewCutups = Cutups;
            CachedParameter.sectionViewCategorySelected = SelectedCategory;
            CachedParameter.sectionViewCategories = Categories;
            CachedParameter.sectionViewGames = Schedule;
            CachedParameter.sectionViewGameSelected = SelectedGame;
            CachedParameter.gameId = null;
            CachedParameter.categoryId = null;
        }

        public void UpdateParameterOnSeasonChange()
        {
            CachedParameter.hubViewNextGame = null;
            CachedParameter.hubViewPreviousGame = null;
        }

        public void OnWindowSizeChanged()
        {
            if (Cutups != null)
            {
                var currentViewState = ApplicationView.Value;
                if (currentViewState == ApplicationViewState.Snapped)
                {
                    foreach (var cutup in Cutups)
                    {
                        cutup.Name_Visibility = SNAPPED_VISIBILITY;
                        cutup.Thumbnail_Visibility = SNAPPED_VISIBILITY;
                        cutup.Width = new GridLength(0);
                        cutup.FontSize = SNAPPED_FONT_SIZE;
                    }
                }
                else
                {
                    foreach (var cutup in Cutups)
                    {
                        cutup.Name_Visibility = FULL_VISIBILITY;
                        cutup.Thumbnail_Visibility = FULL_VISIBILITY;
                        cutup.Width = new GridLength(180);
                        cutup.FontSize = FONT_SIZE;
                    }
                }

                if (Cutups == null || Cutups.Count == 0)
                {
                    //Visibility = Visibility.Visible;
                }
                else
                {
                    Visibility = Visibility.Collapsed;
                }
            }
        }


    }
}
