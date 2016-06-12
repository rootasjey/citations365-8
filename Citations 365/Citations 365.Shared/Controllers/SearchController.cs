﻿using Citations_365.Models;
using System.Threading.Tasks;

namespace Citations_365.Controllers {
    public class SearchController
    {
        /*
         * ***********
         * VARIABLES
         * ***********
         */
        /// <summary>
        /// URL to perform the search
        /// </summary>
        private string _url = "http://evene.lefigaro.fr/citations/mot.php?mot=";

        /// <summary>
        /// For some requests, the url is re-written, so we save it to fetch next pages
        /// </summary>
        private string _redirectedURL = "";

        /// <summary>
        /// Pagination of the search result
        /// </summary>
        private static int _page = 1;

        private string _pageQuery = "&p=";

        /*
         * ************
         * COLLECTIONS
         * ************
         */
        private static SearchCollection _searchCollection { get; set; }
        public static SearchCollection SearchCollection {
            get {
                if (_searchCollection == null) {
                    _searchCollection = new SearchCollection();
                }
                return _searchCollection;
            }
        }

        /*
         * ***********
         * CONSTRUCTOR
         * ***********
         */
        /// <summary>
        /// Initialize the controller
        /// </summary>
        public SearchController() {

        }

        /*
         * ********
         * METHODS
         * ********
         */
        /// <summary>
        /// Populate authors collection
        /// </summary>
        /// <returns>True if data was successfully loaded</returns>
        public async Task<bool> LoadData() {
            return IsDataLoaded();
        }

        /// <summary>
        /// Delete old data and fetch new data
        /// </summary>
        public async Task<bool> Reload() {
            if (IsDataLoaded()) {
                SearchCollection.Clear();
            }
            return await LoadData();
        }

        private string URLBuilding(string query) {
            if (_redirectedURL.Length > 0) {
                if (_redirectedURL.Contains(_pageQuery)) {
                    _redirectedURL = _redirectedURL.Substring(0, _redirectedURL.IndexOf(_pageQuery));
                }
                return _redirectedURL + _pageQuery + _page;

            } else {
                return _url + query + _pageQuery + _page;
            }
        }

        public async Task<bool> Search(string query) {
            int found = await SearchCollection.BuildAndFetch(query);
            if (found >0) {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Return true if the data is already loaded
        /// </summary>
        /// <returns>True if data is already loaded</returns>
        public bool IsDataLoaded() {
            return SearchCollection.Count > 0;
        }

        /// <summary>
        /// Update the favorite icon of a quote
        /// </summary>
        /// <param name="key"></param>
        public static void SyncFavorites(string key) {
            if (SearchCollection.Contains(key)) {
                Quote quote = SearchCollection[key];
                quote.IsFavorite = FavoritesController.IsFavorite(key);
            }
        }
    }
}
