using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace Citations365.Pages
{
    public partial class HelpPage : PhoneApplicationPage
    {
        int myTuto = 0;

        public HelpPage()
        {   
            InitializeComponent();
            DetermineColor();
            AnimeUIElements(myTuto);
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);
        //}

        private void AnimeUIElements(int integer)
        {
            switch (integer)
            {
                case 0:
                    Grid1SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid1SB.Begin();
                    Grid1SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 1:
                    Grid2SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid2SB.Begin();
                    Grid2SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 2:
                    Grid3SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid3SB.Begin();
                    Grid3SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 3:
                    Grid4SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid4SB.Begin();
                    Grid4SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 4:
                    Grid5SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid5SB.Begin();
                    Grid5SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 5:
                    if (Arrow.Visibility == System.Windows.Visibility.Collapsed)
                    {
                        Arrow.Visibility = System.Windows.Visibility.Visible;
                    }
                    Grid6SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid6SB.Begin();
                    Grid6SB.Completed += delegate
                    {
                        ArrowSB.Begin();
                    };
                    break;
                case 6:
                    Grid7SB.BeginTime = TimeSpan.FromSeconds(1);
                    Grid7SB.Begin();
                    Arrow.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }

        // Remplace certaines images pour le thème avec un fond blanc
        void DetermineColor()
        {
            Visibility whiteVisibility = (Visibility)Resources["PhoneLightThemeVisibility"];
            if(whiteVisibility == System.Windows.Visibility.Visible)
            {
                Img2.Source = new BitmapImage(new Uri("/Resources/Icons/ShareDark.png", UriKind.Relative));
                Img4.Source = new BitmapImage(new Uri("/Resources/Icons/SearchDark.png", UriKind.Relative));
                Img6.Source = new BitmapImage(new Uri("/Resources/Icons/Play-OnceDark.png", UriKind.Relative));
                Img7.Source = new BitmapImage(new Uri("/Resources/Icons/Light-BulbDark.png", UriKind.Relative));
            }
        }

        // Méthode pour retourner à l'accueil de l'application quand l'utilisateur a fini le tutoriel.
        // S'active après le tapotement du bouton correspondant.
        private void GoHome_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }


        // S'active quand l'utilisateur fait un Flick sur l'écran
        private void OnFlick(object sender, FlickGestureEventArgs e)
        {
            // Détermine l'orientation du mouvement
            if (e.Direction == System.Windows.Controls.Orientation.Vertical)
            {
                // Arrête l'animation en cours, s'il y a
                StopAnimations(myTuto);

                // Détermine la direction
                if (e.VerticalVelocity < 0)
                {
                    // Si le mouvement est de bas en haut
                    switch (myTuto)
                    {
                        case 0:
                            myTuto++;
                            MakeSwivels(Grid1, Grid2);
                            break;
                        case 1:
                            myTuto++;
                            MakeSwivels(Grid2, Grid3);
                            break;
                        case 2:
                            myTuto++;
                            MakeSwivels(Grid3, Grid4);
                            break;
                        case 3:
                            myTuto++;
                            MakeSwivels(Grid4, Grid5);
                            break;
                        case 4:
                            myTuto++;
                            MakeSwivels(Grid5, Grid6);
                            break;
                        case 5:
                            myTuto++;
                            MakeSwivels(Grid6, Grid7);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    // Sinon, si le mouvement est de haut en bas
                    switch (myTuto)
                    {
                        case 0:
                            break;
                        case 1:
                            myTuto--;
                            UnMakeSwivels(Grid2, Grid1);
                            break;
                        case 2:
                            myTuto--;
                            UnMakeSwivels(Grid3, Grid2);
                            break;
                        case 3:
                            myTuto--;
                            UnMakeSwivels(Grid4, Grid3);
                            break;
                        case 4:
                            myTuto--;
                            UnMakeSwivels(Grid5, Grid4);
                            break;
                        case 5:
                            myTuto--;
                            UnMakeSwivels(Grid6, Grid5);
                            break;
                        case 6:
                            myTuto--;
                            UnMakeSwivels(Grid7, Grid6);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // On masque la grille 1 et on affiche la grille 2
        private void MakeSwivels(Grid grid1, Grid grid2)
        {
            // Réduit la flèche
            if (Arrow.Height != 27)
            {
                ArrowHideSB.Begin();
            }
            // On masque la grille 1
            grid1.Visibility = System.Windows.Visibility.Collapsed;
            ReMakeOpacity(grid1);

            // On rend visible la grille 2 qu'on va afficher
            grid2.Visibility = System.Windows.Visibility.Visible;

            //TRANSITION DE LA GRILLE 2
            SwivelTransition sTransition = new SwivelTransition();
            sTransition.Mode = SwivelTransitionMode.BackwardIn;
            ITransition transition = sTransition.GetTransition(grid2);
            transition.Begin();
            transition.Completed += delegate
            {
            };


            //TRANSITION DE LA GRILLE 1
            //SwivelTransition s1Transition = new SwivelTransition();
            //s1Transition.Mode = SwivelTransitionMode.ForwardOut;
            //ITransition transition1 = s1Transition.GetTransition(grid1);
            //transition1.Begin();
            //transition1.Completed += delegate
            //{
            //    grid1.Visibility = System.Windows.Visibility.Collapsed;
            //    ReMakeOpacity(grid1);
            //    // On rend visible la grille qu'on va afficher
            //    grid2.Visibility = System.Windows.Visibility.Visible;
            //};

            // Animation des éléments (TextBlock, etc.) sur la page
            AnimeUIElements(myTuto);
        }


        // On masque la grille 2 (elem2) et on affiche la grille 1 (elem1)
        private void UnMakeSwivels(Grid grid2, Grid grid1)
        {
            //TRANSITION DE LA GRILLE 1
            SwivelTransition s1Transition = new SwivelTransition();
            s1Transition.Mode = SwivelTransitionMode.BackwardOut;
            ITransition transition1 = s1Transition.GetTransition(grid2);
            transition1.Begin();
            transition1.Completed += delegate
            {
                grid2.Visibility = System.Windows.Visibility.Collapsed;
                ReMakeOpacity(grid2);
                // On rend visible la grille qu'on va afficher
                grid1.Visibility = System.Windows.Visibility.Visible;
            };


            
            //TRANSITION DE LA GRILLE 2
            //SwivelTransition sTransition = new SwivelTransition();
            //sTransition.Mode = SwivelTransitionMode.ForwardIn;
            //ITransition transition = sTransition.GetTransition(grid1);
            //transition.Begin();
            //transition.Completed += delegate
            //{
            //};

            // Animation des éléments (TextBlock, etc.) sur la page
            AnimeUIElements(myTuto);
        }

        // Réinitialise l'Opacité à 0 des enfants des Grilles (sauf le premier élément)
        private void ReMakeOpacity(Grid grid)
        {
            if ((grid != null) && (grid.Children.Count>1))
            {
                for (int i = 1; i < grid.Children.Count; i++)
                {
                    grid.Children[i].Opacity = 0;
                }
            }
        }

        // Arrête les animations en cours sur la grilles
        private void StopAnimations(int integer)
        {
            switch (integer)
            {
                case 0:
                    //Grid1SB.Stop();
                    //Grid2SB.Stop();
                    break;
                case 1:
                    ArrowHideSB.Stop();
                    ArrowSB.Stop();

                    Grid1SB.Stop();
                    Grid2SB.Stop();
                    Grid3SB.Stop();
                    break;
                case 2:
                    ArrowHideSB.Stop();
                    ArrowSB.Stop();

                    Grid2SB.Stop();
                    Grid3SB.Stop();
                    Grid4SB.Stop();
                    break;
                case 3:
                    ArrowHideSB.Stop();
                    ArrowSB.Stop();

                    Grid3SB.Stop();
                    Grid4SB.Stop();
                    Grid5SB.Stop();
                    break;
                case 4:
                    ArrowHideSB.Stop();
                    ArrowSB.Stop();

                    Grid4SB.Stop();
                    Grid5SB.Stop();
                    Grid6SB.Stop();
                    break;
                case 5:
                    ArrowHideSB.Stop();
                    ArrowSB.Stop();

                    Grid5SB.Stop();
                    Grid6SB.Stop();
                    Grid7SB.Stop();
                    break;
                case 6:
                    //Grid6SB.Stop();
                    //Grid7SB.Stop();
                    break;
                default:
                    break;
            }
        }
    }
}