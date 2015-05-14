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

namespace Citations365.Pages
{
    public partial class AuthorsListPage : PhoneApplicationPage
    {
        // VARIABLES
        Author _author = new Author()
        {
            Name = "",
            Link = "",
        };

        bool _letMyFocus = false; // garde le focus sur la TBox
        bool _resultsIsActive = false; // masquer les résultats sur "back" (touche)
        bool _isLookingFor = false;

        public AuthorsListPage()
        {
            InitializeComponent();
            if (!App.ViewModel._AuthorListCharged)
            {
                LoadAuthorsList();
                App.ViewModel._AuthorListCharged = true;
            }
            else
            {
                LongListAuthors.ItemsSource = App.ViewModel.ListAuthorsSorted;

                // On masque les éléments de chargement
                ProgressPage.Visibility = System.Windows.Visibility.Collapsed;
                TBLoading.Visibility = System.Windows.Visibility.Collapsed;
                TextBoxNoAuthors.Visibility = System.Windows.Visibility.Collapsed;
                LongListAuthors.Visibility = System.Windows.Visibility.Visible;
            }
            LongListAuthorsResults.ItemsSource = App.ViewModel.CollectionAuthorsResults;
        }

        // Quand l'utilisateur appuie sur la touche Retour du téléphone
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // Si la page de résultats est affichée
            if (_resultsIsActive)
            {
                // On masque les résultats et on affiche la liste des auteurs
                if (TBSearch.Visibility == System.Windows.Visibility.Visible)
                {

                    TBSearchHideSB.Begin();
                    TBSearchHideSB.Completed += delegate
                    {
                        TBInfo.Visibility = System.Windows.Visibility.Collapsed;
                        LongListAuthors.ItemsSource = App.ViewModel.ListAuthorsSorted;
                        ApplicationBar.IsVisible = true;
                    };
                    e.Cancel = true;
                }
                _resultsIsActive = false;
            }
            // base.OnBackKeyPress(e);
        }

        // Charge la liste des auteurs
        public async void LoadAuthorsList()
        {
            //App.ViewModel._authorsListSaved = false;
            if (!App.ViewModel._IsAuthorsListSaved)
            {
                await App.ViewModel.GetAuthorsList(); // Récupère la liste des auteurs à partir du Web

                // Vérifie que la liste d'auteurs contient plus de 0 élément
                if (App.ViewModel.CollectionAuthors.Count > 0)
                {
                    // Ordonne la liste des auteurs
                    App.ViewModel.ListAuthorsSorted = AlphaKeyGroup<Author>.CreateGroups(App.ViewModel.CollectionAuthors,
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        (Author s) => { return s.Name; }, true);

                    // Set the source
                    LongListAuthors.ItemsSource = App.ViewModel.ListAuthorsSorted;
                    
                    // Sauvegarde de la collection d'auteurs dans l'IO
                    App.ViewModel.SaveAuthorsList();


                    // Sauvegarde du param dans l'IO
                    App.ViewModel._IsAuthorsListSaved = true;
                    App.ViewModel.SaveData();

                    // Visibilités
                    LongListAuthors.Visibility = System.Windows.Visibility.Visible;
                    TextBoxNoAuthors.Visibility = System.Windows.Visibility.Collapsed;
                }

                ProgressPage.Visibility = System.Windows.Visibility.Collapsed;
                TBLoading.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                // Récupère la collection d'auteurs dans l'iO
                await App.ViewModel.LoadAuthorsList();

                // Vérifie que la liste d'auteurs contient plus de 0 élément
                if (App.ViewModel.CollectionAuthors.Count > 0)
                {
                    App.ViewModel.ListAuthorsSorted = AlphaKeyGroup<Author>.CreateGroups(App.ViewModel.CollectionAuthors,
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        (Author s) => { return s.Name; }, true);

                    // Set the source
                    LongListAuthors.ItemsSource = App.ViewModel.ListAuthorsSorted;

                    // Visibilité
                    LongListAuthors.Visibility = System.Windows.Visibility.Visible;
                    TextBoxNoAuthors.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    // On met la variable à faux,
                    // car la liste des auteurs est indisponible
                    App.ViewModel._IsAuthorsListSaved = false;
                    App.ViewModel.SaveData();
                }

                ProgressPage.Visibility = System.Windows.Visibility.Collapsed;
                TBLoading.Visibility = System.Windows.Visibility.Collapsed;

            }
        }

