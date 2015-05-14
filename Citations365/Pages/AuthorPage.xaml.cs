using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Citations365.ViewModels;
using System.Windows.Media.Imaging;
using MetroInMotionUtils;
using Microsoft.Phone.Tasks;
using Windows.Phone.Speech.Synthesis;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Windows.Media;
using Coding4Fun.Toolkit.Controls;

namespace Citations365.Pages
{
    public partial class AuthorPage : PhoneApplicationPage
    {
        public string _authorLink = "";
        public string _authorName = "";
        string _alpha = ""; // juste une variable temporaire pour les termes des citations
        string _lastAuthor = "NA";
        int _currentImg = 0;
        //bool _stopInfosImgWait = false;
        bool _speechStarted = false;
        public SpeechSynthesizer _synthAuthor = new SpeechSynthesizer();

        // APPLICATION BAR
        #region AppBar
        ApplicationBarMenuItem _backgroundOn = new ApplicationBarMenuItem()
        {
            Text = "activer l'arrière plan",
        };
        ApplicationBarMenuItem _backgroundOff = new ApplicationBarMenuItem()
        {
            Text = "désactiver l'arrière plan",
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

        // ELEMENTS POUR LA GRILLE DE DETAILS D'UNE CITATION
        #region detailsQuote
        // WrapPanel pour le détails d'une citation
        WrapPanel _wrapPanel = new WrapPanel()
        {
            Margin = new Thickness(12),
            Orientation = System.Windows.Controls.Orientation.Horizontal,
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


        TextBlock tblock;
        Quote _myQuote = new Quote()
        {
            content = "",
            author = "",
            authorlink = "",
            date = "",
            link = "",
            reference = "",
        };

        // these two fields fully define the zoom state:
        private double TotalImageScale = 1d;
        private Point ImagePosition = new Point(0, 0);

        private const double MAX_IMAGE_ZOOM = 5;
        private Point _oldFinger1;
        private Point _oldFinger2;
        private double _oldScaleFactor;

        private Image _imageScreenTemplate;


        // CONSTRUCTEUR
        public AuthorPage()
        {
            InitializeComponent();
            InitializeParams();
            this.Loaded -= authorLoaded;
            this.Loaded += authorLoaded;
            LongListAuthorQuotes.ItemsSource = App.ViewModel.CollectionQuotesAuthor;
            ApplicationBar.StateChanged += ApplicationBarStateChange;

            InitializeAppBar();
        }

        // Méthode initalisant les paramètres de la page
        private void InitializeParams()
        {
            ImageMyPivot.ImageSource = null; // arrière plan - image auteur
            //App.ViewModel._AuthorBio = "";
            //App.ViewModel._AuthorQuote = "";
            App.ViewModel.CollectionQuotesAuthor.Clear();
            App.ViewModel.CollectionAuthorPictures.Clear();
            LongListAuthorQuotes.Visibility = System.Windows.Visibility.Collapsed;

            GridQuote.Visibility = System.Windows.Visibility.Collapsed;
            TBQuote.Visibility = System.Windows.Visibility.Collapsed;
            Quote0.Visibility = System.Windows.Visibility.Collapsed;
            Quote1.Visibility = System.Windows.Visibility.Collapsed;
            Quote2.Visibility = System.Windows.Visibility.Collapsed;
            Quote3.Visibility = System.Windows.Visibility.Collapsed;
        }


        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)              
        {
            if (_speechStarted)
            {
                _synthAuthor.CancelAll();
                e.Cancel = true;
            }

            if (ShareBar.Visibility == System.Windows.Visibility.Visible)
            {
                //TRANSITION DU LAYOUTROOT
                SlideTransition sTransition = new SlideTransition();
                sTransition.Mode = SlideTransitionMode.SlideDownFadeIn;
                ITransition transition = sTransition.GetTransition(LayoutRoot);
                transition.Begin();
                //-----------

                //TRANSITION DE LA SHAREBAR
                SlideTransition sTransition2 = new SlideTransition();
                sTransition2.Mode = SlideTransitionMode.SlideDownFadeOut;
                ITransition transition2 = sTransition2.GetTransition(ShareBar);
                transition2.Completed += delegate
                {
                    transition.Stop();
                    ShareBar.Visibility = System.Windows.Visibility.Collapsed;  // Masque la barre de partage
                };
                transition2.Begin();
                //-----------

                GridCache.Visibility = System.Windows.Visibility.Collapsed; // masque la grille de masquage

                MyPivot.IsEnabled = true; // réactive l'intéraction avec le panorama
                ApplicationBar.IsVisible = true; // affiche l'application bar à nouveau
                SystemTray.IsVisible = true;

                e.Cancel = true; // notifie qu'on a géré l'évent
            }

            else if(FlipViewAuthor.Visibility == System.Windows.Visibility.Visible)
            {
                HideFlipView();
                e.Cancel = true;
            }
            //else if (ImageContainer.Visibility == System.Windows.Visibility.Visible)
            //{
            //    //Si la popup est affichée, on la masque
            //    CloseImageFullView();


            //    // Annule l'action du bouton Retour (empêche de sortir de l'app)
            //    e.Cancel = true;
            //}

            

            // Si les détails d'une citations sont affichés
            else if (GridQuoteDetails.Visibility == System.Windows.Visibility.Visible)
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

            else
            {
                // Si on fait un retour en arrière depuis la page d'auteur,
                // on veut supprimer la mémoire de la navigation (dans MainPage -> OnNavigatedTo)
                // afin d'éviter le chargement de données inutiles

                // EX. de scénarios : MainPage -> AuthorPage A -> MainPage -> AuthorPage B -> MainPage
                // En faisant un base.OnBackKeyPress(e) on veut le déroulement suivant :
                // MainPage -> AuthorPage B -> MainPage -> Fermeture de l'app

                App.ViewModel._RemoveAllEntriesFromBackStack = true;
            }
            //base.OnBackKeyPress(e);         
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            object author;

            if (PhoneApplicationService.Current.State.TryGetValue("authorName", out author))
            {
                this.DataContext = author;
                _authorName = (string)author;
                if(_authorName.Contains("\n"))
                {
                    _authorName = _authorName.Replace("\n", "")
                                                .Replace("  ", "");
                }
                //ItemFlyInAndOutAnimations.TitleFlyIn(PageTitle);
                if (_authorName.StartsWith("de "))
                {
                    _authorName =_authorName.Substring(3);
                }

                //MyPivot.Title = new MyObject
                //{
                //    content = _authorName.ToUpper(),
                //    size = 30,
                //};
                TopBarAuthor.Text = _authorName.ToUpper();
            }

            if (PhoneApplicationService.Current.State.TryGetValue("authorLink", out author))
            {
                this.DataContext = author;
                _authorLink = (string)author;
            }

            base.OnNavigatedTo(e);
            //authorLoaded();
        }


        private void authorLoaded(object sender, EventArgs e)
        {
            if (_lastAuthor != _authorName)
            {
                _lastAuthor = _authorName;

                if ((_authorLink != null) &&
                    (_authorName != null) && (_authorName != "") && (!_authorName.Contains("Anonyme"))
                    && (!_authorName.Contains("Proverbe")))
                {
                    // Récupère des images (3) de l'auteur de la citation
                    MainViewModel.eventHandlerGetAuthorPictures -= RequestEndedAuthorPictures;
                    MainViewModel.eventHandlerGetAuthorPictures += RequestEndedAuthorPictures;
                    App.ViewModel.GetAuthorPicture(_authorName);

                    BringAuthorInfos(); // Récupère la biographie de l'auteur
                    BringAuthorQuotes(); // Récupère les citations de l'auteur
                    BrindAuthorWorks();
                }
                else
                {
                    CouldnotLoadInfos(false);
                }
            }
        }

        private async void BrindAuthorWorks()
        {
            ProgressAuthor.Text = "Récupération des oeuvres...";
            ProgressAuthor.IsVisible = true;

            bool res = await App.ViewModel.GetAuthorWork(_authorLink, _authorName);

            if (res)
            {
                TBNoWork.Visibility = System.Windows.Visibility.Collapsed;
                LongListAuthorWorks.Visibility = System.Windows.Visibility.Visible;
                LongListAuthorWorks.ItemsSource = App.ViewModel.CollectionAuthorWorks;

            }
            ProgressAuthor.IsVisible = false;
        }


        private void WorkItem_Loaded(object sender, RoutedEventArgs e)
        {
            //TextBlock b = sender as TextBlock;

            //if (b == null) return;
            //if (b.Text == null || b.Text.Length < 4)
            //{
            //    b.Visibility = System.Windows.Visibility.Collapsed;
            //}
            
        }

        

        public async void BringAuthorInfos()
        {
            List<string> results = await App.ViewModel.GetAuthorInfos(_authorLink);
            if (results.Count > 0)
            {
                TopBarGenre.Text = results.ElementAt(2);
                TopBarBirth.Text = results.ElementAt(3);
                TopBarDeath.Text = results.ElementAt(4);

                //AnimeGenreEtc();
            }
            else
            {
                // si la liste ne contient aucun élément
                // alors on n'a rien trouvé sur l'auteur,
                // et on notifie l'utilisateur
                CouldnotLoadInfos(true);
                return;
            }


            // Si le texte est très grand, on le met dans 2 HtmlTextBlock
            if (results.First().Length > 2000)
            {

                string abc = results.First().Substring(0, 2000);
                string xyz = results.First().Substring(2000);

                if (abc.ElementAt(1999).ToString() != " ")
                {
                    int compt = 1998;
                    while (abc.ElementAt(compt).ToString() != " ")
                    {
                        abc = abc.Substring(0, compt);
                        xyz = results.First().Substring(compt);
                        compt--;
                    }
                }

                AnimeAuthorStuff(abc, xyz);
            }

            // Vérifie que le texte est bien une biographie
            // (certains auteurs ont une fiche vide avec des espaces)
            else if (results.First().Length > 100)
            {
                AnimeAuthorStuff(results.First(), "");
            }
            else
            {
                CouldnotLoadInfos(true);
            }


            if ((results.ElementAt(1) != "") && (results.ElementAt(1) != null))
            {
                GridQuote.Visibility = System.Windows.Visibility.Visible;
                TBQuote.Html = results.ElementAt(1);
            }

            ProgressAuthor.Text = "Chargement des citations de l'auteur";

            if ((App.ViewModel._TTSIsActivated) && (_speechStarted == false))
            {
                _speechStarted = true;
                TTSAuthor();
            }
        }


        private  void AnimeAuthorStuff(string abc, string xyz) {
            //ANIMATIONS
            BioSB1.Begin();
            BioSB1.Completed += delegate
            {
                //Animation du résumé de la biographie
                TBGiveMeMore.Visibility = System.Windows.Visibility.Visible;


                //Animation de la citation de l'auteur
                BioQuoteSB.Begin();
                BioQuoteSB.Completed += delegate
                {
                    TBQuote.Visibility = System.Windows.Visibility.Visible;
                    //Animation des guillemets
                    QuotesSB.Begin();
                    QuotesSB.Completed += delegate
                    {
                        Quote0.Visibility = System.Windows.Visibility.Visible;
                        Quote1.Visibility = System.Windows.Visibility.Visible;
                        Quote2.Visibility = System.Windows.Visibility.Visible;
                        Quote3.Visibility = System.Windows.Visibility.Visible;
                    };
                };
            };
            //FIN ANIMATIONS


            if (xyz.Length > 1)
            {
                TBBio2.Html = xyz;
                TBBio2.Visibility = System.Windows.Visibility.Visible;
            }

            TBBio.Html = abc;
            
            TBGiveMeMore.Text += "En savoir plus sur " + _authorName;
            ProgressIAuthorInfos.IsBusy = false;
            ProgressIAuthorInfos.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void AnimeGenreEtc()
        {
            //Anime_TopBar_In.Begin();
            //Anime_TopBar_Out.BeginTime = TimeSpan.FromSeconds(4);
            //Anime_TopBar_Out.Begin();
        }

        public async void BringAuthorQuotes()
        {
            await App.ViewModel.GetAuthorQuotes(_authorLink, _authorName);

            if (App.ViewModel.CollectionQuotesAuthor.Count > 0)
            {
                LongListAuthorQuotes.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                TextBlock tblock = new TextBlock();
                tblock.Text = "Il n'y a pas de citations pour cet auteur.";
                tblock.FontSize = 30;
                tblock.TextWrapping = TextWrapping.Wrap;
                tblock.Margin = new Thickness(12, 0, 0, 0);
                tblock.FontFamily = new System.Windows.Media.FontFamily("Segoe WP Light");
                GridCitations.Children.Add(tblock);
            }
            ProgressAuthor.IsVisible = false;
        }

        public void RequestEndedAuthorInfos(object sender, EventArgs e)
        {
            // Si le texte est très grand, on le met dans 2 HtmlTextBlock
            if (App.ViewModel._AuthorBio.Length > 2000)
            {
                string abc = App.ViewModel._AuthorBio.Substring(0, 2000);
                string xyz = App.ViewModel._AuthorBio.Substring(2000);

                if (abc.ElementAt(1999).ToString() != " ")
                {
                    int compt = 1998;
                    while (abc.ElementAt(compt).ToString() != " ")
                    {
                        abc = abc.Substring(0, compt);
                        xyz = App.ViewModel._AuthorBio.Substring(compt);
                        compt--;
                    }
                }

                //ANIMATIONS
                BioSB1.Begin();
                BioSB1.Completed += delegate
                {
                    //Animation du résumé de la biographie
                    TBBio2.Visibility = System.Windows.Visibility.Visible;
                    TBGiveMeMore.Visibility = System.Windows.Visibility.Visible;
                    //Animation de la citation de l'auteur
                    BioQuoteSB.Begin();
                    BioQuoteSB.Completed += delegate
                    {
                        TBQuote.Visibility = System.Windows.Visibility.Visible;
                        //Animation des guillemets
                        QuotesSB.Begin();
                        QuotesSB.Completed += delegate
                        {
                            Quote0.Visibility = System.Windows.Visibility.Visible;
                            Quote1.Visibility = System.Windows.Visibility.Visible;
                            Quote2.Visibility = System.Windows.Visibility.Visible;
                            Quote3.Visibility = System.Windows.Visibility.Visible;
                        };
                    };
                };
                //FIN ANIMATIONS

                TBBio.Html = abc;
                TBBio2.Html = xyz;
                TBGiveMeMore.Text += "En savoir plus sur " + _authorName;
                ProgressIAuthorInfos.IsBusy = false;
                ProgressIAuthorInfos.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Vérifie que le texte est bien une biographie
            // (certains auteurs ont une fiche vide avec des espaces)
            else if (App.ViewModel._AuthorBio.Length > 100)
            {
                //ANIMATIONS
                BioSB1.Begin();
                BioSB1.Completed += delegate
                {
                    //Animation de la biographie de l'auteur
                    TBBio2.Visibility = System.Windows.Visibility.Visible;
                    TBGiveMeMore.Visibility = System.Windows.Visibility.Visible;
                    //Animation de la citation de l'auteur
                    BioQuoteSB.Begin();
                    BioQuoteSB.Completed += delegate
                    {
                        TBQuote.Visibility = System.Windows.Visibility.Visible;
                        //Animation des guillemets de la citation
                        QuotesSB.Begin();
                        QuotesSB.Completed += delegate
                        {
                            Quote0.Visibility = System.Windows.Visibility.Visible;
                            Quote1.Visibility = System.Windows.Visibility.Visible;
                            Quote2.Visibility = System.Windows.Visibility.Visible;
                            Quote3.Visibility = System.Windows.Visibility.Visible;
                        };
                    };
                };

                TBBio.Html = App.ViewModel._AuthorBio;
                TBGiveMeMore.Text += "En savoir plus sur " + _authorName;
                ProgressIAuthorInfos.IsBusy = false;
                ProgressIAuthorInfos.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                CouldnotLoadInfos(true);
            }


            if ((App.ViewModel._AuthorQuote != "") &&(App.ViewModel._AuthorQuote != null))
            {
                GridQuote.Visibility = System.Windows.Visibility.Visible;
                TBQuote.Html = App.ViewModel._AuthorQuote;
            }

            ProgressAuthor.Text = "Chargement des citations de l'auteur";

            if ((App.ViewModel._TTSIsActivated) && (_speechStarted == false))
            {
                _speechStarted = true;
                TTSAuthor();
            }
        }

        public void RequestEndedAuthorPictures(object sender, EventArgs e)
        {
            if (App.ViewModel.CollectionAuthorPictures.Count > 0)
            {
                Uri imageLink = new Uri(App.ViewModel.CollectionAuthorPictures.ElementAt(0), UriKind.Absolute);



                // Définit l'image récupérée de Bing comme le fond du Pivot
                // Résoud le problème de Thread et d'accès au processus
                Dispatcher.BeginInvoke(() =>
                    {
                        
                        // Définit l'image du Portrait (grâce au résultats de la recherche d'images de Bing)
                        if (imageLink != null)
                        {

                            ImagePortrait.ImageSource = new BitmapImage(imageLink);
                            PortraitSB.BeginTime = TimeSpan.FromSeconds(2);
                            PortraitSB.Begin();
                            
                            // Récupère l'image de l'auteur si l'option est active
                            if (App.ViewModel._AuthorBackgroundActivated)
                            {
                                ImageMyPivotSB0.BeginTime = TimeSpan.FromSeconds(2);
                                ImageMyPivot.ImageSource = new BitmapImage(imageLink);

                                ImageMyPivotSB0.Begin();
                                ImageMyPivotSB0.Completed += delegate
                                {
                                    ImageMyPivot.Opacity = 0.3;
                                };
                            }

                        }
                    });
                
                
            }
        }

        void CouldnotLoadInfos(bool partial)
        {
            ProgressAuthor.IsVisible = false;
            ProgressIAuthorInfos.IsBusy = false;
            ProgressIAuthorInfos.Visibility = System.Windows.Visibility.Collapsed;

            TBBio.Html = "Nous n'avons pas d'information pour cet auteur mais vous pouvez en rechercher sur bing.";

            // Vérifie que l'auteur n'a pas de citations
            // Ajout d'une citation disant qu'il n'y en a pas
            // Si partial = faux
            //  Alors CoundnotLoadInfos() a été appelée quand on n'a pas obtenir le lien et/ou le nom de l'auteur
            // Si partial = vrai
            //  Alors CoundnotLoadInfos() a été appelée alors qu'on a obtenu un lien vers la fiche de l'auteur
            //  On va déterminer si on affiche ou pas le LongListSelector dans la méthode RequestEndedAuthorQuotes();
            if (!partial)
            {
                TBNoQuotes.Visibility = System.Windows.Visibility.Visible;
            }

            //ANIMATIONS
            BioSB1.Begin();
            BioSB1.Completed += delegate
            {
                TBGiveMeMore.Text += "En savoir plus sur " + _authorName;
                TBGiveMeMore.Visibility = System.Windows.Visibility.Visible;
            };
            //FIN ANIMATIONS

            // Récupération d'une image de l'auteur
            if ((ImageMyPivot.ImageSource == null) &&
                (!_authorName.Contains("Anonyme")) && (_authorName != "") && (_authorName != null))
            {
                App.ViewModel.GetAuthorPicture(_authorName);
                MainViewModel.eventHandlerGetAuthorPictures -= RequestEndedAuthorPictures;
                MainViewModel.eventHandlerGetAuthorPictures += RequestEndedAuthorPictures;
            }
        }


        // Text-To-Speech
        // Voix annonçant la date et la citation du jour
        private async void TTSAuthor()
        {
            SpeechShowSB.BeginTime = TimeSpan.FromSeconds(1.5);
            SpeechShowSB.Begin();
            SpeechShowSB.Completed += delegate
            {
                SpeechSB.Begin();
                SpeechSB.RepeatBehavior = RepeatBehavior.Forever;
                GridSpeech.Visibility = System.Windows.Visibility.Visible;
            };

            try
            {
                string speech = "Voici quelques informations à propos " + _authorName;

                if ((TBBio.Html != null) && (TBBio.Html != ""))
                {
                    string abc = TBBio.Html.Replace("<p>", "").Replace("</p>", "");

                    await _synthAuthor.SpeakTextAsync(speech);
                    await _synthAuthor.SpeakTextAsync(abc);

                    if ((TBBio2.Html != null) && (TBBio2.Html != ""))
                    {
                        string xyz = TBBio2.Html.Replace("<p>", "").Replace("</p>", "");
                        await _synthAuthor.SpeakTextAsync(xyz);
                    }

                    SpeechSB.Stop();
                    SpeechHideSB.Begin();
                    SpeechHideSB.Completed += delegate
                    {
                        GridSpeech.Visibility = System.Windows.Visibility.Collapsed;
                        _speechStarted = false;
                    };
                }
                
                
            }
            catch
            {
                //MessageBoxResult mbox = MessageBox.Show("Une erreur est survenue lors de la lecture de la biographie");
                SpeechSB.Stop();
                SpeechShowSB.Stop();

                SpeechHideSB.Begin();
                SpeechHideSB.Completed += delegate
                {
                    GridSpeech.Visibility = System.Windows.Visibility.Collapsed;
                    _speechStarted = false;
                };
                
            }
        }


        private void SpeechStop_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_speechStarted)
            {
                _synthAuthor.Dispose();
                //synthAuthor.CancelAll();
                _speechStarted = false;
            }
            else
            {
                //TTSAuthor();
            }
        }

        private void LongListAuthorQuotes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is Quote)
            {
                _myQuote = (Quote)e.AddedItems[0];
                QuoteSelected(1);
            }
        }

