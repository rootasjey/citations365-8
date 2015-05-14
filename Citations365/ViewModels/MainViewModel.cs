using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Citations365.Resources;
using System.Net;
using HtmlAgilityPack;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Threading.Tasks;
using Microsoft.Phone.Scheduler;
using System.Threading;
using System.Data.Services.Client;
using Bing;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Resources;

namespace Citations365.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Variables
        #region variables
        /// <summary>
        /// URL pour récupérer la citation du jour
        /// </summary>
        public string _LinkDay = "http://www.evene.fr/citations/citation-jour.php?page=";

        /// <summary>
        /// URL pour récupérer les citations précédentes
        /// </summary>
        public string _LinkBack = "http://www.evene.fr/citations/citation-jour.php?page=";

        /// <summary>
        /// URL permettant de récupérer à coup sûr la citation du jour (les autres flux sont souvent expirés)
        /// </summary>
        public string _LinkTodayQuote = "http://evene.lefigaro.fr/citations";


        /// <summary>
        /// Premier démarrage de l'application?
        /// </summary>
        //public bool _FirstLaunch = true;

        /// <summary>
        /// L'application arrive-t-elle à accéder à internet?
        /// </summary>
        public bool _Offline = false;

        /// <summary>
        /// Est-ce que le Text-To-Speech est activé?
        /// </summary>
        public bool _TTSIsActivated = false;

        /// <summary>
        /// Est-ce que la liste des auteurs est sauvegardée dans l'IO?
        /// </summary>
        public bool _IsAuthorsListSaved = false;

        /// <summary>
        /// Est-ce que la liste des auteurs a déjà été chargée?
        /// (depuis le démarrage de l'app)
        /// </summary>
        public bool _AuthorListCharged = false;

        /// <summary>
        /// Est-ce que le fond d'écran de l'auteur est activé?
        /// </summary>
        public bool _AuthorBackgroundActivated = true;

        /// <summary>
        /// Indique le type de fond d'écran sélectionné par l'utilisateur
        /// avec valeur par défaut
        /// </summary>
        public string _ApplicationBackgroundType = "Blue";

        /// <summary>
        /// L'utilisateur a changé le fond de l'app?
        /// Actif au démarrage de l'app 
        /// pour vérifier le dernier choix de l'utilisateur
        /// </summary>
        public bool _BackgroundChanged = true;

        /// <summary>
        /// Doit-on supprimer les entrées enregistrées dans la pile de navigattion?
        /// </summary>
        public bool _RemoveAllEntriesFromBackStack = false;

        /// <summary>
        /// Variable contenant la biographie de l'auteur
        /// (pour réutilisation dans la page AuthorPage.xaml)
        /// </summary>
        public string _AuthorBio = "";
        
        /// <summary>
        /// Variable contenant la citation de l'auteur
        /// (pour réutilisation dans la page AuthorPage.xaml)
        /// </summary>
        public string _AuthorQuote = "";

        /// <summary>
        /// Lien de la page de l'auteur que l'utilisateur recherche
        /// </summary>
        //public string _AuthorLink = "";

        /// <summary>
        /// Mots clés saisis par l'utilisateur pour la recherche de citations
        /// </summary>
        public string _QueryTermFromAuthor = null;

        /// <summary>
        /// URI de l'actuel fond de l'application
        /// avec valeur par défaut
        /// </summary>
        public string _CurrentBackgroundLink = "/Resources/Backgrounds/bg_blue.jpg";
        
        

        /// <summary>
        /// Mutex
        // pour résoudre les accès concurrents entre processus
        /// </summary>
        public static Mutex _mutex = new Mutex(false, "Citations365SettingsMutex");

        /// <summary>
        /// Clé API Bing Search
        /// </summary>
        private const string AccountKey = "pCzCBMoEJtZ76ni+ge9sbAYr5PXDfe2ksLPW63wxcVs= ";

        public EventHandler SearchCompletedEvent;

        #endregion variables

        public MainViewModel()
        {
            this.CollectionQuotes = new ObservableCollection<Quote>();
            this.CollectionQuotesAuthor = new ObservableCollection<Quote>();
            this.CollectionAuthorPictures = new ObservableCollection<string>();
            this.CollectionQuotesSearch = new ObservableCollection<Quote>();
            this.CollectionAuthors = new ObservableCollection<Author>();
            this.CollectionAuthorsResults = new ObservableCollection<Author>();

            this.CollectionAuthorWorks = new ObservableCollection<Work>();

            this.ListAuthorsSorted = new List<AlphaKeyGroup<Author>>();
            this.ListPicturesFlickr = new List<string>();

            
        }

        /// <summary>
        /// Collection pour les citations dans la vue principale
        /// Collection pour les citations de l'auteur du jour
        /// Collection pour les URL des images de l'auteur
        /// </summary>
        public ObservableCollection<Quote> CollectionQuotes { get; private set; }
        public ObservableCollection<Quote> CollectionQuotesAuthor { get; private set; }
        public ObservableCollection<String> CollectionAuthorPictures { get; private set; }
        public ObservableCollection<Quote> CollectionQuotesSearch { get; private set; }
        public ObservableCollection<Author> CollectionAuthors { get; private set; }
        public ObservableCollection<Author> CollectionAuthorsResults { get; set; }


        public ObservableCollection<Work> CollectionAuthorWorks { get; private set; }

        // Liste des auteurs triée par A-Z
        // List de résultats des auteurs recherchés par mot clé
        public List<AlphaKeyGroup<Author>> ListAuthorsSorted { get; set; }


        /// <summary>
        /// Liste d'URL d'images du site Flickr (section explore)
        /// </summary>
        public List<String> ListPicturesFlickr { get; set; }


        /// <summary>
        /// Exemple de propriété qui retourne une chaîne localisée
        /// </summary>
        public string LocalizedSampleProperty
        {
            get
            {
                return AppResources.SampleProperty;
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Crée et ajoute quelques objets ItemViewModel dans la collection Items.
        /// </summary>
        public void LoadData()
        {
            LoadDataFromIO(); // Paramètres
            this.IsDataLoaded = true;
        }


        // Récupère les citations à partir de l'adresse web
        public async Task<bool> LoadQuotes(string webURL, int pageNumber, bool getFirstQuote)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (pageNumber == 0)
                {
                    if (getFirstQuote) webURL = _LinkTodayQuote;
                    else webURL = webURL.Substring(0, 47);
                    
                    // si num = 0, http://www.evene.fr/citations/citation-jour.php?page= devient http://www.evene.fr/citations/citation-jour.php
                }
                else
                {
                    webURL = webURL + pageNumber;
                    // sinon num > 0, http://www.evene.fr/citations/citation-jour.php?page= devient http://www.evene.fr/citations/citation-jour.php?page=num
                }

                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = null;

                try
                {
                    response = await httpClient.GetAsync(webURL);
                    response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                    string responseBodyAsText = await response.Content.ReadAsStringAsync();

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseBodyAsText);

                    // Récupération des données
                    // contenus
                    string[] contents = doc.DocumentNode.Descendants("article").Select(y => y.InnerHtml).ToArray();
                    // dates
                    string[] dates = doc.DocumentNode.Descendants("span").Where(x => (string)x.GetAttributeValue("class", "") == "date").Select(y => y.InnerText).ToArray();


                    Regex content_regex = new Regex("<div class=\"figsco__quote__text\">" + "((.|\n)*?)" + "</a></div>");
                    Regex author_regex = new Regex("<div class=\"figsco__fake__col-9\">" + "((.|\n)*?)" + "</a>");
                    Regex authorLink_regex = new Regex("/celebre/biographie/" + "((.|\n)*?)" + ".php");
                    Regex reference_regex = new Regex("</a>" + "((.|\n)*?)" + "/" + "((.|\n)*?)" + "</br>");
                    Regex quoteLink_regex = new Regex("/citation/" + "((.|\n)*?)" + ".php");
                    //int i = 0;

                    foreach (string block in contents)
                    {
                        MatchCollection content_match = content_regex.Matches(block);
                        MatchCollection author_match = author_regex.Matches(block);
                        MatchCollection authorLink_match = authorLink_regex.Matches(block);
                        MatchCollection reference_match = reference_regex.Matches(block);
                        MatchCollection quoteLink_match = quoteLink_regex.Matches(block);

                        String quote_content = null;
                        if (content_match.Count > 0)
                        {
                            quote_content = DeleteHTMLTags(content_match[0].ToString());
                        }
                        else continue;

                        Quote quote = new Quote();
                        quote.content = quote_content;

                        // Récupère la date
                        //if (i < dates.Length) {
                        //    quote.date = DeleteHTMLTags(dates[i]);
                        //}
                        //else quote.date = "";
                        quote.date = "";

                        // Récupère le contenu de la citation
                        

                        // Récupère l'auteur
                        if (author_match.Count > 0) {
                            quote.author = DeleteHTMLTags(author_match[0].ToString());

                            if (quote.author.Contains("Vos avis"))
                                quote.author = "Anonyme";
                        }

                        // Récupère l'url de l'auteur
                        if (authorLink_match.Count > 0) {
                            quote.authorlink = "http://www.evene.fr" + authorLink_match[0].ToString();
                        }

                        // Récupère la référence
                        if (reference_match.Count > 0)  {
                            quote.reference = DeleteHTMLTags(reference_match[0].ToString());
                        }

                        // Récupère le lien de la citation détaillée
                        if (quoteLink_match.Count > 0) {
                            quote.link = quoteLink_match[0].ToString();
                        }
                        
                        CollectionQuotes.Add(quote);
                        
                        //i++;
                        //if (i > 13) break;

                        // Arrête à la première itération si on est au premier lancement
                        // Car on ne veut récupérer que la première citation
                        if (getFirstQuote) return true;
                    }



                    if (pageNumber == 0)
                    {
                        // Sauvegarde de la CollectionQuotes (des citations que sur la première page)
                        // çàd si num = 0  - et si la collection n'est pas vide
                        if (CollectionQuotes.Count > 0)
                        {
                            SaveCollectionToIO();

                            // Sauvegarde de l'heure à laquelle
                            // la récupération a été faite
                            SaveLastTimeQuoteRefresh();
                        }
                        else
                        {
                            _Offline = true;
                            LoadCollectionFromIO();
                        }
                        // sinon la CollectionQuotes est vide, on essaie alors de charger les citations depuis l'IO
                    }

                    // Tout s'est bien passé
                    return true;
                }
                catch (HttpRequestException hre)
                {
                    // Une erreur est survenue
                    return false;

                    //LoadQuotes(_LinkDay, 0);
                    //_Offline = true;
                    //System.Windows.MessageBox.Show("L'application n'a pas pu se connecter à Internet. Message:{0}" + hre.Message);
                }
            }
            else
            {
                _Offline = true;
                await LoadCollectionFromIO();
                return true;
            }
        }


        public async Task<List<string>> GetAuthorInfos(string link)
        {
            // si lien n'a pas le http devant
            if (! (link.StartsWith("http://evene.lefigaro.fr"))
                && !(link.StartsWith("http://www.evene.fr")))
            {
                link = "http://evene.lefigaro.fr" + link;
            }

            List<string> results = new List<string>();

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            
            try
            {
                response = await httpClient.GetAsync(link);
                response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                string[] bio = doc.DocumentNode.Descendants("div").Where(x => (string)x.GetAttributeValue("class", "") == "txt-block").Select(y => (string)y.InnerHtml).ToArray();
                //string[] pic = doc.DocumentNode.Descendants("img").Where(x => (string)x.GetAttributeValue("itemprop", "") == "image").Select(y => (string)y.GetAttributeValue("src", "")).ToArray();
                string[] essentials = doc.DocumentNode.Descendants("div").Where(x => (string)x.GetAttributeValue("class", "") == "essentiel-tab-content").Select(y => (string)y.InnerHtml).ToArray();

                string start = "<div class=\"txt\">";
                string end = "</div>";
                Regex quote_regex = new Regex(start + "(.*?)" + end);
                MatchCollection quote_matches = quote_regex.Matches(bio[0]);

                //if (quote_matches.Count > 0)
                //    _AuthorQuote = quote_matches[0].ToString();

                //_AuthorBio = Regex.Replace(bio[0], "<a href=\"/error-report(.*?)</a>", (m) => "");
                //_AuthorBio = Regex.Replace(_AuthorBio, start + "(.*?)" + end, (m) => "");
                //_AuthorBio = Regex.Replace(_AuthorBio, "<h4>(.*?)</h4>", (m) => "");

                string biography = Regex.Replace(bio[0], "<a href=\"/error-report(.*?)</a>", (m) => "");
                biography = Regex.Replace(biography, start + "(.*?)" + end, (m) => "");
                biography = Regex.Replace(biography, "<h4>(.*?)</h4>", (m) => "");

                if (bio.Length > 0)
                {
                    results.Add(biography);
                }
                else{
                    results.Add("");
                }

                if (quote_matches.Count > 0){
                    results.Add(quote_matches[0].ToString());
                }
                else{
                    results.Add("");
                }
                
                

                if(essentials.Length > 0) {
                    Regex genre_regex = new Regex("Genre : " + "((.|\n)*?)" + "</li>");
                    Regex birth_regex = new Regex("date de naissance :" + "((.|\n)*?)" + "</li>");
                    Regex death_regex = new Regex("date de décès :" + "((.|\n)*?)" + "</li>");

                    MatchCollection genre_match = genre_regex.Matches(essentials[0]);
                    MatchCollection birth_match = birth_regex.Matches(essentials[0]);
                    MatchCollection death_match = death_regex.Matches(essentials[0]);

                    if (genre_match.Count > 0)
                    {
                        results.Add(DeleteHTMLTags(genre_match[0].ToString()));
                    }
                    else { results.Add(""); }

                    if (birth_match.Count > 0)
                    {
                        results.Add(DeleteHTMLTags(birth_match[0].ToString()));
                        results[results.Count - 1] = results[results.Count - 1].Replace(":", ": ");
                        results[results.Count - 1] = results[results.Count - 1].Substring(0, 1).ToUpper()
                                                        + results[results.Count -1].Substring(1);
                    }
                    else { results.Add(""); }

                    if (death_match.Count > 0)
                    {
                        results.Add(DeleteHTMLTags(death_match[0].ToString()));
                        results[results.Count - 1] = results[results.Count - 1].Replace(":", ": ");
                        results[results.Count - 1] = results[results.Count - 1].Substring(0, 1).ToUpper()
                                                        + results[results.Count - 1].Substring(1);
                    }
                    else { results.Add(""); }
                }

                return results;

            }
            catch (HttpRequestException hre)
            {
                System.Windows.MessageBox.Show("Les informations sur l'auteur n'a pas pu être récupérées! Message:{0}" + hre.Message);

                return results;
            }

        }

        /// <summary>
        /// Récupère les citations d'un auteur
        /// </summary>
        /// <param name="link">URL de la page</param>
        /// <param name="author">Nom de l'auteur</param>
        /// <returns></returns>
        public async Task GetAuthorQuotes(string link, string author)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(link + "?citations");
                response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                string[] quotes = doc.DocumentNode.Descendants("h3").Select(x => (string)x.InnerText).ToArray();

                //CollectionQuotesAuthor.Clear();

                // Ajoute les Quotes à la collection CollectionQuotes
                if (quotes.Length > 0)
                {
                    int i = 0;
                    foreach (string quote in quotes)
                    {
                        // Vérifie qu'on récupère bien une citation
                        if (quote.StartsWith("&laquo;&nbsp;"))
                        {
                            Quote q = new Quote();
                            q.content = DeleteHTMLTags(quote);
                            q.author = author;

                            CollectionQuotesAuthor.Add(q);
                        }
                        i++;
                        if (i > quotes.Length - 3) break;
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                //System.Windows.MessageBox.Show("La liste des citations n'a pas pu être récupérée. Message:{0}" + hre.Message);
            }
        }

        public async Task<bool> GetAuthorWork(string link, string author)
        {
            CollectionAuthorWorks.Clear();
            // ------------------------

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(link + "?oeuvre");
                response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                string[] blocks = doc.DocumentNode.Descendants("div").Where(x => (string)x.GetAttributeValue("class", "") == "block-cdc").Select(y => (string)y.InnerHtml).ToArray();

                if (blocks.Length > 0)
                {
                    //List<string> results = new List<string>();

                    Regex title_regex = new Regex("<h3>" + "((.|\n)*?)" + "</h3>");
                    Regex link_regex = new Regex("/livres/livre/" + "((.|\n)*?)" + ".php");
                    Regex image_regex = new Regex("http://" + "((.|\n)*?)" + "(.gif|.jpg|.jpeg|.png|.bmp)");
                    Regex category_regex = new Regex("<h4>" + "((.|\n)*?)" + "</h4>");
                    //Regex categoryLink_regex = new Regex("/livres/categorie/" + "((.|\n)*?)" + ".php");

                    Regex editor_regex = new Regex("Editeur :" + "((.|\n)*?)" + "</b>");
                    Regex publication_regex = new Regex("Parution :" + "((.|\n)*?)" + "</b>");
                    Regex resume_regex = new Regex("<p>" + "((.|\n)*?)" + "</p>");
                    Regex authors_regex = new Regex("<h4 class=\"de\">" + "((.|\n)*?)" + "</h4>");
                    Regex authorsLink_regex = new Regex("/celebre/biographie/" + "((.|\n)*?)" + ".php");


                    foreach (string block in blocks)
                    {
                        MatchCollection title_match = title_regex.Matches(block);
                        MatchCollection link_match = link_regex.Matches(block);
                        MatchCollection image_match = image_regex.Matches(block);
                        MatchCollection category_match = category_regex.Matches(block);
                        //MatchCollection categoryLink_match = categoryLink_regex.Matches(block);

                        MatchCollection editor_match = editor_regex.Matches(block);
                        MatchCollection publication_match = publication_regex.Matches(block);
                        MatchCollection resume_match = resume_regex.Matches(block);
                        MatchCollection authors_match = authors_regex.Matches(block);


                        if (title_match.Count > 0)
                        {
                            if (title_match[0].ToString().StartsWith("<h3><a href=\"/culture/agenda"))
                            {
                                continue;
                            }
                        }

                        Work work = new Work();

                        // Récuppère le titre de l'oeuvre
                        if (title_match.Count > 0)
                        {
                            work.title = DeleteHTMLTags(title_match[0].ToString());
                        }
                        else work.title = "Sans titre";

                        // Récupère le lien de l'oeuvre
                        if (link_match.Count > 0)
                        {
                            work.link = DeleteHTMLTags(link_match[0].ToString());
                        }
                        // Récupère l'illustration de l'oeuvre
                        if (image_match.Count > 0)
                        {
                            work.imageUrl = image_match[0].ToString();
                            //ImagePortrait.ImageSource = new BitmapImage(imageLink);
                            //work.imageUrl = new BitmapImage();
                            //work.imageUrl.UriSource = new Uri(image_match[0].ToString(), UriKind.Absolute);
                            //work.imageUrl.UriSource = new Uri(DeleteHTMLTags(image_match[0].ToString()));
                        }

                        // Récupère le genre de l'oeuvre
                        if (category_match.Count > 0)
                        {
                            work.category = DeleteHTMLTags(category_match[0].ToString());
                        }
                        else work.category = "Catégorie inconnue";

                        // Récupère l'éditeur de l'oeuvre
                        if (editor_match.Count > 0)
                        {
                            work.editor = DeleteHTMLTags(editor_match[0].ToString());
                        }
                        else work.editor = "Aucun éditeur";

                        // Récupère la date de publication de l'oeuvre
                        if (publication_match.Count > 0)
                        {
                            work.publication = DeleteHTMLTags(publication_match[0].ToString());
                        }
                        else work.publication = "Date de publication inconnue";

                        // Récupère le résumé de l'oeuvre
                        if (resume_match.Count > 0)
                        {
                            work.resume = DeleteHTMLTags(resume_match[0].ToString());
                        }
                        else work.resume = "Sans résumé";

                        // Récupère la liste des auteurs de l'oeuvre
                        if (authors_match.Count > 0)
                        {
                            work.authors = DeleteHTMLTags(authors_match[0].ToString());
                        }
                        else work.authors = "Auteur inconnu";

                        if (authors_match.Count > 0)
                        {
                            MatchCollection authorsLink_match = authorsLink_regex.Matches(authors_match[0].ToString());

                            if (authorsLink_match.Count > 0)
                            {
                                work.auhorsLink = new List<string>();

                                foreach (var url in authorsLink_match)
                                {
                                    work.auhorsLink.Add(url.ToString());
                                }
                            }
                        }

                        CollectionAuthorWorks.Add(work);
                    }
                    return true; // on a récupéré des oeuvres
                }
                else return false; // on n'a rien obtenu
                
            }
            catch (HttpRequestException hre)
            {
                return false;
            }
        }


        public async Task GetAuthorsList()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = null;
                try
                {
                    response = await httpClient.GetAsync("http://www.evene.fr/citations/dictionnaire-citations-auteurs.php");
                    response.EnsureSuccessStatusCode();

                    string responseBodyAsText = await response.Content.ReadAsStringAsync();

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(responseBodyAsText);

                    string[] authorsNames = doc.DocumentNode.Descendants("a").Where(x => (string)x.GetAttributeValue("class", "") == "N11 txtC30").Select(y => (string)y.InnerText).ToArray();
                    string[] authorsLinks = doc.DocumentNode.Descendants("a").Where(x => (string)x.GetAttributeValue("class", "") == "N11 txtC30").Select(y => (string)y.GetAttributeValue("href", "")).ToArray();

                    for (int i = 0; i < authorsNames.Length; i++)
                    {
                        Author author = new Author()
                        {
                            Name = authorsNames[i],
                            Link = authorsLinks[i],
                        };
                        CollectionAuthors.Add(author);
                    }
                }
                catch (HttpRequestException hre)
                {
                    // System.Windows.MessageBox.Show("La liste des auteurs n'a pas pu être récupérée. Message:{0}" + hre.Message);
                }
            }
            else
            {
                _Offline = true;
            }
        }

        public async Task FindAuthorsByName(string name)
        {
            string firstLetter = name.Substring(0, 1).ToUpper();

            //if (name.Length > 1)
            //{
            //    name = firstLetter + name.Substring(1, name.Length - 1);
            //}
            //else if (name.Length == 1)
            //{
            //    name = firstLetter;
            //}

            Regex regex = new Regex(@"([A-Za-z]+)");
            Match match = regex.Match(firstLetter);


            // SWITCH (comparaison de char, regarde la première lettre pour savoir où chercher)
            #region switch
            if ((match.Success) && 
                (firstLetter != null) && (firstLetter != "") && (firstLetter!= " "))
            {
                switch (firstLetter)
                {
                    case "A":
                        {
                            for (int i = 0; i < ListAuthorsSorted[1].Count; i++)
                            {
                                if (ListAuthorsSorted[1].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[1].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "B":
                        {
                            for (int i = 0; i < ListAuthorsSorted[2].Count; i++)
                            {
                                if (ListAuthorsSorted[2].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[2].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "C":
                        {
                            for (int i = 0; i < ListAuthorsSorted[3].Count; i++)
                            {
                                if (ListAuthorsSorted[3].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[3].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "D":
                        {
                            for (int i = 0; i < ListAuthorsSorted[4].Count; i++)
                            {
                                if (ListAuthorsSorted[4].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[4].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "E":
                        {
                            for (int i = 0; i < ListAuthorsSorted[5].Count; i++)
                            {
                                if (ListAuthorsSorted[5].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[5].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "F":
                        {
                            for (int i = 0; i < ListAuthorsSorted[6].Count; i++)
                            {
                                if (ListAuthorsSorted[6].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[6].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "G":
                        {
                            for (int i = 0; i < ListAuthorsSorted[7].Count; i++)
                            {
                                if (ListAuthorsSorted[7].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[7].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "H":
                        {
                            for (int i = 0; i < ListAuthorsSorted[8].Count; i++)
                            {
                                if (ListAuthorsSorted[8].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[8].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "I":
                        {
                            for (int i = 0; i < ListAuthorsSorted[9].Count; i++)
                            {
                                if (ListAuthorsSorted[9].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[9].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "J":
                        {
                            for (int i = 0; i < ListAuthorsSorted[10].Count; i++)
                            {
                                if (ListAuthorsSorted[10].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[10].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "K":
                        {
                            for (int i = 0; i < ListAuthorsSorted[11].Count; i++)
                            {
                                if (ListAuthorsSorted[11].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[11].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "L":
                        {
                            for (int i = 0; i < ListAuthorsSorted[12].Count; i++)
                            {
                                if (ListAuthorsSorted[12].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[12].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "M":
                        {
                            for (int i = 0; i < ListAuthorsSorted[13].Count; i++)
                            {
                                if (ListAuthorsSorted[13].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[13].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "N":
                        {
                            for (int i = 0; i < ListAuthorsSorted[14].Count; i++)
                            {
                                if (ListAuthorsSorted[14].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[14].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "O":
                        {
                            for (int i = 0; i < ListAuthorsSorted[15].Count; i++)
                            {
                                if (ListAuthorsSorted[15].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[15].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "P":
                        {
                            for (int i = 0; i < ListAuthorsSorted[16].Count; i++)
                            {
                                if (ListAuthorsSorted[16].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[16].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "Q":
                        {
                            for (int i = 0; i < ListAuthorsSorted[17].Count; i++)
                            {
                                if (ListAuthorsSorted[17].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[17].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "R":
                        {
                            for (int i = 0; i < ListAuthorsSorted[18].Count; i++)
                            {
                                if (ListAuthorsSorted[18].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[18].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "S":
                        {
                            for (int i = 0; i < ListAuthorsSorted[19].Count; i++)
                            {
                                if (ListAuthorsSorted[19].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[19].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "T":
                        {
                            for (int i = 0; i < ListAuthorsSorted[20].Count; i++)
                            {
                                if (ListAuthorsSorted[20].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[20].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "U":
                        {
                            for (int i = 0; i < ListAuthorsSorted[21].Count; i++)
                            {
                                if (ListAuthorsSorted[21].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[21].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "V":
                        {
                            for (int i = 0; i < ListAuthorsSorted[22].Count; i++)
                            {
                                if (ListAuthorsSorted[22].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[22].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "W":
                        {
                            for (int i = 0; i < ListAuthorsSorted[23].Count; i++)
                            {
                                if (ListAuthorsSorted[23].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[23].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "X":
                        {
                            for (int i = 0; i < ListAuthorsSorted[24].Count; i++)
                            {
                                if (ListAuthorsSorted[24].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[24].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "Y":
                        {
                            for (int i = 0; i < ListAuthorsSorted[25].Count; i++)
                            {
                                if (ListAuthorsSorted[25].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[25].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    case "Z":
                        {
                            for (int i = 0; i < ListAuthorsSorted[26].Count; i++)
                            {
                                if (ListAuthorsSorted[26].ElementAt(i).Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Author aut = ListAuthorsSorted[26].ElementAt(i);
                                    CollectionAuthorsResults.Add(aut);
                                }
                            }
                        }
                        break;

                    default:
                        break;
                }
            #endregion switch
            }
        }

        public void GetAuthorPicture(string authorName)
        {
            // L'expression demandée
            string query = authorName;

            // Création d'un conteneur Bing
            string rootUrl = "https://api.datamarket.azure.com/Bing/Search";
            var bingContainer = new Bing.BingSearchContainer(new Uri(rootUrl));
            bingContainer.UseDefaultCredentials = false;

            // Le marché à utiliser (location)
            string market = "fr-FR";
            
            // Configuration du bingContainer pour utiliser mes identifiants
            bingContainer.Credentials = new NetworkCredential(AccountKey, AccountKey);

            // Construction de la demande, limitée à 3 résultats
            var imageQuery = bingContainer.Image(query, null, market, null, null, null, null);
            imageQuery = imageQuery.AddQueryOption("$top", 6);

            // Lance la demande
            var imageResults = imageQuery.BeginExecute(QueryCompleted, imageQuery);
        }

        private void QueryCompleted(IAsyncResult imageResults)
        {
            // Get the original query from the imageResults.
            DataServiceQuery<Bing.ImageResult> query =
                imageResults.AsyncState as DataServiceQuery<Bing.ImageResult>;

            if (CollectionAuthorPictures.Count > 0)
                CollectionAuthorPictures.Clear();

            foreach (var result in query.EndExecute(imageResults))
            {
                CollectionAuthorPictures.Add(result.MediaUrl);
            }
            NotifyRequesEndGetAuthorPictures();
        }


        public async Task<string> FindAuthorLink(string authorName)
        {
            string linkToFind = "";

            if (authorName.StartsWith("de "))
            {
                authorName = authorName.Replace("de ", "");
            }
            // On réinitialise la variable _linkToFind
            //_AuthorLink = "";

            // Supprime les accents et caractères spéciaux pour la recherche du lien plus tard
            authorName = authorName.Replace("  ", "").Replace(",","").Replace("\n", "").Replace(" ", "%20");
            authorName = authorName.Replace("à", "a").Replace("â", "a").Replace("é", "e").Replace("è", "e").Replace("ê", "e").Replace("î", "i");
            authorName = authorName.Replace("ì", "i").Replace("ô", "o").Replace("ò", "o").Replace("û", "u").Replace("ù", "u").Replace("ç", "c").Replace("ñ", "n");
            authorName = authorName.Replace("ä", "a").Replace("ë", "e").Replace("ï", "i").Replace("ö", "o").Replace("ü", "u");
            string link = "http://evene.lefigaro.fr/celebre/tout/" + authorName; // création du lien de la page


            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;

            try
            {
                response = await httpClient.GetAsync(link);
                response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                string responseBodyAsText = await response.Content.ReadAsStringAsync();


                //string document = e.Result;
                // Récupération des données
                string start = "/celebre/biographie/";
                string end = ".php";
                Regex regex = new Regex(start + "((.|\n)*?)" + end);
                MatchCollection matches = regex.Matches(responseBodyAsText);

                if (matches.Count > 0)
                {
                    authorName = authorName.Replace("%20", "-").ToLower();

                    foreach (var author in matches)
                    {
                        if (author.ToString().Contains(authorName))
                        {
                            linkToFind = author.ToString();
                            break;
                        }
                    }
                }

                return linkToFind;
            }
            catch (HttpRequestException hre)
            {
                //System.Windows.MessageBox.Show("L'exception RespCallback a été levée! Message:{0}" + hre.Message);
                return "";
            }
        }

        /// <summary>
        /// Méthode permettant de trouver des citations en rapport à un mot clé-
        /// Retourne vrai si on a obtenu de nouveaux résultats, faux sinon
        /// </summary>
        /// <param name="query">mot(s) clé(s)</param>
        /// <param name="number">page de résultats à récupérer</param>
        /// <param name="lastQuoteContent">dernière citation-permet de vérifier qu'on n'a pas atteint la fin de la recherche</param>
        /// <returns>Retourne vrai si on a obtenu de nouveaux résultats, faux sinon</returns>
        public async Task<bool> SearchQuote(string query, int number, string lastQuoteContent)
        {
            //Modification de la chaîne de char
            query = query.Replace(" ", "&20");
            query = query + "&p=";

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;

            try
            {
                response = await httpClient.GetAsync("http://www.evene.fr/citations/mot.php?mot=" + query + number);
                response.EnsureSuccessStatusCode(); // Throws exception if bad HTTP status code

                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                // Récupération des données
                // contenus
                string[] contents = doc.DocumentNode.Descendants("div").Where(x => (string)x.GetAttributeValue("class", "") == "txt").Select(y => y.InnerHtml).ToArray();
                // dates
                string[] dates = doc.DocumentNode.Descendants("span").Where(x => (string)x.GetAttributeValue("class", "") == "date").Select(y => y.InnerText).ToArray();


                // Si on n'a qu'un seul résultat,
                // alors on n'en a aucun
                // -> on a récupéré la citation du jour
                if (contents.Length < 2)
                {
                    return false;
                }
                string quoteToNotAdd = contents.Last();
                

                Regex content_regex = new Regex("<h3>" + "((.|\n)*?)" + "</h3>");
                Regex author_regex = new Regex("<h4>" + "((.|\n)*?)" + "</h4>");
                Regex authorLink_regex = new Regex("/celebre/biographie/" + "((.|\n)*?)" + ".php");
                Regex reference_regex = new Regex("<span class=\"author\">" + "((.|\n)*?)" + "</span>");
                Regex quoteLink_regex = new Regex("/citation/" + "((.|\n)*?)" + ".php");
                int i = 0;


                foreach (string block in contents)
                {
                    // Passe, si on tombe sur la citation du jour
                    if (quoteToNotAdd == block) continue;

                    MatchCollection content_match = content_regex.Matches(block);
                    MatchCollection author_match = author_regex.Matches(block);
                    MatchCollection authorLink_match = authorLink_regex.Matches(block);
                    MatchCollection reference_match = reference_regex.Matches(block);
                    MatchCollection quoteLink_match = quoteLink_regex.Matches(block);


                    // Saute l'itération si on n'a pas de contenu
                    if (!(content_match.Count > 0)) continue;

                    // Test au préalable qu'on n'a pas récupérer une catégorie (thème) de citations
                    if (content_match[0].ToString().Contains("/citations/theme/")) continue;

                    // On peut récupérer une autre catégorie quand on dépase le nombre de pages de résultat
                    // Dans ce cas, on arrête la recherche (et suppr. la derniere citation qui est celle du jour)
                    if (content_match[0].ToString().Contains("<a href=\"/livres/"))
                    {
                        // Retire le dernier élément
                        // qui est la citation du jour
                        CollectionQuotesSearch.RemoveAt(CollectionQuotesSearch.Count - 1);

                        // Arrête la recherche
                        return false;
                    }
                    

                    Quote quote = new Quote();

                    if (i < dates.Length)
                    {
                        quote.date = dates[i];
                    }
                    else quote.date = "";

                    // Récupère le contenu de la citation
                    quote.content = DeleteHTMLTags(content_match[0].ToString());

                    // Vérifie que la première citation trouvée est différente
                    // de la recherche précédente
                    if (lastQuoteContent == quote.content) return false;
                    

                    if (author_match.Count > 0)
                    {
                        quote.author = DeleteHTMLTags(author_match[0].ToString());
                    }
                    if (authorLink_match.Count > 0)
                    {
                        quote.authorlink = "http://www.evene.fr" + authorLink_match[0].ToString();
                    }
                    if (reference_match.Count > 0)
                    {
                        quote.reference = DeleteHTMLTags(reference_match[0].ToString());
                    }
                    if (quoteLink_match.Count > 0)
                        quote.link = quoteLink_match[0].ToString();

                    CollectionQuotesSearch.Add(quote);

                    i++;
                    if (i > 13) break;
                }

                return true;
            }
            catch (HttpRequestException hre)
            {
                //System.Windows.MessageBox.Show("L'exception RespCallback a été levée! Message:{0}" + hre.Message);
                return false;
            }
        }

        /// <summary>
        /// Récupère l'image du jour du site bonjourmadame.fr
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetBoujourmadamePicture()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync("http://www.bonjourmadame.fr/");
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                // Récupération de l'image
                //string[] pictures = doc.DocumentNode.Descendants("a").Where(x => (string)x.GetAttributeValue("class", "") == "photo-url").Select(y => (string)y.GetAttributeValue("href", "")).ToArray();

                Regex pic_regex = new Regex("<img src=" + "((.|\n)*?)" + "(.jpg|.jpeg|.png|.bmp)");

                MatchCollection pic_match = pic_regex.Matches(responseBodyAsText);

                if (pic_match.Count > 0) return pic_match[0].ToString().Substring(10);

                
                return "/Resources/Backgrounds/bg_madame.jpg";
            }
            catch
            {
                return "/Resources/Backgrounds/bg_madame.jpg";
            }
        }

        /// <summary>
        /// Récupère l'image du jour du Cosmos
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetCosmosPicture()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync("http://apod.nasa.gov/apod/astropix.html");
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                string start = "<a";
                string end = ".jpg";

                Regex regex = new Regex(start + "(.*?)" + end);
                MatchCollection matches = regex.Matches(responseBodyAsText);

                if (matches.Count > 0)
                    return "http://apod.nasa.gov/apod/" + matches[0].ToString().Substring(9);

                return "/Resources/Backgrounds/bg_stars.jpg";
            }
            catch
            {
                return "/Resources/Backgrounds/bg_stars.jpg";
            }
        }

        
        /// <summary>
        /// Récupère les images populaires de Flickr
        /// </summary>
        /// <returns></returns>
        public async Task GetFlickrPictures()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync("http://www.flickr.com/explore");
                response.EnsureSuccessStatusCode();
                string responseBodyAsText = await response.Content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(responseBodyAsText);

                List<String> array_results = new List<string>();

                Regex regex = new Regex("data-defer-src=\"" + "((.)*?)" + "(.jpg)");
                MatchCollection matches = regex.Matches(responseBodyAsText);

                if (matches.Count > 0)
                {
                    foreach (var link in matches)
                    {
                        ListPicturesFlickr.Add(link.ToString().Replace("data-defer-src=\"", ""));
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Retourne (aléatoirement) une URL d'image de Flickr.
        /// Vérifie si les Lists d'image ne sont pas vides.
        /// Si c'est le cas, il y a un appel de la méthode pour récupérer dabord les images.
        /// </summary>
        /// <param name="website"></param>
        /// <returns></returns>
        public async Task<string> ChooseARandomPicture(string website)
        {
            Random random = new Random();
            int alea = 0;

            if (website == "Flickr")
            {
                if (ListPicturesFlickr.Count < 1)
                {
                    await GetFlickrPictures();
                }

                if (ListPicturesFlickr.Count > 0)
                {

                    // Enregistre l'heure
                    // à laquelle on a effectué l'opération
                    //SaveLastTimeBackgroundRefresh();

                    // Sauvegarde la liste des URL d'images récupérée
                    SaveListPicturesFlickr();

                    alea = random.Next(ListPicturesFlickr.Count);

                    _CurrentBackgroundLink = ListPicturesFlickr.ElementAt(alea);
                    //SaveData();

                    return ListPicturesFlickr.ElementAt(alea);
                }
            }

            return null;
        }

        

        /// <summary>
        /// Enregistre dans l'IO la liste des URLs des images de Flickr
        /// (Permet de changer régulièrement de fond d'écran + lockscreen
        /// ainsi que de gagner du temps sur le chargement)
        /// </summary>
        /// <returns></returns>
        public async Task SaveListPicturesFlickr()
        {
            // On s'assure que la liste est crée et qu'elle contient
            // des éléments
            if (ListPicturesFlickr == null) return;
            if (ListPicturesFlickr.Count < 1) return;

            _mutex.WaitOne();
            IsolatedStorageSettings.ApplicationSettings["ListPicturesFlickr"] = ListPicturesFlickr;
            _mutex.ReleaseMutex();
        }

        
        /// <summary>
        /// Récupère la liste des URLs des images de Flickr
        /// depuis l'IO
        /// </summary>
        /// <returns></returns>
        public async Task LoadListPicturesFlickr()
        {
            if (ListPicturesFlickr == null) return;

            _mutex.WaitOne();
            if (IsolatedStorageSettings.ApplicationSettings.Contains("ListPicturesFlickr"))
            {
                ListPicturesFlickr = IsolatedStorageSettings.ApplicationSettings["ListPicturesFlickr"] as List<String>;
            }
            _mutex.ReleaseMutex();
        }


        /// <summary>
        /// Enregistre les données de l'applcation dans l'IO
        /// </summary>
        public void SaveData()
        {
            _mutex.WaitOne();
            //IsolatedStorageSettings.ApplicationSettings["_firstLaunch"] = _FirstLaunch; // var disant si c'est le premier lancement
            IsolatedStorageSettings.ApplicationSettings["_TTSIsActivated"] = _TTSIsActivated; // var concernant l'activation du TTS
            IsolatedStorageSettings.ApplicationSettings["_authorBackgroundActivated"] = _AuthorBackgroundActivated; // var de l'image de fond de l'auteur
            IsolatedStorageSettings.ApplicationSettings["_IsAuthorsListSaved"] = _IsAuthorsListSaved; // var de la liste d'auteurs
            IsolatedStorageSettings.ApplicationSettings["_ApplicationBackgroundType"] = _ApplicationBackgroundType; // type de fond de l'appli
            IsolatedStorageSettings.ApplicationSettings["_CurrentBackgroundLink"] = _CurrentBackgroundLink; // lien de la dernière image appliquée
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Récupère les données d'application à partir de l'IO
        /// </summary>
        public void LoadDataFromIO()
        {
            _mutex.WaitOne();
            //if (IsolatedStorageSettings.ApplicationSettings.Contains("_firstLaunch"))
            //{
            //    _FirstLaunch = (bool)IsolatedStorageSettings.ApplicationSettings["_firstLaunch"];
            //}
            if (IsolatedStorageSettings.ApplicationSettings.Contains("_TTSIsActivated"))
            {
                _TTSIsActivated = (bool)IsolatedStorageSettings.ApplicationSettings["_TTSIsActivated"];
            }
            if (IsolatedStorageSettings.ApplicationSettings.Contains("_authorBackgroundActivated"))
            {
                _AuthorBackgroundActivated = (bool)IsolatedStorageSettings.ApplicationSettings["_authorBackgroundActivated"];
            }
            if (IsolatedStorageSettings.ApplicationSettings.Contains("_IsAuthorsListSaved"))
            {
                _IsAuthorsListSaved = (bool)IsolatedStorageSettings.ApplicationSettings["_IsAuthorsListSaved"];
            }
            if (IsolatedStorageSettings.ApplicationSettings.Contains("_ApplicationBackgroundType"))
            {
                _ApplicationBackgroundType = (string)IsolatedStorageSettings.ApplicationSettings["_ApplicationBackgroundType"];
            }
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Sauvegarde la collection de citatiions (CollectionQuotes)
        /// (12 premières citations)
        /// </summary>
        /// <returns></returns>
        public async Task SaveCollectionToIO()
        {
            _mutex.WaitOne();
            if (CollectionQuotes.Count > 0)
                await MyDataSerializer<ObservableCollection<Quote>>.SaveObjectsAsync(CollectionQuotes, "CollectionQuotes.xml");
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Récupère les citations de la CollectionQuotes à partir de l'IO
        /// (12 premières citations)
        /// </summary>
        /// <returns></returns>
        public async Task LoadCollectionFromIO()
        {
            _mutex.WaitOne();
            try
            {
                CollectionQuotes = await MyDataSerializer<ObservableCollection<Quote>>.RestoreObjectsAsync("CollectionQuotes.xml");
            }
            catch (IsolatedStorageException exception)
            {
                System.Windows.MessageBox.Show("La liste des citations n'a pas pu être chargée.\nMessage:{0}" + exception);
            }
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Enregistre la liste des auteurs du site (evene) dans l'IO
        /// </summary>
        /// <returns></returns>
        public async Task SaveAuthorsList()
        {
            if (ListAuthorsSorted.Count > 0)
            {
                _mutex.WaitOne();
                //_authorsListSaved = true;
                //await MyDataSerializer<List<AlphaKeyGroup<Author>>>.SaveObjectsAsync(ListAuthorsSorted, "ListSorted.xml");
                await MyDataSerializer<ObservableCollection<Author>>.SaveObjectsAsync(CollectionAuthors, "CollectionAuthors.xml");
                _mutex.ReleaseMutex();

            }
        }

        /// <summary>
        /// Récupère la liste des auteurs (evene) depuis l'IO
        /// </summary>
        /// <returns></returns>
        public async Task LoadAuthorsList()
        {
            _mutex.WaitOne();
            try
            {
                CollectionAuthors = await MyDataSerializer<ObservableCollection<Author>>.RestoreObjectsAsync("CollectionAuthors.xml");
            }
            catch (IsolatedStorageException exception)
            {
                // System.Windows.MessageBox.Show("La liste des autheurs n'a pas pu être chargée.\nMessage:{0}" + exception);
            }
            _mutex.ReleaseMutex();
        }


        /// <summary>
        /// Surveille le temps depuis la dernière
        /// actualisation des images de fond (flickr)
        /// </summary>
        /// <returns>retourne vrai on doit récupérer une nouvelle liste d'images, faux sinon</returns>
        public bool BackgroundMustBeRefreshed()
        {
            //DateTimeOffset lastTimeBackgroundRefresh;
            //if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeBackgroundRefresh"))
            //{
            //    lastTimeBackgroundRefresh = (DateTimeOffset)IsolatedStorageSettings.ApplicationSettings["lastTimeBackgroundRefresh"];
            //}
            //else
            //{
            //    // Si la variable n'exsite pas,
            //    // on affete lastRefreshTime à l'heure actuelle
            //    // et on enregistre cette heure dans l'IO
                
            //    // On renvoie vrai pour recharger la liste d'images
            //    // après avoir enregistré l'heure

            //    // lastTimeBackgroundRefresh = DateTimeOffset.Now;
            //    SaveLastTimeBackgroundRefresh();
            //    return true;
            //}


            //var CurrentDuration = DateTimeOffset.Now.Subtract(lastTimeBackgroundRefresh);

            //// Vérifie si la dernière actualisation des images est supérieure à 4h
            //if(TimeSpan.FromHours(CurrentDuration.Hours) > TimeSpan.FromHours(4))
            //{
                
            //    if (_ApplicationBackgroundType == "Flickr")
            //        ListPicturesFlickr.Clear();

            //    // retourne vrai si
            //    // on doit récupérer une nouvelle liste d'images
            //    return true;
            //}

            // Retourne faux si 
            // on ne doit pas réactualiser la liste des images
            return false;
        }

        /// <summary>
        /// Enregistre, dans l'IO, l'heure du dernier téléchargement
        /// de fonds pour le lockscreen depuis le WEB
        /// </summary>
        //public void SaveLastTimeBackgroundRefresh()
        //{
        //    _mutex.WaitOne();
        //    DateTimeOffset lastTime = DateTimeOffset.Now;

        //    if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeBackground"))
        //        IsolatedStorageSettings.ApplicationSettings["lastTimeBackground"] = lastTime;
        //    else
        //        IsolatedStorageSettings.ApplicationSettings["lastTimeBackground"] = lastTime;
        //    _mutex.ReleaseMutex();
        //}

        public async Task TimeSave()
        {
            //_mutex.WaitOne();
            //DateTimeOffset lastTime = DateTimeOffset.Now;

            //if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeBackground"))
            //    IsolatedStorageSettings.ApplicationSettings["lastTimeBackground"] = lastTime;
            //else
            //    IsolatedStorageSettings.ApplicationSettings["lastTimeBackground"] = lastTime;
            //_mutex.ReleaseMutex();
        }
        public void HowLongForBackground()
        {
            // TEST SI LES CITATIONS DOIVENT ETRE REACTUALISER
            DateTimeOffset lastTime;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeBackground"))
            {
                lastTime = (DateTimeOffset)IsolatedStorageSettings.ApplicationSettings["lastTimeBackground"];
            }
            //else return true;


            var CurrentDuration = DateTimeOffset.Now.Subtract(lastTime);

            // Vérifie si la dernière actualisation des images est supérieure à 4h
            if (TimeSpan.FromHours(CurrentDuration.Hours) > TimeSpan.FromHours(2))
            {
                // retourne vrai si
                // on doit récupérer de nouvelles citations
                //return true;
            }

            // Retourne faux si 
            // on ne doit pas réactualiser les citations
            //return false;
        }

        /// <summary>
        /// Enregistre, dans l'IO, l'heure du dernier accès WEB
        /// pour récupérer les dernières citations
        /// </summary>
        public void SaveLastTimeQuoteRefresh()
        {
            DateTimeOffset lastTimeQuoteRefresh = DateTimeOffset.Now;

            if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeQuoteRefresh"))
                IsolatedStorageSettings.ApplicationSettings["lastTimeQuoteRefresh"] = lastTimeQuoteRefresh;
            else
                IsolatedStorageSettings.ApplicationSettings.Add("lastTimeQuoteRefresh", lastTimeQuoteRefresh);
        }


        public void DownloadImagefromServer(string URL)
        {
            WebClient client = new WebClient();
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
            // give the image url that we need to download and store on IsolatedStorage
            client.OpenReadAsync(new Uri(URL, UriKind.Absolute));
        }

        void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(e.Result);
            //img.Source = bitmap;

            // Create a filename for JPEG file in isolated storage.
            String tempJPEG = "DownloadedWallpaper.jpg";

            // Create virtual store and file stream. Check for duplicate tempJPEG files.
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(tempJPEG))
                {
                    myIsolatedStorage.DeleteFile(tempJPEG);
                }

                IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(tempJPEG);

                StreamResourceInfo sri = null;
                Uri uri = new Uri(tempJPEG, UriKind.Relative);
                sri = Application.GetResourceStream(uri);

                //BitmapImage bitmap = new BitmapImage();
                //bitmap.SetSource(sri.Stream);
                WriteableBitmap wb = new WriteableBitmap(bitmap);

                // Encode WriteableBitmap object to a JPEG stream.
                Extensions.SaveJpeg(wb, fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);

                //wb.SaveJpeg(fileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
                fileStream.Close();

                NotifyRequestEndDownloadImagefromServer();
            }

            // call function to set downloaded image as lock screen 
            //LockScreenChange("DownloadedWalleper.jpg", false);
        }


        /// <summary>
        /// Ajoute de la durée au background agent
        /// </summary>
        public void AddDayBackgroundAgent()
        {
            // Si on trouve l'agent on ajoute des jours à la date d'expiration
            string name = "Citations365TaskAgent";
            var periodicTask = ScheduledActionService.Find(name) as PeriodicTask;
            if (periodicTask != null)
            {
                try
                {
                    periodicTask.ExpirationTime.AddDays(15);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Met à jour la Tuile principale
        /// </summary>
        public void UpdateMainTile()
        {
            if (CollectionQuotes.Count > 0)
            {
                string content = CollectionQuotes.ElementAt(0).content;
                string wideContent = CollectionQuotes.ElementAt(0).content;
                string author = CollectionQuotes.ElementAt(0).author;

                if (CollectionQuotes[0].content.Length > 36)
                {
                    content = content.Substring(0, 36) + "...";

                    if (CollectionQuotes[0].content.Length > 105)
                    {
                        wideContent = wideContent.Substring(0, 105) + "...";
                    }
                }
                if (CollectionQuotes[0].author.Length > 16)
                {
                    author = CollectionQuotes[0].author.Substring(0, 16) + "...";
                }

                // Get application's main tile - application tile always first item in the ActiveTiles collection
                // wheter it is pinned or not
                var mainTile = ShellTile.ActiveTiles.FirstOrDefault();
                if (mainTile != null)
                {
                    FlipTileData tileData = new FlipTileData()
                    {
                        BackContent = content,
                        WideBackContent = wideContent,
                        BackTitle = author,
                    };

                    mainTile.Update(tileData);
                }
            }
        }


        /// <summary>
        /// Normalise le text:
        /// Retire les tags html (<h1></h1>, ...), les caractères spéciaux (&amp;)
        /// ainsi que les accents
        /// </summary>
        /// <param name="text"></param>
        public string DeleteHTMLTags(string text)
        {
            // Html tags
            text = Regex.Replace(text, @"<(.|\n)*?>", string.Empty);

            // Caractères spéciaux
            text = text
                .Replace("&laquo;", "")
                .Replace("&ldquo;", "")
                .Replace("&rdquo;", "")
                .Replace("&nbsp;", "")
                .Replace("&raquo;", "")
                .Replace("&#039;", "'")
                .Replace("&quot;", "'")
                .Replace("&amp;", "&")
                .Replace("[+]", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("\n", "")
                .Replace("  ", "");

            return text;
        }

        // HANDLERS
        /// <summary>
        /// handler image autheur
        /// </summary>
        public static event EventHandler eventHandlerGetAuthorPictures;
        private void NotifyRequesEndGetAuthorPictures()
        {
            EventHandler handler = eventHandlerGetAuthorPictures;
            if (null != handler)
            {
                handler(this, new EventArgs());
            }
        }

        /// <summary>
        /// Handler pour l'image de l'écran de verrouillage
        /// </summary>
        public static event EventHandler eventHandlerDownloadImagefromServer;
        private void NotifyRequestEndDownloadImagefromServer()
        {
            EventHandler handler = eventHandlerDownloadImagefromServer;
            if (null != handler)
            {
                handler(this, new EventArgs());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}