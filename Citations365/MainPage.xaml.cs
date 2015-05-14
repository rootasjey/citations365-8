using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Citations365.ViewModels;
using Microsoft.Phone.Tasks;
using System.Collections.ObjectModel;
using Windows.Phone.Speech.Synthesis;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Media;
using Coding4Fun.Toolkit.Controls;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;

namespace Citations365
{
    public partial class MainPage : PhoneApplicationPage
    {
        // VARABLES
        #region Variables
        int _pageNumber = 1; // numéro de page citations principales
        int _queryNumber = 1; // numéro de page pour la recherche de citations
        int _numberOfItemsInAppBar = 3; // le nombre d'item qu'il doit y avoir dans l'ApplicationBar (varie si on a un fond dynamique)
        //string _authorLink = "";

        /// <summary>
        ///  On ne voulait pas afficher les détails d'une citation mais accèder à la fiche auteur
        /// </summary>
        bool _IDontWantPopup = false;

        /// <summary>
        /// Dernière citation obtenue (citations principales)
        /// si cette variable = le dernier élément de la liste
        /// on arrête la recherche
        /// </summary>
        string _lastQuoteContent = "";

        /// <summary>
        /// Dernière citation obtenue lors d'une recherche par mots clés
        /// si cette variable = le dernier élément de la liste de résultats
        /// on arrête la recherche
        /// </summary>
        string _lastQuoteResultsContent = "";

        /// <summary>
        /// Notify qu'il n'y a pas plus de résultat 
        /// pour une recherche de citations par mot clé
        /// </summary>
        bool _continueToSearch = false;

        SpeechSynthesizer _synth = new SpeechSynthesizer(); // synthétiseur de voix
        TextBlock _tblock;

        // ELEMENTS POUR LES DETAILS D'UNE CITATION
        #region detailsQuote

        // WrapPanel pour le détails d'une citation
        WrapPanel _wrapPanel = new WrapPanel()
        {
            Margin = new Thickness(12),
            Orientation= System.Windows.Controls.Orientation.Horizontal,
        };

        // Couleur pour les détails d'une citation
        SolidColorBrush _breakedWhiteColor = new SolidColorBrush()
        {
            Color = Color.FromArgb(255, 239, 239, 242),
        };

        SolidColorBrush _blackColor = new SolidColorBrush()
        {
            Color = Color.FromArgb(255, 0, 0, 0),
        };

        // Création d'un ScrollViewer
        // dans le cas où le la taille du contenu serait trop grand (>400px)
        ScrollViewer _scrollview = new ScrollViewer()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
        };
        #endregion detailsQuote

        // bool passant à true si on lance la méthode "Revenir en haut de la list"
        // -> permet d'éviter une exception produite par la méthode LongList_ItemRealized
        bool _BackToTop = false;
        // variable indiquant si un chargement est en train de s'effectuer (chargement de données)
        bool _IsLoading = false;
        bool _IsLoaded = false;
        bool _isLookingFor = false; // pour la recherche de citations
        bool _backEnabled = true; // décide si on peut appuyer sur la touche retour
        bool _letMyFocus = false; // garde le focus sur la TBox
        bool _slideToRightisVisible = false; // doit-on fermer l'animation de glisser vers la recherche de mots clés?

        // annule la recherche si on click sur le loupe du LongListSelector avant la fin de la recherche
        // dû au fait que la méthode de recherche de citation SearchQuote() est lancée 2x
        bool _cancelSearch = false; 

        // détermine la fonction sur le cercle (3è item du panorama) : recherche, auteurs, sujets, thémas
        // 0 recherche, -1 auteurs, 1 suggestion de sujet, 2 théma
        int _gridFunction = 0; 

        string _query = "";

        // Variable globale Quote pour récupérer les valeurs quand l'utilisateur sélectionne une citation
        // On pourra par la suite récupérer ces même valeurs pour la fonction DoubleTap de le LongListSelector
        // tBox variable pour récupérer les TextBlock des ItemTemplate
        Quote _myQuote = new Quote()
        {
            content = "",
            author = "",
            authorlink = "",
            date = "",
            link = "",
            reference = "",
        };


        // APPLICATION BAR
        // Eléments pour l'application bar
        #region AppBar
        ApplicationBarMenuItem _mItemRefresh = new ApplicationBarMenuItem()
        {
            Text = "actualiser",
        };
        ApplicationBarMenuItem _mItemSettings= new ApplicationBarMenuItem()
        {
            Text = "paramètres",
        };
        ApplicationBarMenuItem _mItemHelp = new ApplicationBarMenuItem()
        {
            Text = "aide",
        };
        ApplicationBarMenuItem _mItemBackToTop = new ApplicationBarMenuItem()
        {
            Text = "revenir en haut",
        };
        ApplicationBarMenuItem _mItemSaveImg = new ApplicationBarMenuItem()
        {
            Text = "télécharger l'arrière plan",
        };
        ApplicationBarIconButton _iButtonCopy = new ApplicationBarIconButton()
        {
            IconUri = new Uri("/Resources/Icons/Copy.png", UriKind.Relative),
            Text = "copier",
        };
        ApplicationBarIconButton _iButtonClose = new ApplicationBarIconButton()
        {
            IconUri = new Uri("/Resources/Icons/Close.png", UriKind.Relative),
            Text = "fermer",
        };
        ApplicationBarIconButton _iButtonShare = new ApplicationBarIconButton()
        {
            IconUri = new Uri("/Resources/Icons/Share.png", UriKind.Relative),
            Text = "partager",
        };
        #endregion AppBar
        #endregion Variables


        // Constructeur
        public MainPage()
        {
            InitializeComponent();
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
                BringMainQuotes();

                // Permettra d'appliquer le fond au premier démarrage de l'application (voir OnNavigatedTo)
                //App.ViewModel._BackgroundChanged = true;
            }
            else
            {
                ShowTodayQuote();
                if (App.ViewModel._QueryTermFromAuthor != null)
                {
                    // Lance la recherche de mots clés
                    blockFromAuthor_Tap();
                    GridGo.Visibility = System.Windows.Visibility.Visible;
                    //SlideSuggToSearchSB.Begin(); // animation
                    _slideToRightisVisible = true;
                }
            }

            InitializeAppBar();

