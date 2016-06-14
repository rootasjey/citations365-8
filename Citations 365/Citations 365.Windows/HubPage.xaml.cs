using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Citations_365.Data;
using Citations_365.Common;
using Citations_365.Controllers;
using Citations_365.Models;
using Citations_365.Views;

namespace Citations_365 {
    public sealed partial class HubPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        #region today_var
        private static TodayController _Tcontroller;
        public static TodayController Tcontroller {
            get {
                if (_Tcontroller == null) {
                    _Tcontroller = new TodayController();
                }
                return _Tcontroller;
            }
        }
        #endregion today_var

        #region favorites_var
        private static FavoritesController _FController;
        public static FavoritesController FController {
            get {
                if (_FController == null) {
                    _FController = new FavoritesController();
                }
                return _FController;
            }
        }
        #endregion favorites_var

        #region search_var
        private static SearchController _Scontroller;
        public static SearchController Scontroller {
            get {
                if (_Scontroller == null) {
                    _Scontroller = new SearchController();
                }
                return _Scontroller;
            }
        }

        private static IDictionary<string, string> _tips =
            new Dictionary<string, string>();

        private static IDictionary<string, string> _infos =
            new Dictionary<string, string>();

        /// <summary>
        /// Avoid running multiple search calls
        /// </summary>
        private bool _performingSearch = false;

        private static ListView _searchResultsList { get; set; }
        private StackPanel _noContentSearchView { get; set; }
        private TextBox _searchInput { get; set; }

        #endregion search_var

        #region authors_var
        private static AuthorsController _authorController;
        public static AuthorsController AuthorsController {
            get {
                if (_authorController == null) {
                    _authorController = new AuthorsController();
                }
                return _authorController;
            }
        }
        #endregion authors_var