        // Fonction de partage des citations de la liste lors d'un Double Tap
        private void DoubleTap_Quote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Test si on a bien récupéré une citation
            if (_myQuote != null)
            {
                // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                SystemTray.IsVisible = false;

                GridCache.Visibility = System.Windows.Visibility.Visible;

                //TRANSITION BARRE DE PARTAGE
                // Afficher le StackPanel de partage (avec animation)
                SlideTransition stransition = new SlideTransition();
                stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition = stransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                };
                transition.Begin();
                //TRANSITION DU LAYOUTROOT
                SlideTransition sTransition2 = new SlideTransition();
                sTransition2.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition2 = sTransition2.GetTransition(LayoutRoot);
                transition2.Begin();
                //-----------

                ShareBar.Visibility = System.Windows.Visibility.Visible;


                // Désactive l'intéraction avec le panorama
                MyPivot.IsEnabled = false;

                // Masque l'AppBar
                //ApplicationBar.IsVisible = false;
            }
        }

        // Fonction de partage des citations de la liste lors d'un Hold
        // NOTE: MEME ACTION QUE LE DOUBLE TAP
        private void Hold_Quote(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Si on veut partager la citation principale (affiché au-dessus de sa bio)
            if ((MyPivot.SelectedIndex == 0) && (TBQuote.Html!=null)&&(TBQuote.Html!=""))
            {
                _myQuote.content = TBQuote.Html.Replace("<div class=\"txt\">", "").Replace("</div>", "").Replace("&#039;", "'");
                //myQuote.author = authorName;
                // Test si on a bien récupéré une citation

                // CODE REPETITIF - A MODIFIER
                ShareBarShow();

            }

            // Sinon, on veut partager une de ses citations récupérées par la méthode
            else if (MyPivot.SelectedIndex==1)
            {
                if (sender != null)
                {
                    StackPanel stack = (StackPanel)sender;
                    if (stack.Children.ElementAt(0) != null)
                    {
                        //Récupération de la citation
                        tblock = (TextBlock)stack.Children.ElementAt(0);
                        _alpha = tblock.Text;
                        _myQuote.content = _alpha;

                        // Test si on a bien récupéré une citation
                        ShareBarShow();
                    }
                }
            }
        }

        private void ShareBarShow()
        {
            // Test si on a bien récupéré une citation
            if (_myQuote != null)
            {
                //Masquer le SystemTray
                SystemTray.IsVisible = false;
                // Afficher la Grid de masquage pour éviter les interactions avec le Panorama
                GridCache.Visibility = System.Windows.Visibility.Visible;


                //TRANSITION BARRE DE PARTAGE
                // Afficher le StackPanel de partage (avec animation)
                SlideTransition stransition = new SlideTransition();
                stransition.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition = stransition.GetTransition(ShareBar);
                transition.Completed += delegate
                {
                    transition.Stop();
                };
                transition.Begin();
                //TRANSITION DU LAYOUTROOT
                SlideTransition sTransition2 = new SlideTransition();
                sTransition2.Mode = SlideTransitionMode.SlideUpFadeIn;
                ITransition transition2 = sTransition2.GetTransition(LayoutRoot);
                transition2.Begin();
                //-----------
                ShareBar.Visibility = System.Windows.Visibility.Visible;


                // Désactive l'intéraction avec le panorama et l'appbar
                MyPivot.IsEnabled = false;
                ApplicationBar.IsVisible = false;
            }
        }

        // Renvoie sur Bing Search pour en savoir plus sur l'auteur
        private void TBGiveMeMore_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SearchTask searchTask = new SearchTask();
            searchTask.SearchQuery = _authorName.Replace("  ", "");
            searchTask.Show();

            SystemTray.IsVisible = true;
        }

        private void GetMeMorePicture_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SearchTask searchTask = new SearchTask();
            searchTask.SearchQuery = "picture:" + _authorName;
            searchTask.Show();

            SystemTray.IsVisible = true;
        }


        // -> Partage sur twitter, Facebook, etc
        private void share_twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_myQuote != null)
            {

                ShareLinkTask shareLink = new ShareLinkTask();

                shareLink.Title = "Citation";
                shareLink.Message = _myQuote.content + " - " + _authorName;
                shareLink.LinkUri = new Uri("http://www.evene.fr");
                shareLink.Show();
            }

            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;
            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;
            // Réactive l'interaction avec le panorama
            MyPivot.IsEnabled = true;
            // Affiche (à nouveau) l'AppBar
            ApplicationBar.IsVisible = true;
            SystemTray.IsVisible = true;
        }

        // -> Partage par email
        private void share_mail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_myQuote != null)
            {
                EmailComposeTask email = new EmailComposeTask()
                {
                    Subject = "Citation",
                    Body = _myQuote.content + " - " + _authorName,
                    To = "",
                };
                email.Show();
            }
            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;

            // Réactive l'interaction avec le panorama
            MyPivot.IsEnabled = true;
            SystemTray.IsVisible = true;
            //Affiche (à nouveauu) l'AppBar
            ApplicationBar.IsVisible = true;
        }

        // -> Partage par sms
        private void share_sms_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_myQuote != null)
            {
                SmsComposeTask sms = new SmsComposeTask()
                {
                    Body = _myQuote.content + " - " + _authorName,
                    To = "",
                };
                sms.Show();
            }
            // Masquer la Grid de masquage
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Masquer le StackPanel de partage
            ShareBar.Visibility = System.Windows.Visibility.Collapsed;

            // Réactive l'interaction avec le panorama
            MyPivot.IsEnabled = true;
            SystemTray.IsVisible = true;
            // Affiche (à nouveau) l'AppBar
            ApplicationBar.IsVisible = true;
        }

        // Masque la barre de partage quand on double tap sur la grille de masquage
        private void GridCache_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ShareBar.Visibility == System.Windows.Visibility.Visible)
            {
                //TRANSITION DU LAYOUTROOT
                SlideTransition sTransition = new SlideTransition();
                sTransition.Mode = SlideTransitionMode.SlideDownFadeIn;
                ITransition transition = sTransition.GetTransition(LayoutRoot);
                transition.Begin();
                //-----------

                //TRANSITION DE LA SHAREBAR
                SlideTransition sTransition2 = new SlideTransition();
                sTransition2.Mode = SlideTransitionMode.SlideDownFadeOut;
                ITransition transition2 = sTransition2.GetTransition(ShareBar);
                transition2.Completed += delegate
                {
                    transition.Stop();
                    ShareBar.Visibility = System.Windows.Visibility.Collapsed;  // Masque la barre de partage
                };
                transition2.Begin();
                //-----------

                GridCache.Visibility = System.Windows.Visibility.Collapsed; // masque la grille de masquage

                MyPivot.IsEnabled = true; // réactive l'intéraction avec le panorama
                ApplicationBar.IsVisible = true; // affiche l'application bar à nouveau
                SystemTray.IsVisible = true;
            }
        }

        // Enregistrement sur l'évènement StateChanged de l'application bar
        void ApplicationBarStateChange(object sender, ApplicationBarStateChangedEventArgs e)
        {
            bool menuisvisible = e.IsMenuVisible;
            if (menuisvisible)
            {
                ApplicationBar.Opacity = 0.99;
            }
            else
            {
                ApplicationBar.Opacity = 0.0;
            }
        }


        // GRILLE DE DETAILS D'UNE CITATION (METHODES)
        // Suite de méthodes à effectuer pour afficher la Grid de détails
        private void ActivePopupDetails()
        {
            // Désactive l'intéraction avec le panorama et affiche les détails de la citation
            MyPivot.IsEnabled = false;
            GridCache.Visibility = System.Windows.Visibility.Visible;

            // Active l'AppBar pour les détails de la citation
            DetailsAppBarInitialization();

            TBDate_QuoteDetails.Text = _myQuote.date;
            TBAuthor_QuoteDetails.Text = "";

            if (MyPivot.SelectedIndex == 1)
            {
                LongListAuthorQuotes.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        // Suite de méthodes à effectuer pour masquer la Grid de détails
        private void DesactivePopupDetails()
        {
            // Réactivation des éléments
            MyPivot.IsEnabled = true;
            GridCache.Visibility = System.Windows.Visibility.Collapsed;

            // Réinitialisation de L'AppBar
            MainAppBarInitialization();
            
            // Affichage des citations dans le LongListSelector
            // LongListAuthorQuotes.Visibility = System.Windows.Visibility.Visible;


            if (MyPivot.SelectedIndex == 1)
            {
                LongListAuthorQuotes.Visibility = System.Windows.Visibility.Visible;
                LongListAuthorQuotes.SelectedItem = -1;
            }

            // Masque les différents éléments contenus dans la Grille (popup)
            GridContent_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridDate_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridAuthor_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridQuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
        }

        // Méthode éffectuée après un LongListSelector_SelectionChanged
        // La valeur de l'entier passé en paramètre correspond à l'index du panorama sur lequel on est
        private void QuoteSelected(int index)
        {
            // Evite le lancement de la méthode dans le cas où l'utilisateur appuie sur la liste à 15% visible (à droite) dans le Panorama
            if (index == MyPivot.SelectedIndex)
            {
                // Si la Grid de détails de la citation est bien masquée
                // Et si on est bien sur l'index 1 du Panorama
                if ((GridQuoteDetails.Visibility == System.Windows.Visibility.Collapsed)
                    && (MyPivot.SelectedIndex != 0))
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
                }
            }
        }


        // Méthode quand on appuie sur un mot (détails d'une citation)
        private void block_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender != null)
            {
               // Retourner sur l'accueil MainPanorama
               // Lancer la recherche de citation avec le mot clé enregistré
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                //isLoaded = true;
                TextBlock tblock = sender as TextBlock;

                if ((tblock != null) && (tblock.Text != null) && (tblock.Text != "")
                    && (tblock.Text.Length > 2))
                {
                    App.ViewModel._QueryTermFromAuthor = tblock.Text.Replace(",", "").Replace(".", "").Replace("\"", "");
                }
            }
        }
        //----
        // FIN --- METHODES DES DETAILS D'UNE CITATION

        // Méthode initialisant les méthode de l'ApplicationBar et l'AppBar
        private void InitializeAppBar()
        {
            // EventHandlers
            _backgroundOff.Click += BackgroundOffMenuItem_Click;
            _backgroundOn.Click += BackgroundOnMenuItem_Click;

            _iButtonClose.Click += IconButtonClose_Click;
            _iButtonCopy.Click += IconButtonCopy_Click;
            _iButtonShare.Click += IconButtonShare_Click;

            // Initialisation de l'AppBar
            MainAppBarInitialization();
        }

        private void MainAppBarInitialization()
        {
            ApplicationBar.Buttons.Clear();
            ApplicationBar.MenuItems.Clear();

            ApplicationBar.Opacity = 0;
            ApplicationBar.Mode = ApplicationBarMode.Minimized;

            if (App.ViewModel._AuthorBackgroundActivated)
            {
                ApplicationBar.MenuItems.Add(_backgroundOff);
            }
            else
            {
                ApplicationBar.MenuItems.Add(_backgroundOn);
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
            ApplicationBar.Opacity = 0.99;
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
        // ! Géré différemment !
        private void IconButtonShare_Click(object sender, EventArgs e)
        {
            QuoteDetailsShowSB.Stop();

            // DesactivePopupDetails();
            // Masque les différents éléments contenus dans la Grille (popup)
            GridContent_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridDate_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridAuthor_QuoteDetails.Visibility = System.Windows.Visibility.Collapsed;
            GridQuoteDetails.Visibility = System.Windows.Visibility.Collapsed;

            // Réinitialisation de L'AppBar
            MainAppBarInitialization();

            if (MyPivot.SelectedIndex == 1)
            {
                LongListAuthorQuotes.Visibility = System.Windows.Visibility.Visible;
                LongListAuthorQuotes.SelectedItem = -1;
            }

            ShareBarShow();
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


        // IMAGE AUTEUR (METHODES)
        // Désactive l'image en arrière plan de l'auteur
        private void BackgroundOffMenuItem_Click(object sender, EventArgs e)
        {
            App.ViewModel._AuthorBackgroundActivated = false;
            ApplicationBar.MenuItems.RemoveAt(0);
            ApplicationBar.MenuItems.Add(_backgroundOn);
            // Enregistre les données
            App.ViewModel.SaveData();

            ImageMyPivot.ImageSource = null;
        }


        // Active l'image en arrière plan de l'auteur
        private void BackgroundOnMenuItem_Click(object sender, EventArgs e)
        {
            App.ViewModel._AuthorBackgroundActivated = true;
            ApplicationBar.MenuItems.RemoveAt(0);
            ApplicationBar.MenuItems.Add(_backgroundOff);
            // Enregistre les données
            App.ViewModel.SaveData();

            if (App.ViewModel.CollectionAuthorPictures.Count > 0)
            {
                Uri imageLink = new Uri(App.ViewModel.CollectionAuthorPictures.ElementAt(0), UriKind.Absolute);

                ImageMyPivot.Opacity = 0;

                // Résoud le problème de Thread et d'accès au processus
                Dispatcher.BeginInvoke(() =>
                {
                    ImageMyPivotSB0.BeginTime = TimeSpan.FromSeconds(2);
                    ImageMyPivot.ImageSource = new BitmapImage(imageLink);

                    ImageMyPivotSB0.Begin();
                    ImageMyPivotSB0.Completed += delegate
                    {
                        ImageMyPivot.Opacity = 0.3;
                    };

                });
            }
            else
            {
                // Récupération d'une image de l'auteur
                if ((ImageMyPivot.ImageSource == null) &&
                    (!_authorName.Contains("Anonyme")) && (_authorName != "") && (_authorName != null))
                {
                    App.ViewModel.GetAuthorPicture(_authorName);
                    MainViewModel.eventHandlerGetAuthorPictures += new EventHandler(RequestEndedAuthorPictures);
                }
            }
        }

        #region Pinch2Zoom
        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            _oldFinger1 = e.GetPosition(_imageScreenTemplate, 0);
            _oldFinger2 = e.GetPosition(_imageScreenTemplate, 1);
            _oldScaleFactor = 1;
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            var scaleFactor = e.DistanceRatio / _oldScaleFactor;
            if (!IsScaleValid(scaleFactor))
                return;

            var currentFinger1 = e.GetPosition(_imageScreenTemplate, 0);
            var currentFinger2 = e.GetPosition(_imageScreenTemplate, 1);

            var translationDelta = GetTranslationDelta(
                currentFinger1,
                currentFinger2,
                _oldFinger1,
                _oldFinger2,
                ImagePosition,
                scaleFactor);

            _oldFinger1 = currentFinger1;
            _oldFinger2 = currentFinger2;
            _oldScaleFactor = e.DistanceRatio;

            UpdateImageScale(scaleFactor);
            UpdateImagePosition(translationDelta);
        }

        private void UpdateImage(double scaleFactor, Point delta)
        {
            TotalImageScale *= scaleFactor;
            ImagePosition = new Point(ImagePosition.X + delta.X, ImagePosition.Y + delta.Y);

            var transform = (CompositeTransform)_imageScreenTemplate.RenderTransform;
            transform.ScaleX = TotalImageScale;
            transform.ScaleY = TotalImageScale;
            transform.TranslateX = ImagePosition.X;
            transform.TranslateY = ImagePosition.Y;
        }



        private void OnDragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            var translationDelta = new Point(e.HorizontalChange, e.VerticalChange);

            if (IsDragValid(1, translationDelta))
                UpdateImagePosition(translationDelta);
        }

        private void OnDoubleTap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (TotalImageScale < 1.1)
            {
                UpdateImageScale(1.5);
                UpdateImagePosition(e.GetPosition(_imageScreenTemplate));
            }
            else
            {
                ResetImagePosition();
            }
        }

        /// <summary>
        /// Computes the translation needed to keep the image centered between your fingers.
        /// </summary>
        private Point GetTranslationDelta(
            Point currentFinger1, Point currentFinger2,
            Point oldFinger1, Point oldFinger2,
            Point currentPosition, double scaleFactor)
        {
            var newPos1 = new Point(
             currentFinger1.X + (currentPosition.X - oldFinger1.X) * scaleFactor,
             currentFinger1.Y + (currentPosition.Y - oldFinger1.Y) * scaleFactor);

            var newPos2 = new Point(
             currentFinger2.X + (currentPosition.X - oldFinger2.X) * scaleFactor,
             currentFinger2.Y + (currentPosition.Y - oldFinger2.Y) * scaleFactor);

            var newPos = new Point(
                (newPos1.X + newPos2.X) / 2,
                (newPos1.Y + newPos2.Y) / 2);

            return new Point(
                newPos.X - currentPosition.X,
                newPos.Y - currentPosition.Y);
        }

        /// <summary>
        /// Updates the scaling factor by multiplying the delta.
        /// </summary>
        private void UpdateImageScale(double scaleFactor)
        {
            TotalImageScale *= scaleFactor;
            ApplyScale();
        }

        /// <summary>
        /// Applies the computed scale to the image control.
        /// </summary>
        private void ApplyScale()
        {
            ((CompositeTransform)_imageScreenTemplate.RenderTransform).ScaleX = TotalImageScale;
            ((CompositeTransform)_imageScreenTemplate.RenderTransform).ScaleY = TotalImageScale;
        }

        /// <summary>
        /// Updates the image position by applying the delta.
        /// Checks that the image does not leave empty space around its edges.
        /// </summary>
        private void UpdateImagePosition(Point delta)
        {
            var newPosition = new Point(ImagePosition.X + delta.X, ImagePosition.Y + delta.Y);

            if (newPosition.X > 0) newPosition.X = 0;
            if (newPosition.Y > 0) newPosition.Y = 0;

            if ((_imageScreenTemplate.ActualWidth * TotalImageScale) + newPosition.X < _imageScreenTemplate.ActualWidth)
                newPosition.X = _imageScreenTemplate.ActualWidth - (_imageScreenTemplate.ActualWidth * TotalImageScale);

            if ((_imageScreenTemplate.ActualHeight * TotalImageScale) + newPosition.Y < _imageScreenTemplate.ActualHeight)
                newPosition.Y = _imageScreenTemplate.ActualHeight - (_imageScreenTemplate.ActualHeight * TotalImageScale);

            ImagePosition = newPosition;

            ApplyPosition();
        }

        /// <summary>
        /// Applies the computed position to the image control.
        /// </summary>
        private void ApplyPosition()
        {
            ((CompositeTransform)_imageScreenTemplate.RenderTransform).TranslateX = ImagePosition.X;
            ((CompositeTransform)_imageScreenTemplate.RenderTransform).TranslateY = ImagePosition.Y;
        }

        /// <summary>
        /// Resets the zoom to its original scale and position
        /// </summary>
        private void ResetImagePosition()
        {
            TotalImageScale = 1;
            ImagePosition = new Point(0, 0);
            ApplyScale();
            ApplyPosition();
        }

        /// <summary>
        /// Checks that dragging by the given amount won't result in empty space around the image
        /// </summary>
        private bool IsDragValid(double scaleDelta, Point translateDelta)
        {
            if (ImagePosition.X + translateDelta.X > 0 || ImagePosition.Y + translateDelta.Y > 0)
                return false;

            if ((_imageScreenTemplate.ActualWidth * TotalImageScale * scaleDelta) + (ImagePosition.X + translateDelta.X) < _imageScreenTemplate.ActualWidth)
                return false;

            if ((_imageScreenTemplate.ActualHeight * TotalImageScale * scaleDelta) + (ImagePosition.Y + translateDelta.Y) < _imageScreenTemplate.ActualHeight)
                return false;

            return true;
        }

        /// <summary>
        /// Tells if the scaling is inside the desired range
        /// </summary>
        private bool IsScaleValid(double scaleDelta)
        {
            return (TotalImageScale * scaleDelta >= 1) && (TotalImageScale * scaleDelta <= MAX_IMAGE_ZOOM);
        }

        #endregion Pinch2Zoom


        /// <summary>
        ///  Se produit quand l'utilisateur appuie sur la vignette de l'auteur. Affiche l'image en plein écran
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Portrait_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (FlipViewAuthor.Visibility == System.Windows.Visibility.Visible)
                return;

            // Si le FlipView contient déjà les items nécessaires, pas la peine d'en rajouter
            // On fait juste l'affichage
            if (FlipViewAuthor.Items.Count > 0)
            {
                Anime_FlipViewAuthor_In.Begin();
                FlipViewAppBar();
                return;
            }


            // AJOUTE LES ITEMS AU FLIPVIEW
            // ----------------------------
            // > On passe ici si le FlipView est encore vide
            // ----------------------------

            // Vérifie que la collections d'image n'est pas vide
            if (App.ViewModel.CollectionAuthorPictures.Count < 1) return;

            // Ajoute un FlipViewItem contenant une image pour chaque lien récupéré
            foreach (string pic in App.ViewModel.CollectionAuthorPictures)
            {
                Image img = new Image();
                img.Source = new BitmapImage(new Uri(pic, UriKind.Absolute));

                FlipViewItem flip = new FlipViewItem();
                flip.Content = img;

                FlipViewAuthor.Items.Add(flip);
            }


            // Ajout d'un dernier FlipViewItem -> information
            Image other_img = new Image();
            other_img.Source = new BitmapImage(new Uri("/Resources/Others/image_getmore.png", UriKind.Relative));
            FlipViewItem other_flip = new FlipViewItem();
            other_flip.Content = other_img;
            other_flip.Tap += GetMeMorePicture_Tap;
            FlipViewAuthor.Items.Add(other_flip);

            Anime_FlipViewAuthor_In.Begin();

            // Application Bar
            FlipViewAppBar();
        }

        /// <summary>
        /// Initialise l'application bar avec de nouveaux éléments
        /// permettant d'enregistrer l'image, ou de fermer la vue d'images
        /// </summary>
        private void FlipViewAppBar()
        {
            // Application Bar
            ApplicationBar.Opacity = 0;
            ApplicationBar.Buttons.Clear();
            ApplicationBar.MenuItems.Clear();

            ApplicationBarMenuItem menuItemSave = new ApplicationBarMenuItem()
            {
                Text = "enregistrer l'image",
                IsEnabled = true,
            };
            menuItemSave.Click += menuItemSave_Click;

            ApplicationBarMenuItem menuItemClose = new ApplicationBarMenuItem()
            {
                Text = "Fermer",
                IsEnabled = true,
            };
            menuItemClose.Click += menuItemClose_Click;

            ApplicationBar.MenuItems.Add(menuItemSave);
            ApplicationBar.MenuItems.Add(menuItemClose);

            // SysTray et Pivot
            SystemTray.IsVisible = false;
            MyPivot.IsEnabled = false;
        }

        // Enregistre et sauvegarde d'un screenshot dans la bibliothèque d'images
        private void menuItemSave_Click(object sender, EventArgs e)
        {
            if(FlipViewAuthor.Visibility == System.Windows.Visibility.Visible)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            FlipViewItem flip = FlipViewAuthor.SelectedItem as FlipViewItem;
                            if (flip == null) return;

                            Image img = flip.Content as Image;

                            MediaLibrary mediaLibrary = new MediaLibrary();
                            BitmapImage bmp = (BitmapImage)img.Source;
                            Byte[] bytes = ConvertToBytes(bmp);
                            mediaLibrary.SavePicture(_authorName + DateTime.Now, bytes);
                        }
                        catch(Exception except)
                        {
                            MessageBox.Show(except.Message);
                        }
                    });
            }
        }

        /// <summary>
        /// Convertit une image en données binaires
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        public static byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            byte[] data;
            // Get an Image Stream
            using (MemoryStream ms = new MemoryStream())
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


        /// <summary>
        /// Lorsque l'utilisateur tapote sur l'item "fermer" de l'appbar
        /// Ferme la FlipView d'images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItemClose_Click(object sender, EventArgs e)
        {
            //Ferme le FlipView d'image grâce à l'item du menu de l'appbar
            if (FlipViewAuthor.Visibility == System.Windows.Visibility.Visible)
                HideFlipView();
        }

        private void HideFlipView()
        {
            Anime_FlipViewAuthor_Out.Begin();
            //FlipViewAuthor.Visibility = System.Windows.Visibility.Collapsed;

            SystemTray.IsVisible = true;

            // Réinitialisation de l'ApplicationBar
            ApplicationBar.MenuItems.Clear();
            ApplicationBar.Buttons.Clear();
            InitializeAppBar();

            MyPivot.IsEnabled = true;
        }

        private void FlipViewAuthor_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (FlipViewAuthor.Visibility == System.Windows.Visibility.Visible)
                HideFlipView();
        }

        private void LongListAuthorWorks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Work work = sender as Work;

        }

        private void StyleAuthor_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (TopBarStack2.Visibility == System.Windows.Visibility.Collapsed)
            {
                TopBarIn.Begin();
            }
            else
            {
                TopBarOut.BeginTime = TimeSpan.FromSeconds(0);
                TopBarOut.Begin();
            }
        }

        

        // FIN IMAGE AUTEUR
    }
}