        private void LongListAuthors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is Author)
            {
                _author = (Author)e.AddedItems[0];

                if ((_author.Name != "") && (_author.Link != ""))
                {
                    // Si on obtient un résultat, on navigue sur la page de l'auteur
                    PhoneApplicationService.Current.State["authorName"] = _author.Name.Replace("  ", "");
                    PhoneApplicationService.Current.State["authorLink"] = _author.Link;

                    NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                }
                
            }
        }


        private void LongListAuthorsResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is Author)
            {
                _author = (Author)e.AddedItems[0];

                if ((_author.Name != "") && (_author.Link != ""))
                {
                    // Si on obtient un résultat, on navigue sur la page de l'auteur
                    PhoneApplicationService.Current.State["authorName"] = _author.Name.Replace("  ", "");
                    PhoneApplicationService.Current.State["authorLink"] = _author.Link;

                    NavigationService.Navigate(new Uri("/Pages/AuthorPage.xaml", UriKind.Relative));
                }

            }
        }

        // Recherche d'auteurs dans la liste
        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            _resultsIsActive = true;
            ApplicationBar.IsVisible = false;
            LongListAuthorsResults.Opacity = 100;

            TBSearchShowSB.Begin();
            TBSearchShowSB.Completed += delegate
            {
                // LongListAuthors.Visibility = System.Windows.Visibility.Collapsed;
                if (!(App.ViewModel.CollectionAuthorsResults.Count > 0))
                    TBInfo.Visibility = System.Windows.Visibility.Visible;
                TBSearch.Focus();
            };
        }

        // Rafraichit la liste des auteurs
        private void ItemRefresh_Click(object sender, EventArgs e)
        {
            // On enregistre la valeur de la variable authorsListSaved dans l'IO
            // et on met à faux celle disant que la liste des auteurs a été chargée.
            App.ViewModel._IsAuthorsListSaved = false;
            App.ViewModel._AuthorListCharged = false;
            App.ViewModel.SaveData();

            // On vide les collections
            App.ViewModel.CollectionAuthors.Clear();
            App.ViewModel.ListAuthorsSorted.Clear();

            // On affiche les éléments de chargement
            if (ProgressPage.Visibility == System.Windows.Visibility.Collapsed)
            {
                ProgressPage.Visibility = System.Windows.Visibility.Visible;
                TBLoading.Visibility = System.Windows.Visibility.Visible;
            }
            // On masque le LongListSelector
            LongListAuthors.Visibility = System.Windows.Visibility.Collapsed;
            LongListAuthorsResults.Visibility = System.Windows.Visibility.Collapsed;

            // On affiche le TextBox
            TextBoxNoAuthors.Visibility = System.Windows.Visibility.Visible;

            // On lance à nouveau la méthode de récupération des auteurs
            LoadAuthorsList();
        }

        // Quand le TextBox obtiens le focus
        private void TBSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((TBSearch.Text != null) && (TBSearch.Text != ""))
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Visible;
        }


        // Quand le TextBox perd le focus
        private void TBSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_letMyFocus)
            {
                if (GridTBoxSearch.Visibility == System.Windows.Visibility.Visible)
                {
                    //GridTBoxSearch.Visibility = System.Windows.Visibility.Collapsed;
                    //GridTBoxSearch.Opacity = 1.0;


                    // Si on ne souhaitais faire aucune recherche, et
                    // Si aucune recherche est en cours
                    // On masque le LonglistSelector, la barre de progression et le Textbox
                }
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Collapsed;
                // ApplicationBar.IsVisible = true;
            }
            else
            {
                // Si on vient d'appuyer sur la croix pour vider le TBlock
                TBSearch.Focus();
                _letMyFocus = false;
            }
        }
        

        // Quand le texte du TextBox change
        private async void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.ViewModel.CollectionAuthorsResults.Clear();

            if ((!_isLookingFor)&&
                (TBSearch.Text != "") && (TBSearch != null))
            {
                _isLookingFor = true;
                // Affiche la croix dans le TextBox si celui-ci contient du text
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Visible;

                // Recherche de résultats et affichage immédiat
                await App.ViewModel.FindAuthorsByName(TBSearch.Text);
                //LongListAuthors.ItemsSource = App.ViewModel.ListAuthorsResults;
                //LongListAuthors.Visibility = System.Windows.Visibility.Visible;
                LongListAuthorsResults.Visibility = System.Windows.Visibility.Visible;
                
                // GridTBoxSearch.Visibility = System.Windows.Visibility.Collapsed;
                TBInfo.Visibility = System.Windows.Visibility.Collapsed;
                ApplicationBar.IsVisible = false;
                _isLookingFor = false;
            }
            else if ((TBSearch.Text == "") || (TBSearch == null))
            {
                // On la masque sinon
                ClearTBoxSearchButton.Visibility = System.Windows.Visibility.Collapsed;
                
            }
        
        }

        // Quand on appuie sur une touche du clavier
        private void TBSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Lance la recherche si on appuie sur la touche Entrer
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Focus();
                if (App.ViewModel._Offline)
                {
                    MessageBoxResult message = MessageBox.Show("Vous n'êtes pas connecté(e) à Internet");
                }
            }
        }

        // Quand on appuie sur la loupe de recherche
        //private void SearchButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{

        //}

        // Quand on appuie sur l'icone Wrong.png (la croix) pour vider le TextBox
        private void ClearTBoxSearch_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            TBSearch.Text = "";
            _letMyFocus = true;
        }
    }
}