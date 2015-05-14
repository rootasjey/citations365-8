using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;
using System.Windows.Media.Animation;
using Citations365.ViewModels;

namespace Citations365.Pages
{
    public partial class Settings : PhoneApplicationPage
    {
        // VAR


        public Settings()
        {
            InitializeComponent();

            // Active le ToggleSwitch pour l'Agent et la lecture audio si on trouve un agent actif
            var periodicTask = ScheduledActionService.Find("Citations365TaskAgent") as PeriodicTask;
            if (periodicTask != null)
            {
                //TSBAgent.IsChecked = periodicTask != null;
                TSBAgent.IsChecked = true;
            }
            TSBSpeech.IsChecked = App.ViewModel._TTSIsActivated;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

        /// <summary>
        /// Navigue vers la pages des paramètres de l'écran de verrouillage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void GoToBackgroundSettings(object sender, RoutedEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }
        
        /// <summary>
        /// Lorsque l'utilisateur souhaite 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LockScreenButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string type = App.ViewModel._ApplicationBackgroundType;

            if (type == "Cosmos" || type == "Madame" || type == "Flickr")
            {
                //if (App.ViewModel._CurrentBackgroundLink != "")
                //    LockHelper(App.ViewModel._CurrentBackgroundLink, false);
                //await DownloadImageFromURL();
                MainViewModel.eventHandlerDownloadImagefromServer -= RequestEndedDownloadedBackground;
                MainViewModel.eventHandlerDownloadImagefromServer += RequestEndedDownloadedBackground;
                App.ViewModel.DownloadImagefromServer(App.ViewModel._CurrentBackgroundLink);
            }
            else
            {
                if (App.ViewModel._CurrentBackgroundLink != "")
                    LockHelper(App.ViewModel._CurrentBackgroundLink, true);
            }
        }

        /// <summary>
        /// L'image est maintenant enregistrée dans l'IO
        /// (voir la méthode DownloadImagefromServer dans le MainViewModel.cs)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RequestEndedDownloadedBackground(object sender, EventArgs e)
        {
            // call function to set downloaded image as lock screen 
            LockHelper("DownloadedWallpaper.jpg", false);
        }

       
        /// <summary>
        /// Change l'image l'écran de verrouillage
        /// </summary>
        /// <param name="filePathOfTheImage"></param>
        /// <param name="isAppResource"></param>
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
                    MessageBox.Show("You said no, so I can't update your background.");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        // ToggleSwitch pour le Background Agent (checked)
        private void TSBAgent_Checked(object sender, RoutedEventArgs e)
        {
            // Crée le BackGround Agent
            string name = "Citations365TaskAgent";
            var periodicTask = ScheduledActionService.Find(name) as PeriodicTask;
            if (periodicTask != null)
            {
                // S'il y avait déjà un Agent en Tâche de Fond, on le supprime pour en recrée un nouveau
                ScheduledActionService.Remove(name);
            }
            else
            {
                // Sinon, on affiche un MessageBox
                // Affiche un MessageBox d'information
                string information = "A partir de maintenant, vous obtiendrez, chaque jour, une nouvelle citation sur la tuile de l'application (si vous l'avez épinglée sur l'écran d'accueil) et sur l'écran de verrouillage.\n";
                information += "Si votre télépohone se met en mode consomation réduite (un coeur sur la batterie), la tuile de l'application et l'écran de verrouillage ne se mettront pas à jour.\n";
                information += "En raison de la limitation du système, certaines citations trop longues ne s'afficheront pas en entier sur la tuile et l'écran de verrouillage.";
                MessageBoxResult message = MessageBox.Show(information);
            }

            periodicTask = new PeriodicTask(name)
            {
                Description = "Change automatiquement de citation sur la tuile principale et sur l'écran de verrouillage.",
                ExpirationTime = DateTime.Now.AddDays(10),
            };

           

            // Peut lever une exception si on est sur un mobile 256mb
            // ou si le nombre d'users agents est atteint.
            // D'où le Try Catch
            try
            {
                ScheduledActionService.Add(periodicTask);
                if (Debugger.IsAttached)
                {
                    ScheduledActionService.LaunchForTest(name, TimeSpan.FromSeconds(30));
                }
                // Modification du textblock
                TBAgentOnOff.Text = "ON";

                // Met à jour la Tuile Principale
                App.ViewModel.UpdateMainTile();
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: L'action est désactivée"))
                {
                    MessageBox.Show("L'agent en tâche de fond pour cette application a été désactivée par l'utilisateur");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }
            }
            catch (SchedulerServiceException)
            {
                // No user action required.
            }

        }

        // ToggleSwitch pour le Background Agent (unchecked)
        private void TSBAgent_Unchecked(object sender, RoutedEventArgs e)
        {
            // Supprimer le BackGround Agent
            string name = "Citations365TaskAgent";
            var periodicTask = ScheduledActionService.Find(name) as PeriodicTask;
            if (periodicTask != null)
                ScheduledActionService.Remove(name);

            TBAgentOnOff.Text = "OFF";
        }

