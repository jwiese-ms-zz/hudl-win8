﻿using HudlRT.Common;
using HudlRT.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HudlRT.Models;
using HudlRT.Parameters;
using Windows.UI.Xaml.Markup;
using Windows.UI;using Windows.UI.ViewManagement;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Animation;

namespace HudlRT.Views
{   
    public sealed partial class VideoPlayerView : LayoutAwarePage
    {
        private bool rightClicked { get; set; }
        private bool itemClicked { get; set; }
        private bool _isFullscreenToggle = false;
        public bool IsFullscreen
        {
            get { return _isFullscreenToggle; }
            set { _isFullscreenToggle = value; }
        }
        private bool previousVideoStateIsFullScreen = false;
        private bool previousStateIsSnapped = false;

        private Size _previousVideoContainerSize = new Size();
        private Size _previousVideoSize = new Size();
        private string _rootNamespace;
        private Brush background;
        public string RootNamespace
        {
            get { return _rootNamespace; }
            set { _rootNamespace = value; }
        }
        Point initialPoint = new Point();
        Point currentPoint = new Point();
        bool isGridCollapsed = false;
        private  TranslateTransform dragTranslation;
        private System.Diagnostics.Stopwatch keyPressTimer = new System.Diagnostics.Stopwatch();
        private bool isPaused { get; set; }
        private bool isStopped { get; set; }
        private long keyPressLength = 225;
        private DispatcherTimer rewindTimer { get; set; }
        private int rewindTimerInterval { get; set; }
        private int rewindPositionChange { get; set; }

        public VideoPlayerView()
        {
            this.InitializeComponent();

            dragTranslation = new TranslateTransform();

            gridHeaders.ManipulationStarted += gridHeaders_ManipulationStarted;
            gridHeaders.ManipulationDelta += gridHeaders_ManipulationDelta;
            gridHeaders.ManipulationInertiaStarting += gridHeaders_ManipulationInertiaStarting;

            videoMediaElement.ManipulationStarted += videoMediaElement_ManipulationStarted;
            videoMediaElement.ManipulationInertiaStarting += videoMediaElement_ManipulationInertiaStarting;
            videoMediaElement.ManipulationDelta += videoMediaElement_ManipulationDelta;

            gridHeaders.RenderTransform = this.dragTranslation;
            FilteredClips.RenderTransform = this.dragTranslation;
            gridScroll.ViewChanged += scrollHeaders;
            //gridHeaderScroll.ViewChanged += scrollGrid;

            btnFastForward.AddHandler(PointerPressedEvent, new PointerEventHandler(btnFastForward_Click), true);
            full_btnFastForward.AddHandler(PointerPressedEvent, new PointerEventHandler(btnFastForward_Click), true);
            btnSlowForward.AddHandler(PointerPressedEvent, new PointerEventHandler(btnSlowForward_Click), true);
            full_btnSlowForward.AddHandler(PointerPressedEvent, new PointerEventHandler(btnSlowForward_Click), true);
            btnFastReverse.AddHandler(PointerPressedEvent, new PointerEventHandler(btnFastReverse_Click), true);
            full_btnFastReverse.AddHandler(PointerPressedEvent, new PointerEventHandler(btnFastReverse_Click), true);
            btnSlowReverse.AddHandler(PointerPressedEvent, new PointerEventHandler(btnSlowReverse_Click), true);
            full_btnSlowReverse.AddHandler(PointerPressedEvent, new PointerEventHandler(btnSlowReverse_Click), true);
            
            Window.Current.CoreWindow.KeyDown += VideoPage_KeyDown;
            Window.Current.CoreWindow.KeyUp += VideoPage_KeyUp;

            rewindTimer = new DispatcherTimer();
            rewindTimer.Interval = new TimeSpan(0, 0, 0, 0, rewindTimerInterval);
            rewindTimer.Tick += rewindTimerTick;
            rewindTimerInterval = 1;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var library = Windows.Storage.KnownFolders.VideosLibrary;
            var queryOptions = new Windows.Storage.Search.QueryOptions();
            queryOptions.FolderDepth = Windows.Storage.Search.FolderDepth.Deep;
            queryOptions.IndexerOption = Windows.Storage.Search.IndexerOption.UseIndexerWhenAvailable;

            videoMediaElement.Width = Window.Current.Bounds.Width - 300;

            var fileQuery = library.CreateFileQueryWithOptions(queryOptions);

            var fif = new Windows.Storage.BulkAccess.FileInformationFactory(fileQuery,
                Windows.Storage.FileProperties.ThumbnailMode.VideosView, 190,
                Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale, false);

            var dataSource = fif.GetVirtualizedFilesVector();
            string[] displayColumns = CachedParameter.selectedCutup.displayColumns;
            var template = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""> <Grid VerticalAlignment =""Center""> <Grid.ColumnDefinitions> @ </Grid.ColumnDefinitions> % </Grid> </DataTemplate>";
            string columnDefinitions = "";
            string rowText = "";
            if (displayColumns != null)
            {
                for (int i = 0; i < displayColumns.Length; i++)
                {
                    ColumnDefinition col = new ColumnDefinition();
                    col.Width = new GridLength(130);
                    gridHeaders.ColumnDefinitions.Add(col);
                    if (i != displayColumns.Length - 1)
                    {
                        columnDefinitions += @"<ColumnDefinition Width=""130"" /> ";
                    }
                    else
                    {
                        columnDefinitions += @"<ColumnDefinition Width=""130"" /> ";
                    }
                    rowText = rowText + @"<TextBlock  Grid.Column=""X"" HorizontalAlignment = ""Center"" TextWrapping=""NoWrap"" VerticalAlignment=""Center"" Text =""{Binding Path=breakDownData[X]}""/>".Replace("X", i.ToString());
                    Border b = new Border();
                    b.BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0, 0, 0));
                    b.BorderThickness = new Thickness(0, 0, 1, 0);
                    TextBlock t = new TextBlock();
                    t.Text = displayColumns[i];
                    b.SetValue(Grid.RowProperty, 0);
                    b.SetValue(Grid.ColumnProperty, i);
                    t.Style = (Style)Application.Current.Resources["VideoPlayer_TextBlockStyle_GridHeader"];
                    t.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                    
                    t.Tag = i;
                    t.PointerPressed += columnHeaderClick;

                    b.Child = t;
                    gridHeaders.Children.Add(b);
                }
            }
            template = template.Replace("@", columnDefinitions).Replace("%", rowText);