        /// <summary>
        /// Obtient le NavigationHelper utilisé pour faciliter la navigation et la gestion de la durée de vie de processus.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Obtient le DefaultViewModel. Cela peut être remplacé par un modèle d'affichage fortement typé.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public HubPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            //this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
        }

        #region nav_helper
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: créez un modèle de données approprié pour le domaine posant problème pour remplacer les exemples de données
            //var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-4");
            //this.DefaultViewModel["Section3Items"] = sampleDataGroup;
        }

        void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e)
        {
            HubSection section = e.Section;
            var group = section.DataContext;
            //this.Frame.Navigate(typeof(SectionPage), ((SampleDataGroup)group).UniqueId);

            if (section.Name == "SearchSection") {
                ToggleSearchView();
            }
        }

        private void ToggleSearchView() {
            if (_searchResultsList.Visibility == Visibility.Visible) {
                ShowSearchInput();

            } else {
                ShowSearchResults();
            }
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void FavoritesQuotes_Loaded(object sender, RoutedEventArgs e) {
            PopulateFavoritesQuote();
        }

        #endregion nav_helper


        /* ******
         * QUOTES
         * ******
         */
        #region quotes
        private void PopulateHeroQuote() {
            Quote quote = TodayController.TodayCollection[0];

            TextBlock HeroQuoteContent = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteContent");
            TextBlock HeroQuoteAuthor = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteAuthor");
            TextBlock HeroQuoteRef = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteRef");

            HeroQuoteContent.Text = quote.Content;
            HeroQuoteAuthor.Text = quote.Author;
            HeroQuoteRef.Text = quote.Reference;
        }

        private async void PopulateTodayQuotes() {
            ShowLoadingQuotesIndicator();

            await Tcontroller.LoadData();
            BindCollectionToTodayView();

            if (!Tcontroller.IsDataLoaded()) {
                ShowTodayNoContentViews();
                return;
            }

            PopulateHeroQuote();

            HideLoadingQuotesIndicator();
        }

        private void ShowTodayNoContentViews() {
            StackPanel NoContentHeroView = Controller.FindChildControl<StackPanel>(HeroSection, "NoContentHeroView") as StackPanel;
            NoContentHeroView.Visibility = Visibility.Visible;

            StackPanel NoContentTodayView = Controller.FindChildControl<StackPanel>(RecentSection, "NoContentTodayView") as StackPanel;
            NoContentTodayView.Visibility = Visibility.Visible;
        }

        private void BindCollectionToTodayView() {
            ListView todayList = Controller.FindChildControl<ListView>(RecentSection, "ListQuotes") as ListView;
            StackPanel NoContentTodayView = Controller.FindChildControl<StackPanel>(RecentSection, "NoContentTodayView") as StackPanel;

            if (Tcontroller.IsDataLoaded()) {
                todayList.ItemsSource = TodayController.TodayCollection;
                todayList.Visibility = Visibility.Visible;
                NoContentTodayView.Visibility = Visibility.Collapsed;

                //Controller.UpdateTile(TodayController.TodayCollection[0]);

            } else {
                todayList.Visibility = Visibility.Collapsed;
                NoContentTodayView.Visibility = Visibility.Visible;
            }
        }

        private async void PopulateFavoritesQuote() {
            await FavoritesController.Initialize();
            BindCollectionToFavoritesView();
        }

        private void BindCollectionToFavoritesView() {
            if (FavoritesController.IsDataLoaded() && FavoritesController.FavoritesCollection.Count > 0) {

                ListView favoritesList = 
                    Controller.FindChildControl<ListView>(FavoritesSection, "FavoritesQuotes") as ListView;
                StackPanel NoContentFavoritesView = 
                    Controller.FindChildControl<StackPanel>(FavoritesSection, "NoContentFavoritesView") as StackPanel;
                
                favoritesList.ItemsSource = FavoritesController.FavoritesCollection;
                favoritesList.Visibility = Visibility.Visible;
                NoContentFavoritesView.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowLoadingQuotesIndicator() {
            //ViewLoadingQuote.Visibility = Visibility.Visible;
            //RingLoadingQuotes.IsActive = true;
            //RingLoadingQuotes.Visibility = Visibility.Visible;
        }

        private void HideLoadingQuotesIndicator() {
            //ViewLoadingQuote.Visibility = Visibility.Collapsed;
            //RingLoadingQuotes.IsActive = false;
            //RingLoadingQuotes.Visibility = Visibility.Collapsed;
        }


        /* **************
        * EVENTS HANDLERS
        * ***************
        */
        private async void Favorite_Tapped(object sender, TappedRoutedEventArgs e) {
            FontIcon icon = (FontIcon)sender;
            Quote quote = (Quote)icon.DataContext;

            if (FavoritesController.IsFavorite(quote.Link)) {
                // Remove from favorites
                bool result = await FavoritesController.RemoveFavorite(quote);
                if (result) {
                    quote.IsFavorite = false;
                }
            } else {
                // Add to favorites
                bool result = await FavoritesController.AddFavorite(quote);
                if (result) {
                    quote.IsFavorite = true;
                }
            }
        }

        private void Quote_Tapped(object sender, TappedRoutedEventArgs e) {
            StackPanel panel = (StackPanel)sender;
            Quote quote = (Quote)panel.DataContext;

            if (quote.AuthorLink != null && quote.AuthorLink.Length > 0) {
                Frame.Navigate(typeof(AuthorsPage), quote);
            }
        }

        private void Authors_Tapped(object sender, TappedRoutedEventArgs e) {
            StackPanel panel = (StackPanel)sender;
            Author author = (Author)panel.DataContext;

            Frame.Navigate(typeof(AuthorsPage), author);
        }

        #endregion quotes

        /* ******
         * SEARCH
         * ******
         */
        #region search

        private ListView GetListSearchResults() {
            if (_searchResultsList == null) {
                _searchResultsList = Controller.FindChildControl<ListView>(SearchSection, "SearchQuotes") as ListView;
            }
            return _searchResultsList;
        }

        private StackPanel GetNoContentSearchView() {
            if (_noContentSearchView == null) {
                _noContentSearchView = Controller.FindChildControl<StackPanel>(SearchSection, "NoContentSearchView") as StackPanel;
            }
            return _noContentSearchView;
        }

        private TextBox GetSearchInput() {
            if (_searchInput == null) {
                _searchInput = Controller.FindChildControl<TextBox>(SearchSection, "InputSearch") as TextBox;
            }
            return _searchInput;
        }

        private async void RunSearch(string query) {
            ShowLoadingSearchScreen();
            bool result = await Scontroller.Search(query);

            if (result) {
                ShowSearchResults();
                BinCollectionToSearchView();

            } else {
                GetNoContentSearchView().Visibility = Visibility.Visible;
                GetListSearchResults().Visibility = Visibility.Collapsed;
                //TextInfos.Text = _infos["searchFailed"];
            }

            _performingSearch = false;
        }

        private void ClearSearchInput() {
            GetSearchInput().Text = "";
        }

        private void ShowLoadingSearchScreen() {
            GetListSearchResults().Visibility = Visibility.Collapsed;
            GetSearchInput().Visibility = Visibility.Collapsed;
            //TextInfos.Text = _infos["searching"];
        }

        private void HideLoadingSearchScreen() {
            GetListSearchResults().Visibility = Visibility.Visible;
            GetSearchInput().Visibility = Visibility.Visible;
        }

        private void ShowSearchInput() {
            if (GetNoContentSearchView().Visibility == Visibility.Collapsed) {
                GetListSearchResults().Visibility = Visibility.Collapsed;
                GetNoContentSearchView().Visibility = Visibility.Visible;
                //TextInfos.Text = _tips["default"];
                GetSearchInput().Visibility = Visibility.Visible;
            }
        }

        private void ShowSearchResults() {
            bool alreadyVisible =
                GetNoContentSearchView().Visibility == Visibility.Collapsed &&
                GetListSearchResults().Visibility == Visibility.Visible;
            bool noResults = SearchController.SearchCollection.Count < 1;

            if (alreadyVisible || noResults) {
                return;
            }

            GetNoContentSearchView().Visibility = Visibility.Collapsed;
            GetListSearchResults().Visibility = Visibility.Visible;
        }

        private void BinCollectionToSearchView() {
            GetListSearchResults().ItemsSource = SearchController.SearchCollection;
        }

        private void InputSearch_KeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter && !_performingSearch) {
                _performingSearch = true;

                string query = GetSearchInput().Text;
                RunSearch(query);
            }
        }
        #endregion search


        /* *******
         * AUTHORS
         * *******
         */
        #region authors
        private async void PopulateAuthors() {
            bool result = await AuthorsController.LoadData();

            if (result) {
                BindCollectionToAuthorsView();
            }
        }

        private void BindCollectionToAuthorsView() {
            ListView authorsGrid = Controller.FindChildControl<ListView>(AuthorsSection, "AuthorsGrid") as ListView;
            authorsGrid.ItemsSource = AuthorsController.AuthorsCollection;
        }

        private void AuthorsGrid_Loaded(object sender, RoutedEventArgs e) {
            PopulateAuthors();
        }

        private void RecentSection_Loaded(object sender, RoutedEventArgs e) {
            PopulateTodayQuotes();
        }

        #endregion authors
    }
}