        // ToggleSwitch pour le Text-To-Speech (checked)
        private void TSBSpeech_Checked(object sender, RoutedEventArgs e)
        {
            TBSpeechOnOff.Text = "ON";
            App.ViewModel._TTSIsActivated = true;
            App.ViewModel.SaveData();
        }

        // ToggleSwitch pour le Text-To-Speech (unchecked)
        private void TSBSpeech_Unchecked(object sender, RoutedEventArgs e)
        {
            TBSpeechOnOff.Text = "OFF";
            App.ViewModel._TTSIsActivated = false;
            App.ViewModel.SaveData();
        }


        private void RTBChangelog_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RTBChangelog.Height == 38)
            {
                RTBChangelog.Height = Double.NaN;
                //RTBChangelogSB1.Begin();
                //RTBChangelogSB1.Completed += delegate
                //{
                //    RTBChangelog.Height = 250;
                //};
            }
            else
            {
                RTBChangelog.Height = 38;
                //RTBChangelogSB2.Begin();
                //RTBChangelogSB2.Completed += delegate
                //{
                //    RTBChangelog.Height = 38;
                //};
            }
        }

        private void RTBLibrairies_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RTBLibrairies.Height == 30)
            {
                RTBLibrairieSB1.Begin();
                //RTBLibrairieSB1.Completed += delegate
                //{
                //    RTBLibrairies.Height = 150;
                //};
            }
            else
            {
                RTBLibrairieSB2.Begin();
                //RTBLibrairieSB2.Completed += delegate
                //{
                //    RTBLibrairies.Height = 30;
                //};
            }
        }


        private void email_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask email = new EmailComposeTask();
            email.Subject = "Citations365";
            email.To = "metroappdev@outlook.com";
            email.Body = "";
            email.Show();
        }

        private void GoToMarket_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();

            marketplaceDetailTask.ContentIdentifier = "2896fa7c-cc90-4288-8016-43d0eb4855e5";
            marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;

            marketplaceDetailTask.Show();

        }


        private void PanelDynamic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PanelDynamic.Height != 50.0)
            {
                closeSection_tuile.Begin();
                Plus_tuileSB2.Begin();

                // PanelDynamic.Height = 50.0;
            }
            else
            {
                openSection_tuile.Begin();
                Plus_tuileSB1.Begin();
                // PanelDynamic.Height = Double.NaN;
            }
        }

        private void PanelBackground_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PanelBackground.Height != 50.0)
            {
                closeSection_bg.Begin();
                Plus_bgSB2.Begin();
                // PanelBackground.Height = 50.0;
                PanelStaticBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
                PanelDynamicBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                openSection_bg.Begin();
                Plus_bgSB1.Begin();
                // PanelBackground.Height = Double.NaN;
            }
        }


        private void StaticBackground_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel panel = sender as StackPanel;
            if(panel != null)
            {
                App.ViewModel._ApplicationBackgroundType = panel.Name;
                App.ViewModel.SaveData();
                App.ViewModel._BackgroundChanged = true;
            }

            if (PanelBackground.Height != 50.0)
            {
                closeSection_bg.Begin();
                Plus_bgSB2.Begin();

                PanelStaticBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
                PanelDynamicBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void AppChallengeImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // L'URI
            var uriToLaunch = "http://appchallenge.azurewebsites.net/redirect?app=cbc03a81-a25b-419c-8cd0-7d7c8ff33531";

            // Crée un objet URI à partir de la chaîne de caractères
            Uri uri = new Uri(uriToLaunch, UriKind.Absolute);

            // Lance l'URI
            Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void StaticBakcgroundsPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PanelStaticBackgrounds.Visibility == System.Windows.Visibility.Collapsed)
            {
                PanelBackground.Height = double.NaN;
                //PanelStaticBackgrounds.Height = double.NaN;
                //PanelStaticBackgrounds.Visibility = System.Windows.Visibility.Visible;
                Anime_PanelStaticBackgrounds_In.Begin();
            }
            else
            {
                //PanelStaticBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
                Anime_PanelStaticBackgrounds_Out.Begin();
            }
        }

        private void DynamicBackgroundsPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (PanelDynamicBackgrounds.Visibility == System.Windows.Visibility.Visible)
            {
                //PanelDynamicBackgrounds.Visibility = System.Windows.Visibility.Collapsed;
                Anime_PanelDynamicBackgrounds_Out.Begin();
            }
            else
            {
                PanelBackground.Height = double.NaN;
                //PanelDynamicBackgrounds.Visibility = System.Windows.Visibility.Visible;
                Anime_PanelDynamicBackgrounds_In.Begin();
            }
        }
    }
}