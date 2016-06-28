using Citations_365.Common;
using Citations_365.Controllers;
using System;
using Windows.ApplicationModel.Email;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Citations_365.Views {
    public sealed partial class SettingsPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private static SettingsController _SController;

        public static SettingsController Scontroller {
            get {
                if (_SController == null) {
                    _SController = new SettingsController();
                }
                return _SController;
            }
        }

        public SettingsPage() {
            InitializeComponent();

            navigationHelper = new NavigationHelper(this);
            navigationHelper.LoadState += NavigationHelper_LoadState;
            navigationHelper.SaveState += NavigationHelper_SaveState;
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
            navigationHelper.OnNavigatedTo(e);
            UpdateTaskSwitcher();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void UpdateTaskSwitcher() {
            TaskSwitch.IsOn = Scontroller.IsLiveTaskActivated();
        }

        private void TaskSwitch_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            var toggle = (ToggleSwitch)sender;

            if (toggle.IsOn) {
                Scontroller.RegisterBackgroundTask();
            } else {
                Scontroller.UnregisterBackgroundTask();
            }
        }

        private void FeedbackButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            EmailMessage email = new EmailMessage();
            email.Subject = "[Citations 365] Feedback";
            email.Body = "send this email to metrodevapp@outlook.com";
            // TODO : add app infos
            EmailManager.ShowComposeNewEmailAsync(email);
        }

        private async void NoteButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            string appID = "9wzdncrcwfqr";
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=" + appID));
        }

        private async void LockScreenButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e) {
            // Launch URI for the lock screen settings screen. 
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:lockscreen"));
        }
    }
}
