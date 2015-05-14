using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Citations365.Resources;
using Citations365.ViewModels;
using System.IO.IsolatedStorage;

namespace Citations365
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;

        /// <summary>
        /// ViewModel statique utilisé par les vues avec lesquelles établir la liaison.
        /// </summary>
        /// <returns>Objet MainViewModel.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Différer la création du modèle de vue autant que nécessaire
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        /// <summary>
        /// Permet d'accéder facilement au frame racine de l'application téléphonique.
        /// </summary>
        /// <returns>Frame racine de l'application téléphonique.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        enum SessionType
        {
            None,
            Home,
            DeepLink
        }

        // Set to Home when the app is launched from Primary tile.
        // Set to DeepLink when the app is launched from Deep Link.
        private SessionType sessionType = SessionType.None;

        // Set to true when the page navigation is being reset 
        bool wasRelaunched = false;

        // set to true when 5 min passed since the app was relaunched
        bool mustClearPagestack = false;

        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;



        /// <summary>
        /// Constructeur pour l'objet Application.
        /// </summary>
        public App()
        {
            // Gestionnaire global pour les exceptions non interceptées.
            UnhandledException += Application_UnhandledException;

            // Initialisation du XAML standard
            InitializeComponent();

            // Initialisation spécifique au téléphone
            InitializePhoneApplication();

            // Initialisation de l'affichage de la langue
            InitializeLanguage();

            // Affichez des informations de profilage graphique lors du débogage.
            if (Debugger.IsAttached)
            {
                // Affichez les compteurs de fréquence des trames actuels.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Affichez les zones de l'application qui sont redessinées dans chaque frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Activez le mode de visualisation d'analyse hors production,
                // qui montre les zones d'une page sur lesquelles une accélération GPU est produite avec une superposition colorée.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Empêche l'écran de s'éteindre lorsque le débogueur est utilisé en désactivant
                // la détection de l'état inactif de l'application.
                // Attention :- À utiliser uniquement en mode de débogage. Les applications qui désactivent la détection d'inactivité de l'utilisateur continueront de s'exécuter
                // et seront alimentées par la batterie lorsque l'utilisateur ne se sert pas du téléphone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code à exécuter lorsque l'application démarre (par exemple, à partir de Démarrer)
        // Ce code ne s'exécute pas lorsque l'application est réactivée
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            // When a new instance of the app is launched, clear all deactivation settings
            RemoveCurrentDeactivationSettings();
        }

        // Code à exécuter lorsque l'application est activée (affichée au premier plan)
        // Ce code ne s'exécute pas lorsque l'application est démarrée pour la première fois
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Assurez-vous que l'état de l'application est correctement restauré
            //if (!App.ViewModel.IsDataLoaded)
            //{
            //    App.ViewModel.LoadData();
            //}

            // If some interval has passed since the app was deactivated (30 seconds in this example),
            // then remember to clear the back stack of pages
            // mustClearPagestack = CheckDeactivationTimeStamp();
            mustClearPagestack = false;

            // If IsApplicationInstancePreserved is not true, then set the session type to the value
            // saved in isolated storage. This will make sure the session type is correct for an
            // app that is being resumed after being tombstoned.
            if (!e.IsApplicationInstancePreserved)
            {
                RestoreSessionType();
            }
        }

        // Code à exécuter lorsque l'application est désactivée (envoyée à l'arrière-plan)
        // Ce code ne s'exécute pas lors de la fermeture de l'application
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Assurez-vous que l'état de l'application requis est persistant ici.
            // When the applicaiton is deactivated, save the current deactivation settings to isolated storage
            SaveCurrentDeactivationSettings();
            App.ViewModel.SaveData();
        }

        // Code à exécuter lors de la fermeture de l'application (par exemple, lorsque l'utilisateur clique sur Précédent)
        // Ce code ne s'exécute pas lorsque l'application est désactivée
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            // When the application closes, delete any deactivation settings from isolated storage
            RemoveCurrentDeactivationSettings();
        }

        // Code à exécuter en cas d'échec d'une navigation
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // Échec d'une navigation ; arrêt dans le débogueur
                Debugger.Break();
            }
        }

        // Code à exécuter sur les exceptions non gérées
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // Une exception non gérée s'est produite ; arrêt dans le débogueur
                Debugger.Break();
            }
        }

        #region Initialisation de l'application téléphonique

        // Éviter l'initialisation double
        private bool phoneApplicationInitialized = false;

        // Ne pas ajouter de code supplémentaire à cette méthode
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Créez le frame, mais ne le définissez pas encore comme RootVisual ; cela permet à l'écran de
            // démarrage de rester actif jusqu'à ce que l'application soit prête pour le rendu.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Gérer les erreurs de navigation
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Gérer les requêtes de réinitialisation pour effacer la pile arrière
            RootFrame.Navigated += CheckForResetNavigation;

            // Monitor deep link launching 
            RootFrame.Navigating += RootFrame_Navigating;

            // Garantir de ne pas retenter l'initialisation
            phoneApplicationInitialized = true;
        }

        // Ne pas ajouter de code supplémentaire à cette méthode
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Définir le Visual racine pour permettre à l'application d'effectuer le rendu
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Supprimer ce gestionnaire, puisqu'il est devenu inutile
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }


        // Event handler for the Navigating event of the root frame. Use this handler to modify
        // the default navigation behavior.
        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {

            // If the session type is None or New, check the navigation Uri to determine if the
            // navigation is a deep link or if it points to the app's main page.
            if (sessionType == SessionType.None && e.NavigationMode == NavigationMode.New)
            {
                // This block will run if the current navigation is part of the app's intial launch


                // Keep track of Session Type 
                if (e.Uri.ToString().Contains("DeepLink=true"))
                {
                    sessionType = SessionType.DeepLink;
                }
                else if (e.Uri.ToString().Contains("/MainPage.xaml"))
                {
                    sessionType = SessionType.Home;
                }
            }


            if (e.NavigationMode == NavigationMode.Reset)
            {
                // This block will execute if the current navigation is a relaunch.
                // If so, another navigation will be coming, so this records that a relaunch just happened
                // so that the next navigation can use this info.
                wasRelaunched = true;
            }
            else if (e.NavigationMode == NavigationMode.New && wasRelaunched)
            {
                // This block will run if the previous navigation was a relaunch
                wasRelaunched = false;

                if (e.Uri.ToString().Contains("DeepLink=true"))
                {
                    // This block will run if the launch Uri contains "DeepLink=true" which
                    // was specified when the secondary tile was created in MainPage.xaml.cs

                    sessionType = SessionType.DeepLink;
                    // The app was relaunched via a Deep Link.
                    // The page stack will be cleared.
                }
                else if (e.Uri.ToString().Contains("/MainPage.xaml"))
                {
                    // This block will run if the navigation Uri is the main page
                    if (sessionType == SessionType.DeepLink)
                    {
                        // When the app was previously launched via Deep Link and relaunched via Main Tile, we need to clear the page stack. 
                        sessionType = SessionType.Home;
                    }
                    else
                    {
                        if (!mustClearPagestack)
                        {
                            //The app was previously launched via Main Tile and relaunched via Main Tile. Cancel the navigation to resume.
                            e.Cancel = true;
                            RootFrame.Navigated -= ClearBackStackAfterReset;
                        }
                    }
                }

                mustClearPagestack = false;
            }
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // Si l'application a reçu une navigation de « réinitialisation », nous devons vérifier
            // sur la navigation suivante pour voir si la pile de la page doit être réinitialisée
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Désinscrire l'événement pour qu'il ne soit plus appelé
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Effacer uniquement la pile des « nouvelles » navigations (avant) et des actualisations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // Pour une interface utilisateur cohérente, effacez toute la pile de la page
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // ne rien faire
            }
        }

        #endregion

        // Initialise la police de l'application et le sens du flux tels qu'ils sont définis dans ses chaînes de ressource localisées.
        //
        // Pour vous assurer que la police de votre application est alignée avec les langues prises en charge et que le
        // FlowDirection pour chacune de ces langues respecte le sens habituel, ResourceLanguage
        // et ResourceFlowDirection doivent être initialisés dans chaque fichier resx pour faire correspondre ces valeurs avec la
        // culture du fichier. Par exemple :
        //
        // AppResources.es-ES.resx
        //    La valeur de ResourceLanguage doit être « es-ES »
        //    La valeur de ResourceFlowDirection doit être « LeftToRight »
        //
        // AppResources.ar-SA.resx
        //     La valeur de ResourceLanguage doit être « ar-SA »
        //     La valeur de ResourceFlowDirection doit être « RightToLeft »
        //
        // Pour plus d'informations sur la localisation des applications Windows Phone, consultez le site http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Définissez la police pour qu'elle corresponde à la langue d'affichage définie par la
                // chaîne de ressource ResourceLanguage pour chaque langue prise en charge.
                //
                // Rétablit la police de la langue neutre si la langue d'affichage
                // du téléphone n'est pas prise en charge.
                //
                // Si une erreur de compilateur est détectée, ResourceLanguage est manquant dans
                // le fichier de ressources.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Définit FlowDirection pour tous les éléments sous le frame racine en fonction de la
                // de la chaîne de ressource ResourceFlowDirection pour chaque
                // langue prise en charge.
                //
                // Si une erreur de compilateur est détectée, ResourceFlowDirection est manquant dans
                // le fichier de ressources.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // Si une exception est détectée ici, elle est probablement due au fait que
                // ResourceLanguage n'est pas correctement défini sur un code de langue pris en charge
                // ou que ResourceFlowDirection est défini sur une valeur différente de LeftToRight
                // ou RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }

        // Helper method for adding or updating a key/value pair in isolated storage
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Contains(Key))
            {
                // If the value has changed
                if (settings[Key] != value)
                {
                    // Store the new value
                    settings[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Add(Key, value);
                valueChanged = true;
            }
            return valueChanged;
        }

        // Helper method for removing a key/value pair from isolated storage
        public void RemoveValue(string Key)
        {
            // If the key exists
            if (settings.Contains(Key))
            {
                settings.Remove(Key);
            }
        }

        // Called when the app is deactivating. Saves the time of the deactivation and the 
        // session type of the app instance to isolated storage.
        public void SaveCurrentDeactivationSettings()
        {
            if (AddOrUpdateValue("DeactivateTime", DateTimeOffset.Now))
            {
                settings.Save();
            }

            if (AddOrUpdateValue("SessionType", sessionType))
            {
                settings.Save();
            }

        }

        // Called when the app is launched or closed. Removes all deactivation settings from
        // isolated storage
        public void RemoveCurrentDeactivationSettings()
        {
            RemoveValue("DeactivateTime");
            RemoveValue("SessionType");
            settings.Save();
        }

        // Helper method to determine if the interval since the app was deactivated is
        // greater than 30 seconds
        bool CheckDeactivationTimeStamp()
        {
            DateTimeOffset lastDeactivated;

            if (settings.Contains("DeactivateTime"))
            {
                lastDeactivated = (DateTimeOffset)settings["DeactivateTime"];
            }

            var currentDuration = DateTimeOffset.Now.Subtract(lastDeactivated);

            return TimeSpan.FromSeconds(currentDuration.TotalSeconds) > TimeSpan.FromSeconds(30);
        }

        // Helper method to restore the session type from isolated storage.
        void RestoreSessionType()
        {
            if (settings.Contains("SessionType"))
            {
                sessionType = (SessionType)settings["SessionType"];
            }
        }
    }
}