            var dt = (DataTemplate)XamlReader.Load(template);
            FilteredClips.ItemTemplate = dt;
            //btnExpandGrid_Click(null, null);

            VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
            vm.listView = FilteredClips;
            vm.SortFilterPopupControl = SortFilterPopup;
            FilteredClips.SelectedIndex = 0;
        }

        private void columnHeaderClick(object sender, PointerRoutedEventArgs e)
        {
            int id = (int) ((TextBlock)sender).Tag;

            if (id != 0)
            {
                VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                vm.PrepareSortFilterPopup(id);

                if (!SortFilterPopup.IsOpen)
                {
                    RootPopupBorder.Width = 646;
                    SortFilterPopup.HorizontalOffset = Window.Current.Bounds.Width - 646;

                    SortFilterPopup.IsOpen = true;
                }
            }
        }

        private void closeSettingsPopupClicked(object sender, RoutedEventArgs e)
        {
            if (SortFilterPopup.IsOpen) { SortFilterPopup.IsOpen = false; }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            
            gridHeaders.Children.Clear();
        }
        private void scrollHeaders(object sender, ScrollViewerViewChangedEventArgs e)
        {
            gridHeaderScroll.ScrollToHorizontalOffset(gridScroll.HorizontalOffset);
        }

        private void scrollGrid(object sender, ScrollViewerViewChangedEventArgs e)
        {
            gridScroll.ScrollToHorizontalOffset(gridHeaderScroll.HorizontalOffset);
        }

        void gridHeaders_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            if (initialPoint.Y - currentPoint.Y >= 50 && isGridCollapsed)
                btnExpandGrid_Click(null, null);
            else if (initialPoint.Y - currentPoint.Y <= -50 && !isGridCollapsed)
            {
                var currentViewState = ApplicationView.Value;
                if (currentViewState == ApplicationViewState.Filled)
                {
                    FullscreenToggle();
                }
                else
                {
                    btnCollapseGrid_Click(null, null);
                }
            }
            else if (initialPoint.Y - currentPoint.Y <= -50 && isGridCollapsed)
            {
                FullscreenToggle();
            }
        }

        void gridHeaders_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            currentPoint = e.Position;
            gridScroll.ScrollToHorizontalOffset(gridScroll.HorizontalOffset + (initialPoint.X - currentPoint.X));
        }

        void gridHeaders_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            initialPoint = e.Position;
        }

        void videoMediaElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            currentPoint = e.Position;
            if (e.Delta.Scale >= 1.1 && !IsFullscreen)
            {
                FullscreenToggle();
            }
            else if (e.Delta.Scale <= .92 && IsFullscreen)
            {
                FullscreenToggle();
            }
        }

        void videoMediaElement_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            initialPoint = e.Position;
        }

        private void videoMediaElement_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            if (initialPoint.Y - currentPoint.Y >= 50 && IsFullscreen && (initialPoint.Y >= Window.Current.Bounds.Height - 500))
            {
                FullscreenToggle();
            }
        }

        private void VideosList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rightClicked)
            {
                ListView listView = (ListView)sender;
                VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                vm.SetClip((Clip)listView.SelectedItem);
                
                rightClicked = false;
            }
            else if (itemClicked)
            {
                itemClicked = false;
            }
            else
            {
                gridScroll.ScrollToVerticalOffset((FilteredClips.SelectedIndex) * 39);
            }
        }

        private void FullscreenToggle()
        {
            this.IsFullscreen = !this.IsFullscreen;

            if (this.IsFullscreen)
            {
                background = RootGrid.Background;
                RootGrid.Background = new SolidColorBrush();
                dataPanel.Background = new SolidColorBrush();
                // Hide all non full screen controls
                header.Visibility = Visibility.Collapsed;
                header.UpdateLayout();
                gridScroll.Visibility = Visibility.Collapsed;
                FilteredClips.UpdateLayout();
                TransportControlsPanel_Left.Visibility = Visibility.Collapsed;
                TransportControlsPanel_Right.Visibility = Visibility.Collapsed;
                gridHeaderScroll.Visibility = Visibility.Collapsed;

                // Show the full screen controls
                full_mainGrid.Visibility = Visibility.Visible;

                // Save the video containers size
                _previousVideoContainerSize.Width = videoContainer.ActualWidth;
                _previousVideoContainerSize.Height = videoContainer.ActualHeight;
                _previousVideoSize.Width = Window.Current.Bounds.Width - 300;
                _previousVideoSize.Height = videoMediaElement.ActualHeight;

                // Set the video container to fullscreen
                videoContainer.Width = Window.Current.Bounds.Width;
                videoContainer.Height = Window.Current.Bounds.Height;
                videoMediaElement.Width = Window.Current.Bounds.Width;
                videoMediaElement.Height = Window.Current.Bounds.Height;
                VideoGrid.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                RootGrid.Background = background;
                // Show the non full screen controls
                header.Visibility = Visibility.Visible;
                gridScroll.Visibility = Visibility.Visible;
                TransportControlsPanel_Left.Visibility = Visibility.Visible;
                TransportControlsPanel_Right.Visibility = Visibility.Visible;
                gridHeaderScroll.Visibility = Visibility.Visible;

                // Hide the full screen controls
                full_mainGrid.Visibility = Visibility.Collapsed;

                // Reset the video container to it's original height
                videoContainer.Width = Window.Current.Bounds.Width;
                videoContainer.Height = _previousVideoContainerSize.Height;
                videoMediaElement.Width = Window.Current.Bounds.Width - 300;
                videoMediaElement.Height = _previousVideoSize.Height;

                var currentViewState = ApplicationView.Value;
                if (currentViewState == ApplicationViewState.Filled && isGridCollapsed)
                {
                    btnExpandGrid_Click(null, null);
                }
                
                VideoGrid.Margin = new Thickness(0, 70, 0, 0);
            }
        }

        private void btnFullScreenToggle_Click(object sender, RoutedEventArgs e)
        {
            FullscreenToggle();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            rewindTimer.Stop();
            if (videoMediaElement.DefaultPlaybackRate != 1)
            {
                videoMediaElement.DefaultPlaybackRate = 1.0;
                videoMediaElement.PlaybackRate = 1.0;
            }

            videoMediaElement.Play();

            // Here we need to collapse and expand both full and non full screen buttons
            setPauseVisible();
            setStopVisibile();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            videoMediaElement.Pause();
            // Here we need to collapse and expand both full and non full screen buttons
            setPlayVisible();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            videoMediaElement.Stop();
            setPlayVisible();
            setPrevVisible();
        }

        private void btnFastForward_Click(object sender, RoutedEventArgs e)
        {
            videoMediaElement.DefaultPlaybackRate = 2.0;
            videoMediaElement.Play();
            
            setPauseVisible();
            setStopVisibile();
        }
        
        private void btnFastReverse_Click(object sender, RoutedEventArgs e)
        {
            rewindPositionChange = 30;
            
            videoMediaElement.Pause();
            rewindTimer.Start();
        }

        private void btnSlowReverse_Click(object sender, RoutedEventArgs e)
        {
            rewindPositionChange = 8;

            videoMediaElement.Pause();
            rewindTimer.Start();
        }

        private void btnSlowForward_Click(object sender, RoutedEventArgs e)
        {
            videoMediaElement.DefaultPlaybackRate = 0.5;
            videoMediaElement.Play();
            
            setPauseVisible();
            setStopVisibile();
        }

        private void setPlayVisible()
        {
            btnPause.Visibility = Visibility.Collapsed;
            btnPlay.Visibility = Visibility.Visible;

            full_btnPause.Visibility = Visibility.Collapsed;
            full_btnPlay.Visibility = Visibility.Visible;

            snapped_btnPause.Visibility = Visibility.Collapsed;
            snapped_btnPlay.Visibility = Visibility.Visible;
        }

        private void VideoPage_KeyUp(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (IsFullscreen && e.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                FullscreenToggle();
            }
            else
            {
                keyPressTimer.Stop();
                if (keyPressTimer.ElapsedMilliseconds < keyPressLength)
                {
                    if (e.VirtualKey == Windows.System.VirtualKey.Down)
                    {
                        if (isPaused)
                        {
                            btnPlay_Click(null, null);
                        }
                        else
                        {
                            btnPause_Click(null, null);
                        }
                        e.Handled = true;
                    }
                    else if (e.VirtualKey == Windows.System.VirtualKey.Up)
                    {
                        rewindTimer.Stop();
                        VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                        vm.ResetClip();
                        e.Handled = true;
                    }
                    else if (e.VirtualKey == Windows.System.VirtualKey.Right || e.VirtualKey == Windows.System.VirtualKey.PageDown)
                    {
                        VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                        vm.GoToNextClip();
                        e.Handled = true;
                    }
                    else if (e.VirtualKey == Windows.System.VirtualKey.Left || e.VirtualKey == Windows.System.VirtualKey.PageUp)
                    {
                        rewindTimer.Stop();
                        VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                        vm.GoToPreviousClip();
                        e.Handled = true;
                    }
                }
                else
                {
                    if (isStopped)
                    {
                        setPlayVisible();
                        isStopped = false;
                    }
                    else
                    {
                        btnPlay_Click(null, null);
                    }
                }
                keyPressTimer.Reset();
            }
        }

        private void VideoPage_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Down)
            {
                isPaused = btnPause.Visibility == Visibility.Visible ? false : true;
                btnSlowForward_Click(null, null);
                keyPressTimer.Start();
                e.Handled = true;
            }
            else if (e.VirtualKey == Windows.System.VirtualKey.Up)
            {
                btnSlowReverse_Click(null, null);
                keyPressTimer.Start();
                e.Handled = true;
            }
            else if (e.VirtualKey == Windows.System.VirtualKey.Right || e.VirtualKey == Windows.System.VirtualKey.PageDown)
            {
                btnFastForward_Click(null, null);
                keyPressTimer.Start();
                e.Handled = true;
            }
            else if (e.VirtualKey == Windows.System.VirtualKey.Left || e.VirtualKey == Windows.System.VirtualKey.PageUp)
            {
                btnFastReverse_Click(null, null);
                keyPressTimer.Start();
                e.Handled = true;
            }
        }

        void videoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            videoMediaElement.DefaultPlaybackRate = 1.0;
            videoMediaElement.PlaybackRate = 1.0;
            setPauseVisible();
            setStopVisibile();
        }

        void videoMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (videoMediaElement.Position.Seconds < 3)
            {
                videoMediaElement.Stop();
                isStopped = true;
            }
            else
            {
                setPlayVisible();
                setPrevVisible();

                VideoPlayerViewModel vm = (VideoPlayerViewModel)this.DataContext;
                vm.NextClip(NextAngleEvent.mediaEnded);
            }
        }

        private void videoMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            string hr = GetHresultFromErrorMessage(e);
        }

        private string GetHresultFromErrorMessage(ExceptionRoutedEventArgs e)
        {
            String hr = String.Empty;
            String token = "HRESULT - ";
            const int hrLength = 10; 

            int tokenPos = e.ErrorMessage.IndexOf(token, StringComparison.Ordinal);
            if (tokenPos != -1)
            {
                hr = e.ErrorMessage.Substring(tokenPos + token.Length, hrLength);
            }

            return hr;
        }

        private void videoMediaElement_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if (btnPlay.Visibility == Visibility.Collapsed)
            {
                btnPause_Click(null, null);
            }
            else
            {
                btnPlay_Click(null, null);
            }
        }

        private void setPauseVisible()
        {
            btnPlay.Visibility = Visibility.Collapsed;
            btnPause.Visibility = Visibility.Visible;

            full_btnPlay.Visibility = Visibility.Collapsed;
            full_btnPause.Visibility = Visibility.Visible;

            snapped_btnPlay.Visibility = Visibility.Collapsed;
            snapped_btnPause.Visibility = Visibility.Visible;
        }

        private void setStopVisibile()
        {
            btnReverse.Visibility = Visibility.Collapsed;
            btnStop.Visibility = Visibility.Visible;

            full_btnReverse.Visibility = Visibility.Collapsed;
            full_btnStop.Visibility = Visibility.Visible;

            snapped_btnReverse.Visibility = Visibility.Collapsed;
            snapped_btnStop.Visibility = Visibility.Visible;
        }

        private void setPrevVisible()
        {
            btnStop.Visibility = Visibility.Collapsed;
            btnReverse.Visibility = Visibility.Visible;

            full_btnStop.Visibility = Visibility.Collapsed;
            full_btnReverse.Visibility = Visibility.Visible;

            snapped_btnStop.Visibility = Visibility.Collapsed;
            snapped_btnReverse.Visibility = Visibility.Visible;
        }

        private void btnExpandGrid_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb = (Storyboard)RootGrid.Resources["ExpandGridAnimation"];
            sb.Begin();
            isGridCollapsed = false;
        }

        private void btnCollapseGrid_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb = (Storyboard)RootGrid.Resources["CollapseGridAnimation"];
            sb.Begin();
            isGridCollapsed = true;
        }

        private void ListViewItemPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            rightClicked = true;
            e.Handled = true;
        }
        private void ListViewItemClicked(object sender, ItemClickEventArgs e)
        {
            itemClicked = true;
        }

        void rewindTimerTick(object sender, object e)
        {
            if (videoMediaElement.Position.TotalMilliseconds > rewindPositionChange)
            {
                videoMediaElement.Position = videoMediaElement.Position.Subtract(new TimeSpan(0, 0, 0, 0, rewindPositionChange));
            }
            else
            {
                videoMediaElement.Position = videoMediaElement.Position.Subtract(new TimeSpan(0, 0, 0, 0, Convert.ToInt32(videoMediaElement.Position.TotalMilliseconds)));
                rewindTimer.Stop();
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            snapped_mainGrid.Visibility = Visibility.Collapsed;
            videoMediaElement.ControlPanel.Visibility = Visibility.Visible;
            var currentViewState = ApplicationView.Value;
            if (currentViewState == ApplicationViewState.Filled)
            {
                if (IsFullscreen)
                {
                    videoContainer.Width = Window.Current.Bounds.Width;
                    videoContainer.Height = Window.Current.Bounds.Height;
                    videoMediaElement.Width = Window.Current.Bounds.Width;
                    videoMediaElement.Height = Window.Current.Bounds.Height;
                    VideoGrid.Margin = new Thickness(0, 0, 0, 0);
                    full_mainGrid.Visibility = Visibility.Visible;
                }
                else if (isGridCollapsed)
                {
                    btnExpandGrid_Click(null, null);
                    videoContainer.Width = Window.Current.Bounds.Width - 300;
                    videoMediaElement.Width = Window.Current.Bounds.Width - 300;
                }
                else
                {
                    videoContainer.Width = Window.Current.Bounds.Width;
                    videoMediaElement.Width = Window.Current.Bounds.Width - 300;
                }

                if (!previousVideoStateIsFullScreen && previousStateIsSnapped)
                {
                    FullscreenToggle();
                }
                previousStateIsSnapped = false;
                previousVideoStateIsFullScreen = false;
            }
            else if (currentViewState == ApplicationViewState.FullScreenLandscape)
            {
                if (IsFullscreen)
                {
                    videoContainer.Width = Window.Current.Bounds.Width;
                    videoContainer.Height = Window.Current.Bounds.Height;
                    videoMediaElement.Width = Window.Current.Bounds.Width;
                    videoMediaElement.Height = Window.Current.Bounds.Height;
                    VideoGrid.Margin = new Thickness(0, 0, 0, 0);
                    full_mainGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    videoContainer.Width = Window.Current.Bounds.Width;
                    videoMediaElement.Width = Window.Current.Bounds.Width - 300;
                }
                if (!previousVideoStateIsFullScreen && previousStateIsSnapped)
                {
                    FullscreenToggle();
                }
                previousStateIsSnapped = false;
                previousVideoStateIsFullScreen = false;
            }
            else if (currentViewState == ApplicationViewState.Snapped)
            {
                if (IsFullscreen)
                {
                    previousVideoStateIsFullScreen = true;
                }
                else
                {
                    _previousVideoContainerSize.Width = Window.Current.Bounds.Width;
                    _previousVideoContainerSize.Height = 350;
                    _previousVideoSize.Width = Window.Current.Bounds.Width - 300;
                    _previousVideoSize.Height = 350;
                    background = RootGrid.Background;
                }

                previousStateIsSnapped = true;
                this.IsFullscreen = true;

                RootGrid.Background = new SolidColorBrush();
                dataPanel.Background = new SolidColorBrush();
                header.Visibility = Visibility.Collapsed;
                header.UpdateLayout();
                gridScroll.Visibility = Visibility.Collapsed;
                FilteredClips.UpdateLayout();
                TransportControlsPanel_Left.Visibility = Visibility.Collapsed;
                TransportControlsPanel_Right.Visibility = Visibility.Collapsed;
                gridHeaderScroll.Visibility = Visibility.Collapsed;
                snapped_mainGrid.Visibility = Visibility.Visible;
                videoMediaElement.ControlPanel.Visibility = Visibility.Collapsed;
                full_mainGrid.Visibility = Visibility.Collapsed;

                videoContainer.Width = Window.Current.Bounds.Width;
                videoContainer.Height = Window.Current.Bounds.Height;
                videoMediaElement.Width = Window.Current.Bounds.Width;
                videoMediaElement.Height = Window.Current.Bounds.Height;
                VideoGrid.Margin = new Thickness(0, 0, 0, 0);
            }
        }
    }
}