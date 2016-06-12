using Citations_365.Models;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Citations_365.Controllers {
    public class TodayController
    {
        /*
         * **********
         * VARIABLES
         * **********
         */        
        public static Quote _lastPosition;

        private static TodayCollection _todayCollection { get; set; }
        public static TodayCollection TodayCollection {
            get {
                if (_todayCollection==null) {
                    _todayCollection = new TodayCollection();
                }
                return _todayCollection;
            }
        }
        
        public TodayController() {
            TodayCollection.CollectionChanged += CollectionChanged;
        }

        /*
         * ********
         * METHODS
         * ********
         */
        public async Task<bool> LoadData() {
            await FavoritesController.Initialize();

            if (IsDataLoaded()) {
                return true;
            }

            int added = await TodayCollection.BuildAndFetch();
            if (added > 0) {
                return true;
            }
            return false;
        }
        
        public async Task<bool> Reload() {
            if (IsDataLoaded()) {
                TodayCollection.Clear();
            }
            return await LoadData();
        }
        
        public bool IsDataLoaded() {
            return TodayCollection.Count > 0;
        }
        
        /// <summary>
        /// Initialize the favorite quotes collection from the FavoritesController
        /// </summary>
        /// <returns></returns>
        public async Task<bool> InitalizeFavorites() {
            return await FavoritesController.Initialize();
        }

        /// <summary>
        /// Update the favorite icon of a quote
        /// </summary>
        /// <param name="key"></param>
        public static void SyncFavorites(string key) {
            if (TodayCollection.Contains(key)) {
                Quote quote = TodayCollection[key];
                quote.IsFavorite = FavoritesController.IsFavorite(key);
            }
        }
        
        /// <summary>
        /// Notify Collection changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null)
                foreach (Quote item in e.NewItems)
                    item.PropertyChanged += QuotePropertyChanged;

            if (e.OldItems != null)
                foreach (Quote item in e.OldItems)
                    item.PropertyChanged -= QuotePropertyChanged;
        }

        private void QuotePropertyChanged(object sender, PropertyChangedEventArgs e) {
            //if (e.PropertyName == "Content") {
            //}
        }

        /// <summary>
        /// Save the ListView position 
        /// to continue where the user left when he comes back on the page
        /// </summary>
        public void SavePosition() {

        }
    }
}
