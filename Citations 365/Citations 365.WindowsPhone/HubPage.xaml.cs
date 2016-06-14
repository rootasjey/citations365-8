using Citations_365.Common;
using Citations_365.Controllers;
using Citations_365.Data;
using Citations_365.Models;
using Citations_365.Views;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;


namespace Citations_365 {
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

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

        public HubPage()
        {
            this.InitializeComponent();

            // Hub est pris en charge uniquement en mode Portrait
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Obtient le <see cref="NavigationHelper"/> associé à ce <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Obtient le modèle d'affichage pour ce <see cref="Page"/>.
        /// Cela peut être remplacé par un modèle d'affichage fortement typé.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Remplit la page à l'aide du contenu passé lors de la navigation. Tout état enregistré est également
        /// fourni lorsqu'une page est recréée à partir d'une session antérieure.
        /// </summary>
        /// <param name="sender">
        /// La source de l'événement ; en général <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Données d'événement qui fournissent le paramètre de navigation transmis à
        /// <see cref="Frame.Navigate(Type, object)"/> lors de la requête initiale de cette page et
        /// un dictionnaire d'état conservé par cette page durant une session
        /// antérieure.  L'état n'aura pas la valeur Null lors de la première visite de la page.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: créez un modèle de données approprié pour le domaine posant problème pour remplacer les exemples de données
            var sampleDataGroups = await SampleDataSource.GetGroupsAsync();
            this.DefaultViewModel["Groups"] = sampleDataGroups;
        }

        /// <summary>
        /// Conserve l'état associé à cette page en cas de suspension de l'application ou de
        /// suppression de la page du cache de navigation. Les valeurs doivent être conformes aux conditions de
        /// exigences en matière de sérialisation de <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">La source de l'événement ; en général <see cref="NavigationHelper"/></param>
        /// <param name="e">Données d'événement qui fournissent un dictionnaire vide à remplir à l'aide de l'
        /// état sérialisable.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: enregistrer l'état unique de la page ici.
        }

        /// <summary>
        /// Affiche les détails d'un groupe sur lequel l'utilisateur a cliqué dans <see cref="SectionPage"/>.
        /// </summary>
        /// <param name="sender">Source de l'événement de clic.</param>
        /// <param name="e">Détails sur l'événement de clic.</param>
        private void GroupSection_ItemClick(object sender, ItemClickEventArgs e)
        {
            var groupId = ((SampleDataGroup)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(SectionPage), groupId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        /// <summary>
        /// Affiche les détails d'un élément sur lequel l'utilisateur a cliqué dans <see cref="ItemPage"/>.
        /// </summary>
        /// <param name="sender">Source de l'événement de clic.</param>
        /// <param name="e">Détails sur l'événement de clic.</param>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(ItemPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        #region Inscription de NavigationHelper

        /// <summary>
        /// Les méthodes fournies dans cette section sont utilisées simplement pour permettre
        /// NavigationHelper pour répondre aux méthodes de navigation de la page.
        /// <para>
        /// La logique spécifique à la page doit être placée dans les gestionnaires d'événements pour le
        /// <see cref="NavigationHelper.LoadState"/>
        /// et <see cref="NavigationHelper.SaveState"/>.
        /// Le paramètre de navigation est disponible dans la méthode LoadState
        /// en plus de l'état de page conservé durant une session antérieure.
        /// </para>
        /// </summary>
        /// <param name="e">Données d'événement décrivant la manière dont l'utilisateur a accédé à cette page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion


        private void RecentSection_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            PopulateTodayQuotes();
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
                todayList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                NoContentTodayView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                //Controller.UpdateTile(TodayController.TodayCollection[0]);

            } else {
                todayList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                NoContentTodayView.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void PopulateHeroQuote() {
            Quote quote = TodayController.TodayCollection[0];

            TextBlock HeroQuoteContent = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteContent");
            TextBlock HeroQuoteAuthor = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteAuthor");
            TextBlock HeroQuoteRef = (TextBlock)Controller.FindChildControl<TextBlock>(HeroSection, "HeroQuoteRef");

            HeroQuoteContent.Text = quote.Content;
            HeroQuoteAuthor.Text = quote.Author;
            HeroQuoteRef.Text = quote.Reference;
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

        private void FavoritesQuotes_Loaded(object sender, RoutedEventArgs e) {
            PopulateFavoritesQuote();
        }

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

        private void Section1Header_Loaded(object sender, RoutedEventArgs e) {
            PopulateTodayQuotes();
        }

        #endregion authors
    }
}