            // Affecter l'exemple de données au contexte de données du contrôle ListBox
            DataContext = App.ViewModel;
            ApplicationBar.StateChanged += ApplicationBarStateChange_DisplayTime;
        }

        /// <summary>
        /// Utilisation du bouton Back (Retour) sur la page MainPage.xaml
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // Si la Grid de masquage est visible
            if (GridCache.Visibility == System.Windows.Visibility.Visible)
            {
                // Si la var _backEnabled est vraie (on n'est pas en train de charger les infos d'un auteur)
                if (_backEnabled)
                {
                    //TRANSITION DE LA SHAREBAR
                    SlideTransition sTransition = new SlideTransition();
                    sTransition.Mode = SlideTransitionMode.SlideDownFadeOut;
                    ITransition transition = sTransition.GetTransition(ShareBar);
                    transition.Completed += delegate
                    {
                        transition.Stop();
                        ShareBar.Visibility = System.Windows.Visibility.Collapsed;  // Masque la barre de partage
                    };
                    transition.Begin();
                    //-----------

                    GridCache.Visibility = System.Windows.Visibility.Collapsed; // masque la grille de masquage

                    // Masque la barre de partage
                    //ShareBar.Visibility = System.Windows.Visibility.Collapsed;

                    // Masque la pop up d'infos hors ligne
                    GridPop.Visibility = System.Windows.Visibility.Collapsed;

                    MainPanorama.IsEnabled = true; // réactive l'intéraction avec le panorama
                    ApplicationBar.IsVisible = true; // affiche l'application bar à nouveau

                    e.Cancel = true; // notifie qu'on a géré l'évent
                }
                else
                {
                    // Si on est en train de charger les infos d'un auteur,
                    // _backEnabled = faux
                    // On annule la fonction du bouton "Back"
                    e.Cancel = true;
                }
            }

            // Si les détails d'une citations sont affichés
            if (GridQuoteDetails.Visibility == System.Windows.Visibility.Visible)
            {
                // Animation pour masquer la popup de détails de la citation
                QuoteDetailsHideSB.Begin();
                QuoteDetailsHideSB.Completed += delegate
                {
                    DesactivePopupDetails();

                };
                // Annule l'action du bouton Retour (empêche de sortir de l'app)
                e.Cancel = true;
            }
            //base.OnBackKeyPress(e);         
        }


        /// <summary>
        /// Losqu'on navigue sur la page MainPage.xaml
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Permet de contrôler
            // si l'utilisateur ouvre l'app depuis le bouton "open app"
            // de la page des paramètres de l'écrand de verrouillage
            string lockscreenKey = "WallpaperSettings";
            string lockscreenValue = "0";

            bool lockscreenValueExists = NavigationContext.QueryString.TryGetValue(lockscreenKey, out lockscreenValue);

            if (lockscreenValueExists)
            {
                NavigationService.Navigate(new Uri("/Pages/Settings.xaml", UriKind.Relative));
            }
            // -----------------------------------------------------------


            if (App.ViewModel.CollectionQuotes.Count > 0)
            {
                Quote quote = App.ViewModel.CollectionQuotes[0];
            }

            //if (App.ViewModel._RemoveAllEntriesFromBackStack)
            //{
            //    while (NavigationService.CanGoBack)
            //    {
            //        NavigationService.RemoveBackEntry();
            //    }

            //    App.ViewModel._RemoveAllEntriesFromBackStack = false;
            //}

            // Affiche une Astuce lors du premier lancement
            //if (App.ViewModel._FirstLaunch)
            //{
            //    ShowMeATutorial();
            //}

            if (App.ViewModel._BackgroundChanged)
            {
                CheckBackground(false);
                MainAppBarInitialization();
            }
        }

        //private void ShowMeATutorial()
        //{
        //    // Astuce de partage
        //    //MessageBoxResult mbox = MessageBox.Show("Tapotez deux fois une citation pour la partager");
        //    NavigationService.Navigate(new Uri("/Pages/HelpPage.xaml", UriKind.Relative));
        //    //App.ViewModel._FirstLaunch = false;
        //    App.ViewModel.SaveData(); // Enregistre qu'il ne faut pas afficher l'astuce les prochaines fois
        //}


        async Task CheckBackground(bool brandnew)
        {
            // Modifie l'arrière plan de l'appli
            App.ViewModel._BackgroundChanged = false;

            if(App.ViewModel._ApplicationBackgroundType == "None")
            {
                ImageBackground.ImageSource = null;
            }
            else if(App.ViewModel._ApplicationBackgroundType == "Blue")
            {
                ImageBackground.ImageSource = new BitmapImage(new Uri("/Resources/Backgrounds/bg_blue.jpg", UriKind.Relative));
                App.ViewModel._CurrentBackgroundLink = "/Resources/Backgrounds/bg_blue.jpg";
            }
            else if(App.ViewModel._ApplicationBackgroundType == "Aircraft")
            {
                ImageBackground.ImageSource = new BitmapImage(new Uri("/Resources/Backgrounds/bg_aircraft.jpg", UriKind.Relative));
                App.ViewModel._CurrentBackgroundLink = "/Resources/Backgrounds/bg_aircraft.jpg";
            }
            else if (App.ViewModel._ApplicationBackgroundType == "Fleurs")
            {
                ImageBackground.ImageSource = new BitmapImage(new Uri("/Resources/Backgrounds/bg_flowers.jpg", UriKind.Relative));
                App.ViewModel._CurrentBackgroundLink = "/Resources/Backgrounds/bg_flowers.jpg";
            }

            else if (App.ViewModel._ApplicationBackgroundType == "Madame")
            {
                ShowProgressBarTraySystem("Chargement de l'image de bonjourmadame.fr");

                string URL = await App.ViewModel.GetBoujourmadamePicture();
                BitmapImage madameBG = new BitmapImage(new Uri(URL, UriKind.RelativeOrAbsolute));
                ImageBackground.ImageSource = madameBG;

                App.ViewModel._CurrentBackgroundLink = URL;

                HideProgressBarTrysystem();
            }

            else if (App.ViewModel._ApplicationBackgroundType == "Cosmos")
            {
                ShowProgressBarTraySystem("Chargement de l'image du cosmos");
                ImageBackground.ImageSource = new BitmapImage(new Uri("/Resources/Backgrounds/bg_stars.jpg", UriKind.Relative));


                string URL = await App.ViewModel.GetCosmosPicture();
                BitmapImage astroBG = new BitmapImage(new Uri(URL, UriKind.RelativeOrAbsolute));
                ImageBackground.ImageSource = astroBG;

                App.ViewModel._CurrentBackgroundLink = URL;

                HideProgressBarTrysystem();
            }

            
            else if (App.ViewModel._ApplicationBackgroundType == "Flickr")
            {
                ShowProgressBarTraySystem("Chargement de l'image de flickr");

                //if (!(App.ViewModel.BackgroundMustBeRefreshed()))
                //{
                //    // Alors on n'a pas besoin d"actualiser la liste des URL d'images
                //    // Et on récupère la liste d'URL depuis l'IO
                //    await App.ViewModel.LoadListPicturesFlickr();
                //}

                if (!brandnew)
                    await App.ViewModel.LoadListPicturesFlickr();
                
                string URL = "";
                URL = await App.ViewModel.ChooseARandomPicture(App.ViewModel._ApplicationBackgroundType);

                if (URL == null || URL == "") URL = "/Resources/Backgrounds/bg_Flickr.jpg";
                BitmapImage flickrBG = new BitmapImage(new Uri(URL, UriKind.RelativeOrAbsolute));
                ImageBackground.ImageSource = flickrBG;
            }

            // Sinon, on applique le fond par défaut de l'app
            else ImageBackground.ImageSource = new BitmapImage(new Uri("/Resources/Backgrounds/bg_blue.jpg", UriKind.Relative));

            ImageBackground.Stretch = Stretch.UniformToFill;

            HideProgressBarTrysystem();
        }

        private void ShowProgressBarTraySystem(string message = "Chargement...")
        {
            Progress.Text = message;
            Progress.IsIndeterminate = true;
            Progress.IsVisible = true;
            SystemTray.IsVisible = true;
        }

        private void HideProgressBarTrysystem()
        {
            Progress.Text = "";
            Progress.IsIndeterminate = false;
            Progress.IsVisible = false;
            SystemTray.IsVisible = false;
        }

        public async void BringMainQuotes()
        {
            bool res = false;
            //await App.ViewModel.LoadQuotes(App.ViewModel._LinkTodayQuote, 0, true);
            res = await App.ViewModel.LoadQuotes(App.ViewModel._LinkDay, 0, false);

            if (!res)
            {
                BringMainQuotes();
                return;
            }

            if ((App.ViewModel.CollectionQuotes.ElementAt(0) != null)
                && (!_IsLoaded))
            {
                DaySB1.Begin();
                DaySB1.Completed += delegate
                {
                    QuotesSB.BeginTime = TimeSpan.FromSeconds(0.0);
                    QuotesSB.Begin();
                    QuotesSB.Completed += delegate
                    {
                        // Affichage les guillement
                        Quote1.Visibility = System.Windows.Visibility.Visible;
                        Quote2.Visibility = System.Windows.Visibility.Visible;
                    };
                };

                TBQuoteDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).content;
                TBAuthorDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).author;
                //TBDateDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).date;


                // Définit explicitement la source
                LongListMainQuotes.ItemsSource = App.ViewModel.CollectionQuotes;

                // Masque la ProgressBar de l'élément 1 du panorama
                ProgressIDay.IsBusy = false;
                // Masque la ProgressBar de l'élément 2 du panorama
                // ProgressIndicatorQuotes.IsBusy = false;

                // Affiche un message si hors ligne
                if (App.ViewModel._Offline == true)
                {
                    GridCache.Visibility = System.Windows.Visibility.Visible;
                    GridPop.Visibility = System.Windows.Visibility.Visible;
                    MainPanorama.IsEnabled = false; // désactive les intéractions avec l'utilisateur
                    ApplicationBar.IsVisible = false; // masque l'app bar
                }
                _IsLoaded = true;
                if (App.ViewModel._TTSIsActivated)
                    TTSToday();
            }
        }

        private void ShowTodayQuote()
        {
            if (App.ViewModel.CollectionQuotes[0] != null)
            {
                ProgressIDay.IsBusy = false;
                ProgressIDay.Visibility = System.Windows.Visibility.Collapsed;

                DaySB1.Begin();
                DaySB1.Completed += delegate
                {
                    QuotesSB.BeginTime = TimeSpan.FromSeconds(0.0);
                    QuotesSB.Begin();
                    QuotesSB.Completed += delegate
                    {
                        // Affichage les guillement
                        Quote1.Visibility = System.Windows.Visibility.Visible;
                        Quote2.Visibility = System.Windows.Visibility.Visible;
                    };
                };

                TBQuoteDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).content;
                TBAuthorDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).author;
                TBDateDay.Text = App.ViewModel.CollectionQuotes.ElementAt(0).date;
            }
        }


        // Text-To-Speech
        // Voix annonçant la date et la citation du jour
        private async void TTSToday()
        {
            SpeechShowSB.BeginTime = TimeSpan.FromSeconds(0.5);
            SpeechShowSB.Begin();
            SpeechShowSB.Completed += delegate
            {
                SpeechSB.Begin();
                SpeechSB.RepeatBehavior = RepeatBehavior.Forever;
                GridSpeech.Visibility = System.Windows.Visibility.Visible;
            };

            try
            {
                string speech = "La citation du jour est. ";
                speech += TBQuoteDay.Text;
                speech += TBAuthorDay.Text;

                
                await _synth.SpeakTextAsync(speech);
                SpeechSB.Stop();
                SpeechHideSB.Begin();
                SpeechHideSB.Completed += delegate
                {
                    GridSpeech.Visibility = System.Windows.Visibility.Collapsed;
                };
                
            }
            catch
            {
                //MessageBoxResult mbox = MessageBox.Show("Une erreur est survenue lors de la lecture de la citation");
                SpeechSB.Stop();
                SpeechShowSB.Stop();

                SpeechHideSB.Begin();
                SpeechHideSB.Completed += delegate
                {
                    GridSpeech.Visibility = System.Windows.Visibility.Collapsed;
                };
            }
        }

        // Arrête la lecture audio
        private void SpeechStop_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _synth.CancelAll();
        }

        // METHODES DES DETAILS D'UNE CITATION----
        // Notifie d'une action quand l'utilisateur sélectionne un élément de la liste
        private void LongListMainQuotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_IDontWantPopup) { _IDontWantPopup = false; return; }

            if (e.AddedItems[0] is Quote)
            {
                // Récupère l'objet
                _myQuote = (Quote)e.AddedItems[0];
                QuoteSelected(1); // appelle la méthode en disant qu'on est sur l'index 1
            }
        }


        // GRILLE DE DETAILS D'UNE CITATION
        // Notifie d'une action quand l'utilisateur sélectionne un élément de la liste
        private void LongListSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_IDontWantPopup) { _IDontWantPopup = false; return; }

            if (e.AddedItems[0] is Quote)
            {
                // Récupère l'objet
                _myQuote = (Quote)e.AddedItems[0];
                QuoteSelected(2); // appelle la méthode en disant qu'on est sur l'index 2
            }
        }

        // Méthode éffectuée après un LongListSelector_SelectionChanged
        // La valeur de l'entier passé en paramètre correspond à l'index du panorama sur lequel on est
        private void QuoteSelected(int index)
        {
            // Evite le lancement de la méthode dans le cas où l'utilisateur appuie sur la liste à 15% visible (à droite) dans le Panorama
            if (index == MainPanorama.SelectedIndex)
            {
                // Si la Grid de détails de la citation est bien masquée
                // Et si on est bien sur l'index 1 du Panorama
                if ((GridQuoteDetails.Visibility == System.Windows.Visibility.Collapsed)
                    && (MainPanorama.SelectedIndex != 0))
                {
                    ActivePopupDetails();

                    if ((_myQuote.author != null) && (_myQuote.author != ""))
                    {
                        TBAuthor_QuoteDetails.Text = _myQuote.author.Replace("  ", "").Replace("\n", "");
                    }
                    else
                    {
                        TBAuthor_QuoteDetails.Text = "de Anonyme";
                    }

                    if ((_myQuote.date == "") || (_myQuote.date == null))
                    {
                        TBDate_QuoteDetails.Text = "N/A";
                    }

                    if ((_myQuote.authorlink != "") && (_myQuote.authorlink.Length > 0))
                    {
                        TBAuthorLinkDetails.Text = _myQuote.authorlink;
                    }



                    // Vide la Grille s'il y avait déjà des Eléments
                    // Vide le WrapPanel
                    // Vide le ScrollViewer
                    GridContent_QuoteDetails.Children.Clear();
                    _wrapPanel.Children.Clear();
                    _scrollview.Content = null;

                    string str = "";
                    int j = 0;
                    while (j < _myQuote.content.Length - 1)
                    {
                        while ((_myQuote.content[j] != ' ')
                            && (_myQuote.content[j] != '\n'))
                        {
                            // Evite le dépassement
                            if (!(j < _myQuote.content.Length - 1))
                            {
                                str += _myQuote.content.ElementAt(j);
                                j++;
                                break;
                            }
                            str += _myQuote.content.ElementAt(j);
                            j++;
                        }

                        // Méthode pour ajouter le mot à la Grille de contenu
                        Grid myGrid = new Grid();
                        myGrid.Background = _breakedWhiteColor;
                        myGrid.Margin = new Thickness(0, 0, 8, 8);
                        myGrid.Height = 40;

                        // Création d'un TextBlock avec ses paramètres
                        TextBlock block = new TextBlock();
                        block.Foreground = _blackColor;
                        block.FontSize = 30;
                        block.Text = str;

                        // Ajout de l'Event quand on tape sur un mot
                        block.Tap -= block_Tap;
                        block.Tap += block_Tap;

                        // Ajout du TextBlock à une Grille avec un fond gris
                        myGrid.Children.Add(block);
                        MetroInMotionUtils.MetroInMotion.SetTilt(myGrid, 3); // Tilt Effect

                        // Ajout de la Grille au WrapPanel
                        _wrapPanel.Children.Add(myGrid);
                        str = ""; // réinitialise
                        j++;
                    }

                    // Ajout du WrappPanel au ScrollViewer
                    _scrollview.Content = _wrapPanel;

                    // Ajout à la Grille
                    GridContent_QuoteDetails.Children.Add(_scrollview);

                    // Animation pour afficher la popup
                    QuoteDetailsShowSB.Begin();

                    //FlipQuote.Visibility = System.Windows.Visibility.Visible;
                    //GridContent_QuoteDetails.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }
        
        
        // Méthode quand on appuie sur un mot (détails d'une citation)
        private void block_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender != null)
            {
                TextBlock tblock = sender as TextBlock;
                if ((tblock != null) && (tblock.Text != null) && (tblock.Text != "")
                    && (tblock.Text.Length > 2))
                {
                    _queryNumber = 1;
                    TBSearch.Text = tblock.Text.Replace(",", "").Replace(".", "");

                    QuoteDetailsHideSB.Begin();
                    QuoteDetailsHideSB.Completed += delegate
                    {
                        DesactivePopupDetails();
                        GridSearch.Visibility = System.Windows.Visibility.Collapsed;
                        Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Collapsed;
                        
                        ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Collapsed;
                        GridTBoxSearch.Visibility = System.Windows.Visibility.Collapsed;
                    };
                    _cancelSearch = false;
                    SearchStarted();
                    Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Collapsed;
                    //MainPanorama.DefaultItem = MainPanorama.Items[2];
                }
            }
        }

        private void blockFromAuthor_Tap()
        {
            _queryNumber = 1;
            _cancelSearch = false;
            TBSearch.Text = App.ViewModel._QueryTermFromAuthor;
            GridSearch.Visibility = System.Windows.Visibility.Collapsed;
            Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Collapsed;

            SearchStarted();
            App.ViewModel._QueryTermFromAuthor = null;
        }

        //----
        // FIN --- METHODES DES DETAILS D'UNE CITATION


        // Fonction pour partager la citation du jour lors d'un double tap
        private void DoubleTap_dayquote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (App.ViewModel.CollectionQuotes.ElementAt(0) != null)
            {

                // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                GridCache.Visibility = System.Windows.Visibility.Visible;

                // Afficher le StackPanel de partage (avec animation)
                SlideTransition stransition = new SlideTransition();
                stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition = stransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                };
                transition.Begin();
                ShareBar.Visibility = System.Windows.Visibility.Visible;

                // Désactive l'intéraction avec le panorama
                MainPanorama.IsEnabled = false;

                // Masque l'AppBar
                ApplicationBar.IsVisible = false;
            }
        }

        // Fonction pour partager la citation du jour lors d'un maintient de la citation du jour
        private void Hold_dayquote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (App.ViewModel.CollectionQuotes.ElementAt(0) != null)
            {

                // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                GridCache.Visibility = System.Windows.Visibility.Visible;

                // Afficher le StackPanel de partage (avec animation)
                SlideTransition stransition = new SlideTransition();
                stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition = stransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                };
                transition.Begin();
                ShareBar.Visibility = System.Windows.Visibility.Visible;

                // Désactive l'intéraction avec le panorama
                MainPanorama.IsEnabled = false;

                // Masque l'AppBar
                ApplicationBar.IsVisible = false;
            }
        }
 

        // Affiche les infos de l'auteur de la citation du jour
        private void TBAuthorDay_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel._Offline)
            {
                if (App.ViewModel.CollectionQuotes.ElementAt(0) != null)
                {
                    Quote quote = App.ViewModel.CollectionQuotes.ElementAt(0);
                    if (quote.authorlink != "")
                    {
                        PhoneApplicationService.Current.State["authorName"] = quote.author;
                        PhoneApplicationService.Current.State["authorLink"] = quote.authorlink;

                    }
                    NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                }
            }
            else
            {
                MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
            }
        }


        // Fonction de partage des citations du LongListSelector lors d'un Double Tap
        private void DoubleTap_Quote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((MainPanorama.SelectedIndex == 1) || (MainPanorama.SelectedIndex == 2))
            {
                // Test si on a bien récupéré une citation
                if (_myQuote != null)
                {
                    // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                    GridCache.Visibility = System.Windows.Visibility.Visible;


                    // Afficher le StackPanel de partage (avec animation)
                    SlideTransition stransition = new SlideTransition();
                    stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                    ITransition transition = stransition.GetTransition(ShareBar);
                    transition.Completed += delegate
                    {
                        transition.Stop();
                    };
                    transition.Begin();
                    ShareBar.Visibility = System.Windows.Visibility.Visible;


                    // Désactive l'intéraction avec le panorama
                    MainPanorama.IsEnabled = false;

                    // Masque l'AppBar
                    ApplicationBar.IsVisible = false;
                }
            }
        }

        // Fonction de partage des citations de la liste lors d'un Hold
        // NOTE: MEME ACTION QUE LE DOUBLE TAP
        public void Hold_Quote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender != null)
            {
                StackPanel stack = (StackPanel)sender;
                if (stack.Children.ElementAt(0) != null)
                {
                    if (MainPanorama.SelectedIndex == 1)
                    {
                        //Récupération de la date
                        _tblock = (TextBlock)stack.Children.ElementAt(0);
                        //_alpha = _tblock.Text;
                        _myQuote.date = _tblock.Text;
                        //Récupération de la citation
                        _tblock = (TextBlock)stack.Children.ElementAt(1);
                        //_alpha = _tblock.Text;
                        _myQuote.content = _tblock.Text;
                        //Récupération de l'autheur
                        _tblock = (TextBlock)stack.Children.ElementAt(2);
                        //_alpha = _tblock.Text;
                        _myQuote.author = _tblock.Text;
                    }
                    else if (MainPanorama.SelectedIndex == 2)
                    {
                        //Récupération de la citation
                        _tblock = (TextBlock)stack.Children.ElementAt(0);
                        //_alpha = _tblock.Text;
                        _myQuote.content = _tblock.Text;
                        //Récupération de l'autheur
                        _tblock = (TextBlock)stack.Children.ElementAt(1);
                        //_alpha = _tblock.Text;
                        _myQuote.author = _tblock.Text;
                    }
                    // Test si on a bien récupéré une citation
                    if (_myQuote != null)
                    {
                        // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                        GridCache.Visibility = System.Windows.Visibility.Visible;
                        GridCache.Opacity = 0.5;

                        // Afficher le StackPanel de partage (avec animation)
                        SlideTransition stransition = new SlideTransition();
                        stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                        ITransition transition = stransition.GetTransition(ShareBar);
                        transition.Completed += delegate
                        {
                            transition.Stop();
                        };
                        transition.Begin();
                        ShareBar.Visibility = System.Windows.Visibility.Visible;


                        // Désactive l'intéraction avec le panorama
                        MainPanorama.IsEnabled = false;

                        // Masque l'AppBar
                        ApplicationBar.IsVisible = false;
                    }
                }
            }
        }

        // -> Partage sur twitter, Facebook, etc
        private void share_twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((MainPanorama.SelectedIndex == 0) && (TBQuoteDay.Text!=null))
            {

                ShareLinkTask shareLink = new ShareLinkTask();

                shareLink.Title = "Citation";
                shareLink.Message = TBQuoteDay.Text + TBAuthorDay.Text;
                //if(TBAuthorDay.Text != null)
                //    shareLink.Message+= " - " + TBAuthorDay.Text;

                shareLink.LinkUri = new Uri("http://www.evene.fr");
                shareLink.Show();
            }

            else if ((MainPanorama.SelectedIndex == 1)
                && (_myQuote.content != ""))
            {
                ShareLinkTask shareLink = new ShareLinkTask();

                shareLink.Title = "Citation";
                shareLink.Message = _myQuote.content + " - " + _myQuote.author;
                //if (_myQuote.author != "")
                //    shareLink.Message+= " - " + _myQuote.author;

                shareLink.LinkUri = new Uri("http://www.evene.fr");
                shareLink.Show();
            }

            else if ((MainPanorama.SelectedIndex == 2)
                && (_myQuote.content != ""))
            {
                ShareLinkTask shareLink = new ShareLinkTask();

                shareLink.Title = "Citation";
                shareLink.Message = _myQuote.content + " - " + _myQuote.author;
                //if (_myQuote.author != "")
                //    shareLink.Message += " - " + _myQuote.author;

                shareLink.LinkUri = new Uri("http://www.evene.fr");
                shareLink.Show();
            }

            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;

            // Réactive l'interaction avec le panorama
            MainPanorama.IsEnabled = true;

            // Affiche (à nouveau) l'AppBar
            ApplicationBar.IsVisible = true;
        }

        // -> Partage par email
        private void share_mail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((MainPanorama.SelectedIndex == 0) && (TBQuoteDay.Text != null))
            {

                EmailComposeTask email = new EmailComposeTask()
                {
                    Subject = "Citation",
                    Body = TBQuoteDay.Text + " - " + TBAuthorDay.Text,
                    To = "",
                };

                //if (TBAuthorDay.Text != null)
                //    email.Body += " - " + TBAuthorDay.Text;

                email.Show();
            }

            else if ((MainPanorama.SelectedIndex == 1)
                 && (_myQuote.content != ""))
            {
                EmailComposeTask email = new EmailComposeTask()
                {
                    Subject = "Citation",
                    Body = _myQuote.content +  " - " + _myQuote.author,
                    To = "",
                };

                //if (_myQuote.author != "")
                //    email.Body += " - " + _myQuote.author;

                email.Show();
            }

            else if ((MainPanorama.SelectedIndex == 2)
                 && (_myQuote.content != ""))
            {
                EmailComposeTask email = new EmailComposeTask()
                {
                    Subject = "Citation",
                    Body = _myQuote.content + " - " + _myQuote.author,
                    To = "",
                };
                //if (_myQuote.author != "")
                //    email.Body += " - " + _myQuote.author;

                email.Show();
            }

            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;

            // Réactive l'interaction avec le panorama
            MainPanorama.IsEnabled = true;

            // Affiche (à nouveau) l'AppBar
            ApplicationBar.IsVisible = true;
        }

        // -> Partage par sms
        private void share_sms_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((MainPanorama.SelectedIndex == 0) && (TBQuoteDay.Text != null))
            {
                SmsComposeTask sms = new SmsComposeTask()
                {
                    Body = TBQuoteDay.Text + " - " + TBAuthorDay.Text,
                    To = "",
                };

                //if (TBAuthorDay.Text != null)
                //    sms.Body += " - " + TBAuthorDay.Text;

                sms.Show();
            }
            else if ((MainPanorama.SelectedIndex == 1)
                && (_myQuote.content != ""))
            {
                SmsComposeTask sms = new SmsComposeTask()
                {
                    Body = _myQuote.content + " - " + _myQuote.author,
                    To = "",
                };
                //if (_myQuote.author != "")
                //    sms.Body += " - " + _myQuote.author;

                sms.Show();
            }


            else if ((MainPanorama.SelectedIndex == 2)
                && (_myQuote.content != ""))
            {
                SmsComposeTask sms = new SmsComposeTask()
                {
                    Body = _myQuote.content + " - " + _myQuote.author,
                    To = "",
                };
                //if (_myQuote.author != "")
                //    sms.Body += " - " + _myQuote.author;

                sms.Show();
            }

            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;

            // Réactive l'interaction avec le panorama
            MainPanorama.IsEnabled = true;

            // Affiche (à nouveau) l'AppBar
            ApplicationBar.IsVisible = true;
        }

        // Evènement se produisant quand de nouveaux éléments apparaissent (sont visibles) dans la LongListSelector
        private async void LongListMainQuotes_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if ((LongListMainQuotes.ItemsSource
                as ObservableCollection<Quote>) == null) return;

            // Get a count of how many items are loaded in memory
            int currentItemsCount = (LongListMainQuotes.ItemsSource
                as ObservableCollection<Quote>).Count;

            if ((currentItemsCount >= 14) && (_BackToTop == false) &&
                (e.Container.Content as Quote) != null)
            {
                if ((e.Container.Content as Quote).
                    Equals((LongListMainQuotes.ItemsSource as ObservableCollection<Quote>)
                    [currentItemsCount - 5]))
                {
                    if ((!App.ViewModel._Offline) && (!_IsLoading))
                    {
                        _IsLoading = true;
                        ShowProgressBarTraySystem("Charge encore plus de citations...");

                        _pageNumber++; // incrémente numéro pour la prochaine page
                        await App.ViewModel.LoadQuotes(App.ViewModel._LinkBack, _pageNumber, false); // appelle la méthode avec de nouveaux params

                        HideProgressBarTrysystem();
                        //Microsoft.Phone.Shell.SystemTray.SetProgressIndicator(this, Progress);
                        _IsLoading = false;
                    }
                }
            }
        }


        private async void LongListSearchResults_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            // On n'a pas eu de nouveaux résultats à la dernière recherche
            // donc inutile de continuer
            if (!(_continueToSearch)) return;

            if ((LongListSearchResults.ItemsSource
                as ObservableCollection<Quote>) == null) return;

            // Get a count of how many items are loaded in memory
            int currentItemsCount = (LongListSearchResults.ItemsSource
                as ObservableCollection<Quote>).Count;

            if ((currentItemsCount >= 4) && (_BackToTop == false) &&
                (e.Container.Content as Quote) != null)
            {
                if ((e.Container.Content as Quote).
                    Equals((LongListSearchResults.ItemsSource as ObservableCollection<Quote>)
                    [currentItemsCount - 5]))
                {
                    if ((!App.ViewModel._Offline) &&(!_IsLoading))
                    {
                        _IsLoading = true;
                        ShowProgressBarTraySystem("Charge encore plus de citations...");

                        _continueToSearch = await App.ViewModel.SearchQuote(_query, _queryNumber, _lastQuoteResultsContent);

                        SearchCompleted();

                        HideProgressBarTrysystem();
                        _IsLoading = false;
                    }
                }
            }
        }



        private void MainPanoramaSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Masque l'item du menu "revenir au début" si on est sur la première page du panorama
            if (MainPanorama.SelectedIndex == 0)
            {
                if (ApplicationBar.MenuItems.Count == (_numberOfItemsInAppBar +1))
                    ApplicationBar.MenuItems.RemoveAt(_numberOfItemsInAppBar);
            }

            // Affiche l'item du menu "revenir au début" si on est sur la deuxième page du panorama
            else if ((MainPanorama.SelectedIndex == 1) || (MainPanorama.SelectedIndex == 2))
            {
                // modifie l'AppBar
                if (ApplicationBar.MenuItems.Count == _numberOfItemsInAppBar)
                {
                    ApplicationBarMenuItem Item_BackToTop = new ApplicationBarMenuItem()
                    {
                        Text = "revenir au début",
                        IsEnabled = true,
                    };
                    Item_BackToTop.Click += new EventHandler(MenuItem_BackToTop_Click);
                    ApplicationBar.MenuItems.Add(Item_BackToTop);
                }

                // masque la popup indiquant d'allant sur l'écran de gauche pour accéder aux résultats de la recherche
                if (_slideToRightisVisible)
                {
                    SlideToResultsSB2.Begin();
                    _slideToRightisVisible = false;
                }
            }
        }

        // Enregistrement sur l'évènement StateChanged de l'application bar
        void ApplicationBarStateChange_DisplayTime(object sender, ApplicationBarStateChangedEventArgs e)
        {
            bool menuisvisible = e.IsMenuVisible;
            if (menuisvisible)
            {
                SystemTray.IsVisible = true;
                ApplicationBar.Opacity = 0.999;
            }
            else
            {
                SystemTray.IsVisible = false;
                ApplicationBar.Opacity = 0.999;
            }
        }

        // Appuie sur le bouton Ok
        private void Ok_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GridPop.Visibility == System.Windows.Visibility.Visible)
            {
                GridCache.Visibility = System.Windows.Visibility.Collapsed; // masque la grille sombre
                GridPop.Visibility = System.Windows.Visibility.Collapsed;   // masque la popup d'informations
                MainPanorama.IsEnabled = true;
                ApplicationBar.IsVisible = true;
            }
        }

        // Masque la grille sombre quand on double tap dessus (si la shareBar est affichée)
        private void GridCache_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ((GridCache.Visibility == System.Windows.Visibility.Visible)
                && (ShareBar.Visibility == System.Windows.Visibility.Visible))
            {
                //TRANSITION DE LA SHAREBAR
                SlideTransition sTransition = new SlideTransition();
                sTransition.Mode = SlideTransitionMode.SlideDownFadeOut;
                ITransition transition = sTransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                    ShareBar.Visibility = System.Windows.Visibility.Collapsed;  // Masque la barre de partage
                };
                transition.Begin();
                //-----------

                GridCache.Visibility = System.Windows.Visibility.Collapsed; // masque la grille de masquage

                MainPanorama.IsEnabled = true;       // réactive l'intéraction avec le panorama
                ApplicationBar.IsVisible = true;    // affiche l'application bar à nouveau
            }
        }

        

        // Affiche le TextBox de recherche lors d'un tape sur la loupe de recherche (bouton)
        private void GridSearch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _cancelSearch = true;

            // Masque la longlist si elle visible
            if (LongListSearchResults.Visibility == System.Windows.Visibility.Visible)
                LongListSearchResults.Visibility = System.Windows.Visibility.Collapsed;

            _queryNumber = 1;
            

            if (_gridFunction == 0)
            {
                ApplicationBar.IsVisible = false;

                TBTerms.Text = "";
                //GridSearch.Visibility = System.Windows.Visibility.Collapsed;
                Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Collapsed;

                GridTBoxSearch.Opacity = 1.0;

                // Lance l'animation du TextBox de recherche
                TBSearchSB1.Begin();
                TBSearchSB1.Completed += delegate
                {
                    GridTBoxSearch.Visibility = System.Windows.Visibility.Visible;
                    TBSearch.Focus();

                    // Affiche la croix pour vider le Textbox si ce dernier contient du texte
                    if ((TBSearch.Text != "") && (TBSearch.Text != null))
                    {
                        ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Visible;
                    }
                };
            }
            else if (_gridFunction == -1)
            {
                NavigationService.Navigate(new Uri("/Pages/AuthorsListPage.xaml", UriKind.Relative));
            }
            else if (_gridFunction == 1)
            {
            }
        }

        // Vide le TextBox de recherche quand on tapote sur la croix
        private void ClearTBoxSearch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TBSearch.Text = "";
            _letMyFocus = true;
        }

        // Masque le TextBox s'il perd le focus
        // Gère le cas où on appuie sur la croix pour supprimer le texte
        // sans perdre le focus
        private void TBSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_letMyFocus)
            {
                // Si le Textbox perd le focus et qu'on souhaite faire une recherche
                if (GridTBoxSearch.Visibility == System.Windows.Visibility.Visible)
                {
                    TBSearchSB2.Begin();
                    TBSearchSB2.Completed += delegate
                    {
                        GridTBoxSearch.Visibility = System.Windows.Visibility.Collapsed;
                        GridTBoxSearch.Opacity = 1.0;
                        GridTBoxSearch.RenderTransform.SetValue(CompositeTransform.ScaleXProperty, 1.0);
                        GridTBoxSearch.RenderTransform.SetValue(CompositeTransform.ScaleYProperty, 1.0);
                    };

                    TBVoidSearch.Visibility = System.Windows.Visibility.Collapsed;

                    // Si on ne souhaitais faire aucune recherche, et
                    // Si aucune recherche est en cours
                    // On masque le LonglistSelector, la barre de progression et le Textbox
                    if (!_isLookingFor)
                    {
                        LongListSearchResults.Visibility = System.Windows.Visibility.Collapsed;

                        ellipseSearch.Visibility = System.Windows.Visibility.Visible;
                        ImgSearch.Visibility = System.Windows.Visibility.Visible;
                        GridSearch.Visibility = System.Windows.Visibility.Visible;
                        Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Visible;

                        ProgressQuotesResults.IsBusy = false;
                        ProgressQuotesResults.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Collapsed;
                ApplicationBar.IsVisible = true;
            }
            else
            {
                // Si on vient d'appuyer sur la croix pour vider le Textbox de recherche
                // On remet le Focus sur le Textbox
                TBSearch.Focus();
                _letMyFocus = false;
            }

        }

        // Surveille le texte dans le TextBox
        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((TBSearch.Text != "") && (TBSearch != null))
            {
                // Affiche la croix dans le TextBox si celui-ci contient du text
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Visible;
            }
            else if ((TBSearch.Text == "") || (TBSearch == null))
            {
                // On la masque sinon
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        // Lance la recherche si on tapote sur la petite loupe contenue dans le TextBox
        private void SearchButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel._Offline)
            {
                // Si le TextBox n'est pas vide, on lance la recherche
                if ((TBSearch.Text != "") && (TBSearch.Text != null))
                {
                    SearchStarted();
                    _cancelSearch = false;
                    this.Focus();
                }
            }
            else
            {
                MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
            }
        }

        // Surveille les touches appuyés quand le focus est sur le TextBox
        private void TBSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Lance la recherche si on appuie sur la touche Entrer
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!App.ViewModel._Offline)
                {
                    // Vérifie que le Textbox n'est pas vide
                    if ((TBSearch.Text != "") && (TBSearch.Text != null))
                    {
                        SearchStarted();
                        _cancelSearch = false;
                        this.Focus();
                    }
                }
                else
                {
                    MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
                }
            }
        }

        // Méthode démarrant la recherche
        private async void SearchStarted()
        {
            _isLookingFor = true;
            _lastQuoteContent = "";

            // Réinitialise
            _continueToSearch = true;

            // Masque le TextBox disant que la recherche est vide
            TBVoidSearch.Visibility = System.Windows.Visibility.Collapsed;

            // Textblock d'arrière plan qui affiche les termes recherchés
            _query = TBSearch.Text.ToUpper();
            TBTerms.Text = _query;


            ProgressQuotesResults.IsBusy = true;
            ProgressQuotesResults.Visibility = System.Windows.Visibility.Visible;

            // On teste que la requête n'est pas vide
            if ((_query != "") && (_query != null))
            {
                // Vide la Collection de citations de recherche
                // Lance la méthode de recherche du MainViewModel, avec un eventHandler
                App.ViewModel.CollectionQuotesSearch.Clear();
                await App.ViewModel.SearchQuote(_query, _queryNumber, "");
                SearchCompleted();
            }
            else
            {
                App.ViewModel.CollectionQuotesSearch.Clear();
                SearchCompleted();
            }
        }

        // Méthode signalant la fin de la recherche
        private void SearchCompleted()
        {
            // Si on n'a pas annulé la recherche en cliquant sur la loupe
            if (!_cancelSearch)
            {
                _isLookingFor = false;
                HideProgressBarTrysystem();

                ProgressQuotesResults.IsBusy = false;
                ProgressQuotesResults.Visibility = System.Windows.Visibility.Collapsed;

                if (GridQuoteDetails.Visibility == System.Windows.Visibility.Collapsed)
                    LongListSearchResults.Visibility = System.Windows.Visibility.Visible;

                // Si on obtient des résultats, on incrémente la variable _queryNumber
                if (App.ViewModel.CollectionQuotesSearch.Count > 0)
                {
                    _lastQuoteResultsContent = App.ViewModel.CollectionQuotesSearch.Last().content;
                    
                    //Incrémente le numéro de page à consulter la prochaine fois
                    // si la recherche a donné des résultats.
                    _queryNumber++;
                    
                }
                else
                {
                    // Si la recherche n'a rien donné comme résultat,
                    // On le dit à l'utilisateur, et on masque la longlist
                    // On affiche le bouton Loupe de recherche
                    _queryNumber = 1;
                    LongListSearchResults.Visibility = System.Windows.Visibility.Collapsed;
                    
                    

                    ProgressQuotesResults.IsBusy = false;
                    ProgressQuotesResults.Visibility = System.Windows.Visibility.Collapsed;
                    TBTerms.Text = "";

                    //GridSearch.Visibility = System.Windows.Visibility.Visible;
                    //ellipseSearch.Visibility = System.Windows.Visibility.Visible;
                    //ImgSearch.Visibility = System.Windows.Visibility.Visible;
                    //Grid_Icon_Arrow_Down.Visibility = System.Windows.Visibility.Visible;

                    GridTBoxSearch.Visibility = System.Windows.Visibility.Visible;


                    TBVoidSearch.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                // Si on a voulu annuler la recherche en cliquant sur la loupe du LonglistsSelector
                // On masque les indicateurs de chargement
                _isLookingFor = false;
                HideProgressBarTrysystem();

                ProgressQuotesResults.IsBusy = false;
                ProgressQuotesResults.Visibility = System.Windows.Visibility.Collapsed;
                LongListSearchResults.Visibility = System.Windows.Visibility.Collapsed;
                _queryNumber = 1;
                TBTerms.Text = "";
            }
        }

        // Méthode pour colorer les termes de la recherche (pas implémentée)
        void ColorSearchTerms()
        {
            //int pos = 0;
            //pos = rtb.Find(searchText, pos, RichTextBoxFinds.MatchCase);
            //while (pos != -1)
            //{
            //    if (rtb.SelectedText == searchText && tbxEditor.SelectedText != "")
            //    {
            //        rtb.SelectionLength = searchText.Length;
            //        rtb.SelectionFont = new Font("arial", 8, FontStyle.Underline);
            //        rtb.SelectionColor = Color.Green;
            //    }
            //    pos = rtb.Find(searchText, pos + 1, RichTextBoxFinds.MatchCase);
            //}

        }

        // Suite de méthodes à effectuer pour afficher la Grid de détails
        private void ActivePopupDetails()
        {
            // Désactive l'intéraction avec le panorama et affiche les détails de la citation
            MainPanorama.IsEnabled = false;
            GridCache.Visibility = System.Windows.Visibility.Visible;
            
            // Active l'AppBar pour les détails de la citation
            DetailsAppBarInitialization();

            TBDate_QuoteDetails.Text = _myQuote.date;
            TBAuthor_QuoteDetails.Text = "";

            if (MainPanorama.SelectedIndex == 1)
            {
                LongListMainQuotes.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                LongListSearchResults.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        // Suite de méthodes à effectuer pour masquer la Grid de détails
        private void DesactivePopupDetails()
        {
            // Réactivation des éléments
            MainPanorama.IsEnabled = true;
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            MainAppBarInitialization();
            //ApplicationBar.IsVisible = true;
            // LongListMainQuotes.Visibility = System.Windows.Visibility.Visible;

            if (MainPanorama.SelectedIndex == 1)
            {
                LongListMainQuotes.Visibility = System.Windows.Visibility.Visible;
                LongListMainQuotes.SelectedItem = -1;
            }
            else if (MainPanorama.SelectedIndex == 2)
            {
                LongListSearchResults.Visibility = System.Windows.Visibility.Visible;
                LongListSearchResults.SelectedItem = -1;
            }

            // Masque les différents éléments contenus dans la Grille (popup)
            GridContent_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridDate_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridAuthor_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridQuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Méthode lancée quand on tapote sur un auteur dans une LongListSelector
        // des citations précédentes ou des citations recherchées
        private async void ListAuthor_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel._Offline)
            {
                _IDontWantPopup = true;

                _tblock = (TextBlock)sender;

                // Récupère le nom de l'auteur
                // sans le "de "
                string author = "";
                if (_tblock.Text.StartsWith("de "))
                {
                    author = _tblock.Text.Substring(3);
                }
                else author = _tblock.Text;


                if (_tblock != null)
                {
                    StackPanel oneParent = _tblock.Parent as StackPanel;
                    
                    if (oneParent != null)
                    {
                        TextBlock secondChild = oneParent.Children.Last() as TextBlock;
                        if (secondChild != null &&
                            secondChild.Text != null && secondChild.Text != "")
                        {
                            PhoneApplicationService.Current.State["authorName"] = author;
                            PhoneApplicationService.Current.State["authorLink"] = secondChild.Text;
                            NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                        }
                        else
                        {
                            if (!(_tblock.Text.Contains("de ")) || (_tblock.Text.Contains("Proverbe"))
                                || (_tblock.Text.Contains("Bible")) || (_tblock.Text.Contains("Anonyme")))
                            {
                                MessageBoxResult message = MessageBox.Show("Désolé, je n'ai pas trouvé d'informations sur cet auteur");
                                return;
                            }

                            // Affichage de la barre de progression Syncfusion
                            // et d'un petit texte disant que l'app est en train de charger
                            // Empêche l'intéraction avec le Panorama principal et masque l'ApplicationBar
                            _backEnabled = false;
                            ApplicationBar.IsVisible = false;
                            MainPanorama.IsEnabled = false;

                            GridCache2ShowSB.Begin();
                            GridCache2ShowSB.Completed += delegate
                            {
                                GridCache2.Visibility = System.Windows.Visibility.Visible;
                            };

                            TBLoading.Visibility = System.Windows.Visibility.Visible;
                            ProgressPage.Visibility = System.Windows.Visibility.Visible;

                            // Lancement asynchrone de la méthode pour récupérer le lien de la page de l'auteur
                            string authorLink = await App.ViewModel.FindAuthorLink(author);

                             //Lancement de la méthode pour obtenir la bio et les citations
                            if (authorLink != null && authorLink != "")
                            {
                                // Si on obtient un résultat, on navigue sur la page de l'auteur
                                PhoneApplicationService.Current.State["authorName"] = author;
                                PhoneApplicationService.Current.State["authorLink"] = authorLink;

                                NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                            }
                            else
                            {
                                // Masque de la barre de progression Syncfusion et le texte de chargement
                                // Réactive l'intéraction avec le Panorama principal et affiche l'ApplicationBar
                                ApplicationBar.IsVisible = true;

                                GridCache2HideSB.Begin();
                                GridCache2HideSB.Completed += delegate
                                {
                                    GridCache2.Visibility = System.Windows.Visibility.Collapsed;
                                };

                                TBLoading.Visibility = System.Windows.Visibility.Collapsed;
                                ProgressPage.Visibility = System.Windows.Visibility.Collapsed;
                                TBLoading.Text = "chargement des informations";
                                //SfBusyGetAuthorInfos.IsBusy = false;
                                MainPanorama.IsEnabled = true;
                                _backEnabled = true;


                                // --------------------
                                // Recherche sur Bing ?
                                SearchTask searchTask = new SearchTask();
                                searchTask.SearchQuery = author;
                                searchTask.Show();
                            }
                        }
                    }

                    // sinon on est dans la popup
                    else
                    {
                        Grid anotherParent = _tblock.Parent as Grid;

                        TextBlock secondChild = anotherParent.Children.Last() as TextBlock;
                        if (secondChild != null &&
                            secondChild.Text != null && secondChild.Text != "")
                        {
                            PhoneApplicationService.Current.State["authorName"] = author;
                            PhoneApplicationService.Current.State["authorLink"] = secondChild.Text;
                            NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                        }
                        else
                        {
                            if (!(_tblock.Text.Contains("de ")) || (_tblock.Text.Contains("Proverbe"))
                                || (_tblock.Text.Contains("Bible")) || (_tblock.Text.Contains("Anonyme")))
                            {
                                MessageBoxResult message = MessageBox.Show("Désolé, je n'ai pas trouvé d'informations sur cet auteur");
                                return;
                            }

                            // Affichage de la barre de progression Syncfusion
                            // et d'un petit texte disant que l'app est en train de charger
                            // Empêche l'intéraction avec le Panorama principal et masque l'ApplicationBar
                            _backEnabled = false;
                            ApplicationBar.IsVisible = false;
                            MainPanorama.IsEnabled = false;

                            GridCache2ShowSB.Begin();
                            GridCache2ShowSB.Completed += delegate
                            {
                                GridCache2.Visibility = System.Windows.Visibility.Visible;
                            };

                            TBLoading.Visibility = System.Windows.Visibility.Visible;
                            ProgressPage.Visibility = System.Windows.Visibility.Visible;

                            // Lancement asynchrone de la méthode pour récupérer le lien de la page de l'auteur
                            string authorLink = await App.ViewModel.FindAuthorLink(author);

                            //Lancement de la méthode pour obtenir la bio et les citations
                            if (authorLink != null && authorLink != "")
                            {
                                // Si on obtient un résultat, on navigue sur la page de l'auteur
                                PhoneApplicationService.Current.State["authorName"] = author;
                                PhoneApplicationService.Current.State["authorLink"] = authorLink;

                                NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                            }
                            else
                            {
                                // Masque de la barre de progression Syncfusion et le texte de chargement
                                // Réactive l'intéraction avec le Panorama principal et affiche l'ApplicationBar
                                ApplicationBar.IsVisible = true;

                                GridCache2HideSB.Begin();
                                GridCache2HideSB.Completed += delegate
                                {
                                    GridCache2.Visibility = System.Windows.Visibility.Collapsed;
                                };

                                TBLoading.Visibility = System.Windows.Visibility.Collapsed;
                                ProgressPage.Visibility = System.Windows.Visibility.Collapsed;
                                TBLoading.Text = "chargement des informations";
                                //SfBusyGetAuthorInfos.IsBusy = false;
                                MainPanorama.IsEnabled = true;
                                _backEnabled = true;


                                // --------------------
                                // Recherche sur Bing ?
                                SearchTask searchTask = new SearchTask();
                                searchTask.SearchQuery = author;
                                searchTask.Show();
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
            }
        }


        // Initialise les Items de l'AppBar
        // et affiche l'appbar principal
        private void InitializeAppBar()
        {
            _iButtonClose.Click += IconButtonClose_Click;
            _iButtonCopy.Click += IconButtonCopy_Click;
            _iButtonShare.Click += IconButtonShare_Click;

            _mItemRefresh.Click += MenuItem_Refresh_Click;
            _mItemSettings.Click += MenuItem_Settings_Click;
            _mItemHelp.Click += MenuItem_Help_Click;
            _mItemBackToTop.Click += MenuItem_BackToTop_Click;
            _mItemSaveImg.Click += MenuItem_SaveImg_Click;

            MainAppBarInitialization();
        }

        // Ferme les détails de la citation
        private void IconButtonClose_Click(object sender, EventArgs e)
        {
            // Arrêt l'animation si elle est en cours
            QuoteDetailsShowSB.Stop();

            // Animation pour masquer la popup de détails de la citation
            QuoteDetailsHideSB.Begin();
            QuoteDetailsHideSB.Completed += delegate
            {
                DesactivePopupDetails();

            };
            
        }

        // Partage la citation
        private void IconButtonShare_Click(object sender, EventArgs e)
        {
            QuoteDetailsShowSB.Stop();
            DesactivePopupDetails();

            if (_myQuote != null)
            {
                // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                GridCache.Visibility = System.Windows.Visibility.Visible;
                GridCache.Opacity = 0.5;

                // Afficher le StackPanel de partage (avec animation)
                SlideTransition stransition = new SlideTransition();
                stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition = stransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                };
                transition.Begin();
                ShareBar.Visibility = System.Windows.Visibility.Visible;


                // Désactive l'intéraction avec le panorama
                MainPanorama.IsEnabled = false;

                // Masque l'AppBar
                ApplicationBar.IsVisible = false;
            }
        }

        // Copie la citation et l'auteur dans le press-papier
        private void IconButtonCopy_Click(object sender, EventArgs e)
        {
            QuoteDetailsShowSB.Stop();

            if ((_myQuote != null)
                && (_myQuote.content != null) && (_myQuote.author != null))
            {
                Clipboard.SetText(_myQuote.content + " - " + _myQuote.author);

                // Animation pour masquer la popup de détails de la citation
                QuoteDetailsHideSB.Begin();
                QuoteDetailsHideSB.Completed += delegate
                {
                    DesactivePopupDetails();

                };


                ToastPrompt toast = new ToastPrompt()
                {
                    Message = "La citation a été copiée dans le press-papier",
                };
                toast.Show();


            }
            else
            {
                MessageBoxResult m = MessageBox.Show("Une erreur est survenue lors de la copie de la citation dans le press-papier");
            }

        }

        // Initialisation de l'AppBar principal
        private void MainAppBarInitialization()
        {
            ApplicationBar.Buttons.Clear();
            ApplicationBar.MenuItems.Clear();

            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.MenuItems.Add(_mItemRefresh);
            ApplicationBar.MenuItems.Add(_mItemSettings);
            ApplicationBar.MenuItems.Add(_mItemHelp);

            string type = App.ViewModel._ApplicationBackgroundType;
            if (type == "Madame" || type == "Cosmos" || type == "Flickr")
            {
                // Ce code est exécuté si on a activé l'arrière plan de bonjourmadame
                ApplicationBar.MenuItems.Add(_mItemSaveImg);
                if (_numberOfItemsInAppBar == 3)
                    _numberOfItemsInAppBar++;
            }
            if ((MainPanorama.SelectedIndex != 0) && (MainPanorama.SelectedIndex != -1))
            {
                ApplicationBar.MenuItems.Add(_mItemBackToTop);
            }

        }


        // Activation de l'AppBar de détails d'une citation
        private void DetailsAppBarInitialization()
        {
            ApplicationBar.Buttons.Clear();
            ApplicationBar.MenuItems.Clear();

            ApplicationBar.Buttons.Add(_iButtonCopy);
            ApplicationBar.Buttons.Add(_iButtonClose);
            ApplicationBar.Buttons.Add(_iButtonShare);
            ApplicationBar.Mode = ApplicationBarMode.Default;
        }

        //private void OnFlickGridSearch(object sender, FlickGestureEventArgs e)
        //{
        //    if (e.Direction == System.Windows.Controls.Orientation.Vertical)
        //    {
        //        // De Haut en Bas
        //        if (e.VerticalVelocity > 0)
        //        {
        //            if (_gridFunction > -1)
        //            {
        //                if (_gridFunction == 0)
        //                {
        //                    SlideSearchToAuthorSB.Begin();
        //                    //else if (_gridFunction == 1)
        //                    //    SlideSuggToSearchSB.Begin();

        //                    _gridFunction--;
        //                }
        //            }
        //        }

        //        // De Bas en Haut
        //        else
        //        {
        //            if (_gridFunction < 2)
        //            {
        //                if (_gridFunction == -1)
        //                {
        //                    SlideAuthorToSearchSB.Begin();

        //                    //else if (_gridFunction == 0)
        //                    //    SlideSearchToSuggSB.Begin();

        //                    _gridFunction++;
        //                }
        //            }

        //        }
        //    }
        //}

        /// <summary>
        /// Lors d'un appuie sur l'icone de la flèche (en dessous de la loupe)
        /// Alternative au flick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Icon_Arrow_Down_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_gridFunction == 0)
            {
                SlideSearchToAuthorSB.Begin();
                Anime_icon_Arrow_Down_1.Begin();

                _gridFunction--;
            }
            else if (_gridFunction == -1)
            {
                SlideAuthorToSearchSB.Begin();
                Anime_icon_Arrow_Down_2.Begin();

                _gridFunction++;
            }
        }

        /// <summary>
        /// Navigue vers la page de paramères
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Settings_Click(object sender, EventArgs e)
        {
            if (App.ViewModel.CollectionQuotes.Count > 0)
            {
                Citations365.ViewModels.Quote quote = App.ViewModel.CollectionQuotes[0];
            }
            NavigationService.Navigate(new Uri("/Pages/Settings.xaml", UriKind.Relative));
        }

        /// <summary>
        ///  Navigue vers la page d'aide
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Help_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/HelpPage.xaml", UriKind.Relative));
        }

        // Action pour revenir en haut du LongListSelector
        private void MenuItem_BackToTop_Click(object sender, EventArgs e)
        {
            if (MainPanorama.SelectedIndex == 1)
            {
                if (App.ViewModel.CollectionQuotes.ElementAt(0) != null)
                {
                    _BackToTop = true;
                    LongListMainQuotes.ScrollTo(App.ViewModel.CollectionQuotes.ElementAt(0)); // Retour au premier élément 
                }
                _BackToTop = false;
            }
            else if ((MainPanorama.SelectedIndex == 2) && (App.ViewModel.CollectionQuotesSearch.Count > 0))
            {
                if (App.ViewModel.CollectionQuotesSearch.ElementAt(0) != null)
                {
                    _BackToTop = true;
                    LongListSearchResults.ScrollTo(App.ViewModel.CollectionQuotesSearch.ElementAt(0));
                }
                _BackToTop = false;
            }
        }

        // Rafraîchit les citations
        private void MenuItem_Refresh_Click(object sender, EventArgs e)
        {
            if (!App.ViewModel._Offline)
            {
                if ((MainPanorama.SelectedIndex == 0) || (MainPanorama.SelectedIndex == 1))
                {
                    // Affichage des deux ProgressIndicator
                    ProgressIDay.IsBusy = true;
                    ShowProgressBarTraySystem("Rafraîchissement des données...");

                    // Supprimer les textblocks du jour
                    TBAuthorDay.Text = "";
                    TBDateDay.Text = "";
                    TBQuoteDay.Text = "";

                    // Vider la Collection
                    App.ViewModel.CollectionQuotes.Clear();

                    // Appeler la méthode de chargement des citations
                    _IsLoaded = false;
                    App.ViewModel.LoadData();
                    BringMainQuotes();


                    // Background
                    App.ViewModel.ListPicturesFlickr.Clear();
                    CheckBackground(true);
                }
                else if (MainPanorama.SelectedIndex == 2)
                {
                    if ((_query != "") && (_query != null)
                        && (GridSearch.Visibility == System.Windows.Visibility.Collapsed)
                        && (LongListSearchResults.Visibility == System.Windows.Visibility.Visible))
                    {
                        _queryNumber = 1;
                        SearchStarted();
                        _cancelSearch = false;
                    }
                }
            }
            else
            {
                MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
            }
        }

        private void MenuItem_SaveImg_Click(object sender, EventArgs e)
        {
            ShowProgressBarTraySystem("Sauvegarde de l'image...");

            if (ImageBackground.ImageSource != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {

                        Microsoft.Xna.Framework.Media.MediaLibrary myMediaLibrary = new Microsoft.Xna.Framework.Media.MediaLibrary();
                        BitmapImage bmpI = (BitmapImage)ImageBackground.ImageSource;
                        Byte[] tabByte = ConvertToBytes(bmpI);
                        myMediaLibrary.SavePicture("saved_image_" + DateTime.Now, tabByte);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                });

            }

            HideProgressBarTrysystem();
        }

        public static byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            byte[] data;
            // Get an Image Stream
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                WriteableBitmap btmMap = new WriteableBitmap(bitmapImage);

                // write an image into the stream
                Extensions.SaveJpeg(btmMap, ms,
                    bitmapImage.PixelWidth, bitmapImage.PixelHeight, 0, 100);

                // reset the stream pointer to the beginning
                ms.Seek(0, 0);
                //read the stream into a byte array
                data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
            }
            //data now holds the bytes of the image
            return data;
        }

    }
}