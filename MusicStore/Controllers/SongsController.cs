using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Data;
using MusicStore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    public class SongsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostingEnvironment _env;

        public SongsController(ApplicationDbContext context, IHostingEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Songs
        public async Task<IActionResult> Index()
        {
            return View(await _context.Songs.ToListAsync());
        }

        // GET: Songs/LoadSongs
        //  https://localhost:44366/songs/loadsongs?temp=88&chanceOfRain=.80&forecast=sunny&wind=11
        public IActionResult LoadSongs(int temp, float wind, float chanceOfRain, string forecast)
        {
            string directions = BePrepared(temp, wind, chanceOfRain, forecast);
            ViewBag.Directions = directions;
            return View();
        }

        private string BePrepared(int temp, float wind, float chanceOfRain, string forecast)
        {
            string directions = "";
            if (temp < 32) directions += "Put on a heavy coat\n";
            if (temp > 32 && temp < 40) directions += "Wear your mittens\n";
            if (temp >= 40 && temp < 70) directions += "Do you have a long sleeve shirt\n";
            if (temp > 70) directions += "A short sleeve shirt will be fine\n";
            if (temp > 110) directions += "Move out of Texas\n";
            if (wind < 10.8) directions += "Nothing extra\n";
            if (wind >= 10.8 && wind < 45.5) directions += "Put on a wind breaker\n";
            if (wind >= 45.5 && wind < 75.2) directions += "Stay in doors\n";
            if (wind >= 75.2) directions += "Get to the storm cellar\n";
            if (chanceOfRain < .10) directions += "Don't worry about it\n";
            if (chanceOfRain >= .10 && chanceOfRain < .35) directions += "Wear a hat\n";
            if (chanceOfRain > .35) directions += "Take an umbrella\n";
            if (chanceOfRain > .80) directions += "Stay home and read a book\n";
            switch (forecast)
            {
                case "sunny": directions += "Put on your sun glasses and put on sun screen\n"; break;
                case "windy": directions += "Wear a coat\n"; break;
                case "snowy": directions += "Wear you boots\n"; break;
            }
            return directions;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search2(string Search)
        {
            List<Song> allSongs = _context.Songs.ToList();
            List<Song> shortList = allSongs.FindAll(s => s.Album.ToUpper().Contains(Search.ToUpper())
                                                      || s.Title.ToUpper().Contains(Search.ToUpper())
                                                      || s.Artist.ToUpper().Contains(Search.ToUpper()));
            return View("ListSongs", shortList);
        }

        // POST: Songs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoadSongs(IFormFile file)
        {
            if (file != null)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = _env.WebRootPath + "\\uploads\\albums\\" + fileName;

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                System.IO.StreamReader sfile = new System.IO.StreamReader(path);
                string line;
                sfile.ReadLine();
                while ((line = sfile.ReadLine()) != null)
                {
                    string[] properties = line.Split(",");
                    Song qSong = new Song
                    {
                        Title = properties[0],
                        Artist = properties[1],
                        Album = properties[2],
                        ImagePath = "uploads/albums/" + properties[2] + ".jpg",
                        ReleaseDate = DateTime.Parse(properties[3]),
                        Genre = properties[4]
                    };
                    decimal price;

                    decimal.TryParse(properties[5], out price);
                    qSong.Price = price;
                    _context.Add(qSong);
                }
                await _context.SaveChangesAsync();

                sfile.Close();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Songs/Search?searchFor=The Eagles
        //  https://localhost:44366/songs/deleteall
        public IActionResult DeleteAll()
        {
            List<Song> allSongs = _context.Songs.ToList();
            allSongs.ForEach(s => _context.Songs.Remove(s));
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // GET: Songs/Search?searchFor=The Eagles
        //  https://localhost:44366/songs/GetList?searchFor=ABCDEF
        public IActionResult GetList(string searchFor)
        {
            List<Song> allSongs = _context.Songs.ToList();
            List<string> albums = new List<string>();

            foreach (var song in allSongs)
            {
                //  find all album titles that match searchFor
                if (song.Album.Contains(searchFor))
                {
                    albums.Add(song.Album);
                }
                //  add the song album name to the albums list
            }

            //  if a unique set of names is required convert from a List to a HashSet
            HashSet<string> unique = new HashSet<string>(albums);
            //albums = new List<string>(unique);
            //  make this list alphabetical
            albums.Sort();
            return View("ListAlbums", albums);
        }

        // GET: Songs/Search?searchFor=The Eagles
        //  https://localhost:44366/songs/search?searchfor=boys&field=artistll&searchfor2=rock&field2=genre
        public IActionResult Search(string searchFor1, string field1, string searchFor2, string field2)
        {
            if (searchFor1 == null)
            {
                return NotFound();
            }

            List<Song> songs;
            List<Song> allSongs = _context.Songs.ToList();
            songs = FilterSongs(allSongs, searchFor1, field1);
            songs = FilterSongs(songs, searchFor2, field2);

            //  how would you only return songs with a high ranking
            ViewBag.Artist = searchFor1;
            return View("ListSongs", songs);
        }

        private List<Song> FilterSongs(List<Song> someSongs, string searchFor, string field)
        {
            List<Song> songs;

            switch (field.ToLower())
            {
                case "genre": songs = someSongs.FindAll(s => s.Genre.ToUpper().Contains(searchFor.ToUpper())); break;
                case "artist": songs = someSongs.FindAll(s => s.Artist.ToUpper().Contains(searchFor.ToUpper())); break;
                case "album": songs = someSongs.FindAll(s => s.Album.ToUpper().Contains(searchFor.ToUpper())); break;
                case "date": songs = someSongs.FindAll(s => s.ReleaseDate.CompareTo(DateTime.Parse(searchFor)) >= 0); break;
                default: songs = someSongs; break;
            }
            return songs;
        }

        // GET: Songs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var song = await _context.Songs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (song == null)
            {
                return NotFound();
            }

            return View(song);
        }

        // GET: Songs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Songs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Artist,Album,ReleaseDate,Genre,ImagePath,Price,IsActive,IsFeatured")] Song song, IFormFile file)
        {
            if (file != null)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = _env.WebRootPath + "\\uploads\\albums\\" + fileName;

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                song.ImagePath = "uploads/albums/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(song);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(song);
        }

        // GET: Songs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var song = await _context.Songs.FindAsync(id);
            if (song == null)
            {
                return NotFound();
            }
            return View(song);
        }

        // POST: Songs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Artist,Album,ReleaseDate,Genre,ImagePath,Price,IsActive,IsFeatured")] Song song, IFormFile file)
        {
            if (file != null)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = _env.WebRootPath + "\\uploads\\albums\\" + fileName;

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                song.ImagePath = "uploads/albums/" + fileName;
            }

            if (id != song.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(song);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SongExists(song.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(song);
        }

        // GET: Songs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var song = await _context.Songs.FirstOrDefaultAsync(m => m.Id == id);
            if (song == null)
            {
                return NotFound();
            }

            return View(song);
        }

        // POST: Songs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SongExists(int id)
        {
            return _context.Songs.Any(e => e.Id == id);
        }
    }
}
