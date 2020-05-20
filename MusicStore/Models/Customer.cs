using System.ComponentModel.DataAnnotations;

namespace MusicStore.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Display(Name = "Customer Name")]
        public string Name { get; set; }
        public int Age { get; set; }

        [Display(Name = "Favorite Genre")]
        public string FavoriteGenre { get; set; }
        public string FavoriteSong { get; set; }
        public string Gender { get; set; }

        [Display(Name = "eMail Address")]
        public string email { get; set; }
        public string Image { get; set; }
    }
}
