using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Citations365.ViewModels
{
    /// <summary>
    /// Objet oeuvre d'un auteur
    /// </summary>
    public class Work
    {
        /// <summary>
        /// URL de l'illustration de l'oeuvre
        /// </summary>
        public string imageUrl { get; set; }

        /// <summary>
        /// Titre de l'oeuvre
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// Catégorie de l'oeuvre
        /// </summary>
        public string category { get; set; }

        /// <summary>
        /// Editeur de l'oeuvre
        /// </summary>
        public string editor { get; set; }

        /// <summary>
        /// Date de publication de l'oeuvre
        /// </summary>
        public string publication { get; set; }

        /// <summary>
        /// Courte description de l'oeuvre
        /// </summary>
        public string resume { get; set; }

        /// <summary>
        /// URL de l'oeuvre détaillé
        /// </summary>
        public string link { get; set; }

        /// <summary>
        /// Liste des auteurs de l'oeuvre
        /// </summary>
        public string authors { get; set; }

        /// <summary>
        /// Liste des fiches (url) des auteurs
        /// </summary>
        public List<string> auhorsLink { get; set; }
    }
}
