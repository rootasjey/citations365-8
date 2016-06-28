using Citations_365.Common;
using Citations_365.Controllers;
using Citations_365.Models;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;


namespace Citations_365.Views {
    public sealed partial class AuthorsPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private static DetailAuthorController _dAuthorController;
        public static DetailAuthorController DAuthorController {
            get {
                if (_dAuthorController == null) {
                    _dAuthorController = new DetailAuthorController();
                }
                return _dAuthorController;
            }
        }
        private bool _isQuotesLoaded { get; set; }


        public AuthorsPage() {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Obtient le <see cref="NavigationHelper"/> associé à ce <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Obtient le modèle d'affichage pour ce <see cref="Page"/>.
        /// Cela peut être remplacé par un modèle d'affichage fortement typé.
        /// </summary>
        public ObservableDictionary DefaultViewModel {
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
        /// <see cref="Frame.Navigate(Type, Object)"/> lors de la requête initiale de cette page et
        /// un dictionnaire d'état conservé par cette page durant une session
        /// antérieure.  L'état n'aura pas la valeur Null lors de la première visite de la page.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            PopulatePageView(e.NavigationParameter);
        }

        /// <summary>
        /// Conserve l'état associé à cette page en cas de suspension de l'application ou de
        /// suppression de la page du cache de navigation. Les valeurs doivent être conformes aux conditions de
        /// exigences en matière de sérialisation de <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">La source de l'événement ; en général <see cref="NavigationHelper"/></param>
        /// <param name="e">Données d'événement qui fournissent un dictionnaire vide à remplir à l'aide de l'
        /// état sérialisable.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e) {
        }

        #region Inscription de NavigationHelper

        /// <summary>
        /// Les méthodes fournies dans cette section sont utilisées simplement pour permettre
        /// NavigationHelper pour répondre aux méthodes de navigation de la page.
        /// <para>
        /// La logique spécifique à la page doit être placée dans les gestionnaires d'événements pour  
        /// <see cref="NavigationHelper.LoadState"/>
        /// et <see cref="NavigationHelper.SaveState"/>.
        /// Le paramètre de navigation est disponible dans la méthode LoadState 
        /// en plus de l'état de page conservé durant une session antérieure.
        /// </para>
        /// </summary>
        /// <param name="e">Fournit des données pour les méthodes de navigation et
        /// les gestionnaires d'événements qui ne peuvent pas annuler la requête de navigation.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void PopulatePageView(object obj) {
            string name = "";
            string url = "";
            string imageLink = "";

            if (obj.GetType() == typeof(Author)) {
                Author author = (Author)obj;
                name = author.Name;
                url = author.Link;
                imageLink = author.ImageLink;

            } else if (obj.GetType() == typeof(Quote)) {
                Quote quote = (Quote)obj;
                name = quote.Author.Replace("De ", "");
                url = quote.AuthorLink;
            }

            PopulatePageTitle(name);
            PopulateDetailView(url, imageLink);
        }

        private void PopulatePageTitle(string title) {
            PageTitle.Text = title;
        }

        private async void PopulateDetailView(string url, string imageLink) {
            if (imageLink.Length < 1) {
                imageLink = "ms-appx:///Assets/Icons/gray.png";
            }

            ShowAuthorBioLoadingIndicator();

            AuthorInfos infos = await DAuthorController.LoadData(url);

            HideAuthorBioLoadingIndicator();

            if (infos != null) {
                PopulateBio(infos, imageLink);
                ShowBio();

            } else {
                ShowNoBioView();
            }
        }

        private void PopulateBio(AuthorInfos infos, string imageLink) {
            ContentBio.Text = infos.bio;
            LifeTime.Text = infos.birth + " - " + infos.death;
            Job.Text = infos.job;
            MainQuote.Text = infos.quote;
            AuthorImage.UriSource = new Uri(imageLink);
        }

        private void ShowBio() {
            if (ContentBio.Text.Length < 1) {
                ShowNoBioView();
                return;
            }

            ViewBio.Visibility = Windows.UI.Xaml.Visibility.Visible ;
            NoContentViewBio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void ShowNoBioView() {
            ViewBio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            NoContentViewBio.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void HideBio() {
            ViewBio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            NoContentViewBio.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void ShowAuthorBioLoadingIndicator() {
            RingLoadingAuthorBio.IsActive = true;
            RingLoadingAuthorBio.Visibility = Windows.UI.Xaml.Visibility.Visible;
            NoContentViewBio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        private void HideAuthorBioLoadingIndicator() {
            RingLoadingAuthorBio.IsActive = false;
            RingLoadingAuthorBio.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void HideAuthorQuotesLoadingIndicator() {
            RingLoadingAuthorQuotes.IsActive = false;
            RingLoadingAuthorQuotes.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void ShowAuthorQuotesLoadingIndicator() {
            RingLoadingAuthorQuotes.IsActive = true;
            RingLoadingAuthorQuotes.Visibility = Windows.UI.Xaml.Visibility.Visible;
            NoContentViewQuotes.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }
        
        private void ShowListQuotes() {
            NoContentViewQuotes.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ListAuthorQuotes.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void BindCollectionToView() {
            _isQuotesLoaded = true;

            ShowListQuotes();
            ListAuthorQuotes.Visibility = Windows.UI.Xaml.Visibility.Visible;
            ListAuthorQuotes.ItemsSource = DAuthorController.AuthorQuotesCollection;
        }

        private async void PopulateQuotes() {
            if (_isQuotesLoaded) {
                return;
            }

            if (DAuthorController.QuotesLoaded() && DAuthorController.isSameRequest()) {
                BindCollectionToView();
                return;
            }

            if (DAuthorController.HasQuotes()) {
                ShowAuthorQuotesLoadingIndicator();

                bool result = await DAuthorController.FetchQuotes();

                HideAuthorQuotesLoadingIndicator();

                if (result) {
                    BindCollectionToView();
                }
            }
        }

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

        private void PagePivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Pivot pivot = (Pivot)sender;
            if (pivot.SelectedIndex == 1) {
                PopulateQuotes();
            }
        }

        private void Share_Tapped(object sender, TappedRoutedEventArgs e) {
            FontIcon icon = (FontIcon)sender;
            Quote q = (Quote)icon.DataContext;
            Controller.share(q);
        }
    }
}
