using Citations_365.Common;
using Citations_365.Controllers;
using Citations_365.Models;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;


namespace Citations_365.Views {
    public sealed partial class AuthorsPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        
        public ObservableDictionary DefaultViewModel {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper est utilisé sur chaque page pour faciliter la navigation et 
        /// gestion de la durée de vie des processus
        /// </summary>
        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

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

            // Configurer l'assistant de navigation
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            // Configurer les composants de navigation de page logique qui permettent
            // la page pour afficher un seul volet à la fois.
            this.navigationHelper.GoBackCommand = new Citations_365.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            this.authorsListView.SelectionChanged += itemListView_SelectionChanged;

            // Commencer à écouter les modifications de la taille de la fenêtre 
            // pour passer de l'affichage de deux volets à l'affichage d'un volet
            Window.Current.SizeChanged += Window_SizeChanged;
            this.InvalidateVisualState();
        }

        void itemListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.UsingLogicalPageNavigation()) {
                this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Remplit la page à l'aide du contenu passé lors de la navigation. Tout état enregistré est également
        /// fourni lorsqu'une page est recréée à partir d'une session antérieure.
        /// </summary>
        /// <param name="sender">
        /// Source de l'événement ; en général <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Données d'événement qui fournissent le paramètre de navigation transmis à
        /// <see cref="Frame.Navigate(Type, Object)"/> lors de la requête initiale de cette page et
        /// un dictionnaire d'état conservé par cette page durant une session
        /// antérieure.  L'état n'aura pas la valeur Null lors de la première visite de la page.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            // TODO: affectez un groupe pouvant être lié à Me.DefaultViewModel("Group")
            // TODO: affectez une collection d'éléments pouvant être liés à Me.DefaultViewModel("Items")

            if (e.PageState == null) {
                // Quand il s'agit d'une nouvelle page, sélectionne automatiquement le premier élément, sauf si la navigation
                // de page logique est utilisée (voir #region navigation de page logique ci-dessous.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null) {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }

                PopulatePageView(e.NavigationParameter);

            } else {
                // Restaure l'état précédemment enregistré associé à cette page
                if (e.PageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null) {
                    // TODO: appelez Me.itemsViewSource.View.MoveCurrentTo() avec l'élément
                    //       sélectionné, comme spécifié par la valeur de pageState("SelectedItem")
                }
            }
        }

        /// <summary>
        /// Conserve l'état associé à cette page en cas de suspension de l'application ou de
        /// suppression de la page du cache de navigation. Les valeurs doivent être conformes aux conditions de
        /// exigences de <see cref="Common.SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">Source de l'événement ; en général <see cref="Common.NavigationHelper"/></param>
        /// <param name="e">Données d'événement qui fournissent un dictionnaire vide à remplir à l'aide de
        /// état sérialisable.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e) {
            if (this.itemsViewSource.View != null) {
                // TODO: dérivez un paramètre de navigation sérialisable et assignez-le à la valeur
                //       la valeur de pageState("SelectedItem")

            }
        }

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

            PopulateAuthorsList();
            PopulatePageTitle(name);
            PopulateDetailView(url, imageLink);
        }

        private void PopulateAuthorsList() {
            authorsListView.ItemsSource = AuthorsController.AuthorsCollection;
        }

        private void PopulatePageTitle(string title) {
            pageTitle.Text = title;
        }

        private async void PopulateDetailView (string url, string imageLink) {
            if (imageLink.Length < 1) {
                imageLink = "ms-appx:///Assets/Icons/gray.png";
            }

            ShowAuthorBioLoadingIndicator();

            AuthorInfos infos = await DAuthorController.LoadData(url);

            HideAuthorBioLoadingIndicator();

            if (infos != null) {
                PopulateBio(infos, imageLink);
                ShowBio();
                PopulateQuotes();

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

            ViewBio.Visibility = Visibility.Visible;
            NoContentViewBio.Visibility = Visibility.Collapsed;
        }

        private void ShowNoBioView() {
            ViewBio.Visibility = Visibility.Collapsed;
            NoContentViewBio.Visibility = Visibility.Visible;
        }

        private void HideBio() {
            ViewBio.Visibility = Visibility.Collapsed;
            NoContentViewBio.Visibility = Visibility.Visible;
        }

        private void ShowAuthorBioLoadingIndicator() {
            RingLoadingAuthorBio.IsActive = true;
            RingLoadingAuthorBio.Visibility = Visibility.Visible;
            NoContentViewBio.Visibility = Visibility.Collapsed;
        }

        private void HideAuthorBioLoadingIndicator() {
            RingLoadingAuthorBio.IsActive = false;
            RingLoadingAuthorBio.Visibility = Visibility.Collapsed;
        }

        private void BindCollectionToView() {
            _isQuotesLoaded = true;

            ListAuthorQuotes.Visibility = Visibility.Visible;
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
                bool result = await DAuthorController.FetchQuotes();

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


        #region Navigation entre pages logiques

        // La page fractionnée est conçue de telle sorte que si la fenêtre ne dispose pas d'assez d'espace pour afficher
        // la liste et les détails, un seul volet s'affichera à la fois.
        //
        // Le tout est implémenté à l'aide d'une seule page physique pouvant représenter deux pages logiques.
        // Le code ci-dessous parvient à ce but sans que l'utilisateur ne se rende compte de
        // la distinction.

        private const int MinimumWidthForSupportingTwoPanes = 768;

        /// <summary>
        /// Invoqué pour déterminer si la page doit agir en tant qu'une ou deux pages logiques.
        /// </summary>
        /// <returns>True si la fenêtre doit agir en tant qu'une page logique, false
        /// .</returns>
        private bool UsingLogicalPageNavigation() {
            return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
        }

        /// <summary>
        /// Appelé avec la modification de la taille de la fenêtre
        /// </summary>
        /// <param name="sender">La fenêtre active</param>
        /// <param name="e">Données d'événement décrivant la nouvelle taille de la fenêtre</param>
        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e) {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Invoqué lorsqu'un élément d'une liste est sélectionné.
        /// </summary>
        /// <param name="sender">GridView qui affiche l'élément sélectionné.</param>
        /// <param name="e">Données d'événement décrivant la façon dont la sélection a été modifiée.</param>
        private void authorsListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Invalidez l'état d'affichage lorsque la navigation entre pages logiques est en cours, car une modification
            // apportée à la sélection pourrait entraîner la modification de la page logique active correspondante.  Lorsqu'un
            // élément est sélectionné, l'affichage passe de la liste d'éléments
            // aux détails concernant l'élément sélectionné.  Lorsque cet élément est désélectionné, l'effet inverse
            // est produit.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();

            ClearAuthorQuotes();

            ListView authorsList = (ListView)sender;
            Author author = (Author)authorsList.SelectedValue;
            PopulatePageView(author);
        }

        private void ClearAuthorQuotes() {
            _isQuotesLoaded = false;
            DAuthorController.AuthorQuotesCollection.Clear();
        }

        private bool CanGoBack() {
            if (this.UsingLogicalPageNavigation() && this.authorsListView.SelectedItem != null) {
                return true;
            } else {
                return this.navigationHelper.CanGoBack();
            }
        }
        private void GoBack() {
            if (this.UsingLogicalPageNavigation() && this.authorsListView.SelectedItem != null) {
                // Lorsque la navigation entre pages logiques est en cours et qu'un élément est sélectionné,
                // les détails de l'élément sont actuellement affichés.  La suppression de la sélection entraîne le retour à
                // la liste d'éléments.  Du point de vue de l'utilisateur, ceci est un état visuel précédent logique
                // de logique inversée.
                this.authorsListView.SelectedItem = null;
            } else {
                this.navigationHelper.GoBack();
            }
        }

        private void InvalidateVisualState() {
            var visualState = DetermineVisualState();
            VisualStateManager.GoToState(this, visualState, false);
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Invoqué pour déterminer le nom de l'état visuel correspondant à l'état d'affichage
        /// état d'affichage.
        /// </summary>
        /// <returns>Nom de l'état visuel désiré.  Il s'agit du même nom que celui
        /// de l'état d'affichage, sauf si un élément est sélectionné dans l'affichage Portrait ou Snapped où
        /// cette page logique supplémentaire est représentée par l'ajout du suffixe _Detail.</returns>
        private string DetermineVisualState() {
            if (!UsingLogicalPageNavigation())
                return "PrimaryView";

            // Modifiez l'état d'activation du bouton Précédent lorsque l'état d'affichage est modifié
            var logicalPageBack = this.UsingLogicalPageNavigation() && this.authorsListView.SelectedItem != null;

            return logicalPageBack ? "SinglePane_Detail" : "SinglePane";
        }

        #endregion

        #region Inscription de NavigationHelper

        /// Les méthodes fournies dans cette section sont utilisées simplement pour permettre
        /// NavigationHelper pour répondre aux méthodes de navigation de la page.
        /// 
        /// La logique spécifique à la page doit être placée dans les gestionnaires d'événements pour  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// et <see cref="Common.NavigationHelper.SaveState"/>.
        /// Le paramètre de navigation est disponible dans la méthode LoadState 
        /// en plus de l'état de page conservé durant une session antérieure.

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
