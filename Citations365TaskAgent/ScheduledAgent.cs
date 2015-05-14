using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System.Net;
using System;
using HtmlAgilityPack;
using System.Linq;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Net.NetworkInformation;
using System.Net.Http;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace Citations365TaskAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        public event EventHandler SearchCompletedEvent;

        /// <remarks>
        /// Le constructeur ScheduledAgent initialise le gestionnaire UnhandledException
        /// </remarks>
        static ScheduledAgent()
        {
            // S'abonner au gestionnaire d'exceptions prises en charge
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code à exécuter sur les exceptions non gérées
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // Une exception non gérée s'est produite ; arrêt dans le débogueur
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent de Citations qui exécute une tâche planifiée
        /// </summary>
        /// <param name="task">
        /// La tâche appelée
        /// </param>
        /// <remarks>
        /// Cette méthode est appelée lorsqu'une tâche périodique est appelée
        /// </remarks>
        protected override async void OnInvoke(ScheduledTask task)
        {
            //NOTE: On déplace les méthodes NotifyComplete() à l'intérireur des fonctions, sinon la méthode asynchrone n'a pas le temps de s'exécuter

            //TODO: ajoutez du code pour exécuter votre tâche en arrière-plan
            //if (NetworkInterface.GetIsNetworkAvailable())
            //{
            if (QuotesMustBeRefreshed())
            {
                // Si la fonction détermine qu'on doit
                // récupérer de nouvelles citations, alors
                // on appelle la fonction suivante
                GetDayQuotes();
            }

            //if(BackgroundMustBeRefreshed())
            //{
            //    //    // Si la fonction détermine qu'on doit
            //    //    // rafraichir le fond du lockscreen, alors
            //    //    // on appelle la fonction suivante
            //    RefreshLockscreenImage();
            // }
                
            //}
            //else
            //{
            //    // Pas de connexion internet
            //    NotifyComplete();
            //}
        }


        // SUB METHODS --------------
        bool BackgroundMustBeRefreshed()
        {
            return true;
        }

        bool QuotesMustBeRefreshed()
        {
            // TEST SI LES CITATIONS DOIVENT ETRE REACTUALISER
            DateTimeOffset lastTimeQuoteRefresh;
            if (IsolatedStorageSettings.ApplicationSettings.Contains("lastTimeQuoteRefresh"))
            {
                lastTimeQuoteRefresh = (DateTimeOffset)IsolatedStorageSettings.ApplicationSettings["lastTimeQuoteRefresh"];
            }
            else return true;


            var CurrentDuration = DateTimeOffset.Now.Subtract(lastTimeQuoteRefresh);

            // Vérifie si la dernière actualisation des images est supérieure à 4h
            if (TimeSpan.FromHours(CurrentDuration.Hours) > TimeSpan.FromHours(2))
            {
                // retourne vrai si
                // on doit récupérer de nouvelles citations
                return true;
            }

            // Retourne faux si 
            // on ne doit pas réactualiser les citations
            return false;
        }


        void GetDayQuotes()
        {
            try
            {
                WebClient webclient = new WebClient();
                webclient.DownloadStringAsync(new Uri("http://evene.lefigaro.fr/citations"));
                webclient.DownloadStringCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        if (SearchCompletedEvent != null)
                            SearchCompletedEvent(this, EventArgs.Empty);
                        return;
                    }

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(e.Result);

                    // Récupération des données
                    // contenus
                    //string[] contents = doc.DocumentNode.Descendants("h3").Select(y => y.InnerText).ToArray();
                    // dates
                    //string[] dates = doc.DocumentNode.Descendants("span").Where(x => (string)x.GetAttributeValue("class", "") == "date").Select(y => y.InnerText).ToArray();
                    // auteurs
                    //string[] authors_brutes = doc.DocumentNode.Descendants("h4").Select(y => y.InnerText).ToArray();

                    // travail sur le tableau d'auteurs
                    // le tableau authors_brutes contient des entrées qu'on souhaite supprimer
                    // on va donc tester le début de la chaine de caractères
                    //string[] authors = new string[authors_brutes.Length];
                    //int j = 0; //pour remplir le nouveau tableau
                    //int moncompte = 0;
                    // - moncompte -permet de savoir si on a 2 entrées indésirables qui se suivent
                    // si tel est le cas, on laisse une entrée vide dans le nouveau tableau
                    // car cette citation n'a pas d'auteur.

                    //for (int i = 0; i < 2; i++)
                    //{

                    //    if (authors_brutes[i].StartsWith("&laquo;&nbsp;"))
                    //    {
                    //        if (moncompte == 0)
                    //            moncompte++;
                    //        else
                    //            j++;
                    //    }
                    //    else
                    //    {
                    //        authors[j] = authors_brutes[i];
                    //        j++;
                    //        moncompte = 0;
                    //    }
                    //}

                    String content = null, 
                           author  = null;

                    string[] contents = doc.DocumentNode.Descendants("article").Select(y => y.InnerHtml).ToArray();
                    Regex content_regex = new Regex("<div class=\"figsco__quote__text\">" + "((.|\n)*?)" + "</div>");
                    Regex author_regex = new Regex("<div class=\"figsco__quote__from\">" + "((.|\n)*?)" + "</div>");

                    for (int i = 0; i < 1; i++)
                    {
                        MatchCollection content_match = content_regex.Matches(contents[i]);
                        MatchCollection author_match = author_regex.Matches(contents[i]);

                        String quote_content = null;
                                                
                        // Récupère le contenu de la citation
                        if (content_match.Count > 0)
                        {
                            content = DeleteHTMLTags(content_match[0].ToString());
                        }
                        else continue;

                        // Récupère l'auteur
                        if (author_match.Count > 0)
                        {
                            author = DeleteHTMLTags(author_match[0].ToString());
                        }
                        break;
                    }
                    //contents[0] = ReplaceSpecialChars(contents[0]);

                    try
                    {
                        int limit = content.Length;

                        if (content.Length > 36)
                        {
                            limit = 36;
                            if (content.Length > 105)
                            {
                                content = content.Substring(0, 105) + "...";
                            }
                        }
                        if (author.Length > 16)
                        {
                            author = author.Substring(0, 16) + "...";
                        }


                        var TileToFind = ShellTile.ActiveTiles.FirstOrDefault();
                        if (TileToFind != null)
                        {
                            FlipTileData NewTileData = new FlipTileData()
                            {
                                BackContent = content.Substring(0, limit) + "...",
                                WideBackContent = content,
                                BackTitle = author,
                            };
                            TileToFind.Update(NewTileData);
                        }

                        //int limit = contents[0].Length;

                        //if (contents[0].Length > 36)
                        //{
                        //    limit = 36;
                        //    if (contents[0].Length > 105)
                        //    {
                        //        contents[0] = contents[0].Substring(0, 105) + "...";
                        //    }
                        //}
                        //if (authors[0].Length > 16)
                        //{
                        //    authors[0] = authors[0].Substring(0, 16) + "...";
                        //}


                        //var TileToFind = ShellTile.ActiveTiles.FirstOrDefault();
                        //if (TileToFind != null)
                        //{
                        //    FlipTileData NewTileData = new FlipTileData()
                        //    {
                        //        BackContent = contents[0].Substring(0, limit) + "...",
                        //        WideBackContent = contents[0],
                        //        BackTitle = authors[0],
                        //    };
                        //    TileToFind.Update(NewTileData);
                        //}

                        // Sauvegarde l'heure la dernière récupération a été faite
                        // si SUCCES
                        SaveLastTimeQuoteRefresh();

                        // Signale qu'on a terminé d'exécuter l'Agent
                        NotifyComplete();
                    }
                    catch
                    {
                        NotifyComplete();
                    }
                };
            }
            catch
            {
                NotifyComplete();
            }
        }

        public string ReplaceSpecialChars(string text)
        {
            text = text.Replace("&#039;", "'").Replace("&laquo;", "")
                .Replace("&nbsp;", "").Replace("&raquo;", "").Replace("&quot;", "'");
            return text;
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


        async void RefreshLockscreenImage()
        {
            string _ApplicationBackgroundType="";
            string URL = "";

            // TEST LE TYPE (background)
            // Récupérer le type de Background l'utilisateur a choisi
            if (IsolatedStorageSettings.ApplicationSettings.Contains("_ApplicationBackgroundType"))
            {
                _ApplicationBackgroundType = (string)IsolatedStorageSettings.ApplicationSettings["_ApplicationBackgroundType"];
            }

            // OBTIENT L'URL (image)
            if(_ApplicationBackgroundType == "Madame")
            {
                URL = await GetBoujourmadamePicture();
            }
            else if (_ApplicationBackgroundType=="Cosmos")
            {
                URL = await GetAstroPicture();
            }
            
            //else if(_ApplicationBackgroundType=="Flickr")
            //{
            //    // Récupérer la liste d'URL depuis l'IO
            //    List<String> ListPicturesFlickr;
            //    if (IsolatedStorageSettings.ApplicationSettings.Contains("ListPicturesFlickr"))
            //    {
            //        ListPicturesFlickr = IsolatedStorageSettings.ApplicationSettings["ListPicturesFlickr"] as List<String>;
            //        URL = await ChooseARandomPicture(ListPicturesFlickr);
            //    }
            //    else return;
            //}
            

            // TELECHARGE LE .JPG
            DownloadImagefromServer(URL);

            // APPLIQUE L'IMAGE
        }

        public void DownloadImagefromServer(string URL)
        {
            if (!(URL.StartsWith("http://"))) return;

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
            }

            // call function to set downloaded image as lock screen 
            LockHelper("DownloadedWallpaper.jpg", false);
        }

        private async void LockHelper(string filePathOfTheImage, bool isAppResource)
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!isProvider)
                {
                    // If you're not the provider, this call will prompt the user for permission.
                    // Calling RequestAccessAsync from a background agent is not allowed.
                    var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

                    // Only do further work if the access was granted.
                    isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;
                }

                if (isProvider)
                {
                    // At this stage, the app is the active lock screen background provider.

                    // The following code example shows the new URI schema.
                    // ms-appdata points to the root of the local app data folder.
                    // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                    var schema = isAppResource ? "ms-appx:///" : "ms-appdata:///Local/";
                    var uri = new Uri(schema + filePathOfTheImage, UriKind.Absolute);

                    // Set the lock screen background image.
                    Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);

                    // Get the URI of the lock screen background image.
                    var currentImage = Windows.Phone.System.UserProfile.LockScreen.GetImageUri();
                    // System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                }
                else
                {
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

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
        public async Task<string> GetAstroPicture()
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
            //HttpClient httpClient = new HttpClient();
            //HttpResponseMessage response = null;
            //try
            //{
            //    response = await httpClient.GetAsync("http://www.flickr.com/explore");
            //    response.EnsureSuccessStatusCode();
            //    string responseBodyAsText = await response.Content.ReadAsStringAsync();

            //    HtmlDocument doc = new HtmlDocument();
            //    doc.LoadHtml(responseBodyAsText);

            //    List<String> array_results = new List<string>();

            //    string start = "src=\"http://f";
            //    string end = ".jpg";

            //    Regex regex = new Regex(start + "(.*?)" + end);
            //    MatchCollection matches = regex.Matches(responseBodyAsText);

            //    if (matches.Count > 0)
            //    {
            //        foreach (var link in matches)
            //        {
                        //ListPicturesFlickr.Add(link.ToString().Substring(5));
            //        }
            //    }
            //}
            //catch
            //{
            //}
        }

        /// <summary>
        /// Retourne (aléatoirement) une URL d'image de Flickr.
        /// Vérifie si les Lists d'image ne sont pas vides.
        /// Si c'est le cas, il y a un appel de la méthode pour récupérer dabord les images.
        /// </summary>
        /// <param name="website"></param>
        /// <returns></returns>
        public async Task<string> ChooseARandomPicture(List<string> aList)
        {
            if (aList == null) return null;

            Random random = new Random();
            int alea = 0;

            alea = random.Next(aList.Count);
            return aList.ElementAt(alea);

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
    }
}