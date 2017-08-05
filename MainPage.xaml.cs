using AlbumCoverMatchGame.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AlbumCoverMatchGame
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Song> Songs;
        private ObservableCollection<StorageFile> Allsongs;
        bool _playingMusic = false;
        int _round = 0;
        int _totalscore = 0;
        public MainPage()
        {
            this.InitializeComponent();

            Songs = new ObservableCollection<Song>();
        }
        //Get access to music librabry method
        private async Task RetrieveFilesInFolders(ObservableCollection<StorageFile> list, StorageFolder parent)
        {
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".mp3")
                    list.Add(item);

            }
            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveFilesInFolders(list, item);
            }
          
        }
        // 2. choose random songs from librabry helper method
        private async Task<List<StorageFile>> pickRandomSongs(ObservableCollection<StorageFile> allSongs)
        {

            Random random = new Random();
            var songCount = allSongs.Count; 
            var randomSongs = new List<StorageFile>();

            while (randomSongs.Count < 13)
            {
                var randomNumber = random.Next(songCount);
                var randomSong = allSongs[randomNumber];
                // I want two things to not happen before adding the random song to a LIST.:
                //1. Don't pick same song twice!
                //2.Don't pick a song from an album I've already picked!
               MusicProperties randomSongMusicProperties = 
                    await randomSong.Properties.
                    GetMusicPropertiesAsync();
                bool isDuplicate = false;
                foreach (var song in randomSongs)
                {
                    MusicProperties songMusicProperties = await song.Properties.GetMusicPropertiesAsync();
                    if (string.IsNullOrEmpty(randomSongMusicProperties.Album)||
                        randomSongMusicProperties.Album == songMusicProperties.Album)
                         isDuplicate = true;
                    
                }
                if (!isDuplicate)
                  randomSongs.Add(randomSong);
            }

            

            return randomSongs;
        }

      // 3.Pluck off meta data from selected songs HELPER method
      private async Task PopulateSongList(List<StorageFile> files)
        {
            int id = 0;
            foreach (var file in files)
            {
             MusicProperties songProperties =  await file.Properties.GetMusicPropertiesAsync();
                StorageItemThumbnail currentThumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 200, ThumbnailOptions.UseCurrentScale);

                var albumCover = new BitmapImage();
                albumCover.SetSource(currentThumb);

                var song = new Song(); // Create a new Song Object from the Class Song.
                song.Id = id;
                song.Title = songProperties.Title;
                song.Artist = songProperties.Artist;
                song.Album = songProperties.Album;
                song.AlbumCover = albumCover;
                song.SongFile = file;

                Songs.Add(song); // Then add the song to the instance of our observableCollection of Song Class
                id++;
            }
        }

        private async void  SongGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Evaluate the user selection
            // user should able to select
            if (_playingMusic == true)
            {
                var clickedSong = (Song)e.ClickedItem;
                var correctsong = Songs.FirstOrDefault(p => p.Selected == true);
                Countdown.Pause();
                MyMediaElement.Stop();
                // if song is selected 
                Uri uri;
                int score;
               
                if (clickedSong.Selected)
                {


                    uri = new Uri("ms-appx:///Assets/correct.png");
                    score = (int)MyProgressBar.Value;
                   

                }
                else
                {
                    uri = new Uri("ms-appx:///Assets/incorrect.png");
                    score = (int)MyProgressBar.Value * -1;
                    
                }
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                var filestream = await file.OpenAsync(FileAccessMode.Read);
                await clickedSong.AlbumCover.SetSourceAsync(filestream);
                _totalscore += score;
                _round++;
                ResultTextBlock.Text = string.Format("score:{0}, TotalScore after {1} Rounds: {2}",  score , _round , _totalscore);
                TitleTextBlock.Text = string.Format("The Tittle is: {0}", correctsong.Title);
                ArtistTextBlock.Text = string.Format("Perfomed by: {0}", correctsong.Artist);
                AlbumTextBlock.Text = string.Format("On Album: {0}", correctsong.Album);

                clickedSong.Used = true;
                correctsong.Selected = false;
                correctsong.Used = true;
                if (_round >= 5)
                {
                    InstructionTextBlock.Text = string.Format("Hey! Game over... Your Totalscore:{0}", _totalscore);
                    PlayAgainButton.Visibility = Visibility.Visible;
                }
                else
                {
                    StartCooldown();
                }
                
            }
           
        }

        private async void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            await PrepareNewGame();
            
            PlayAgainButton.Visibility = Visibility.Collapsed;

        }

     

        //Helper method for the Grid_Loaded
        private async Task<ObservableCollection<StorageFile>> setupMusicList()
        {
            // 1. Get access to music library

            StorageFolder folder = KnownFolders.MusicLibrary;
            var allSongs = new ObservableCollection<StorageFile>();
            await RetrieveFilesInFolders(allSongs, folder);
            return allSongs;
        }

        private async Task PrepareNewGame()
        {
            Songs.Clear();
            // 2. choose random songs from librabry
            var randomsongs = await pickRandomSongs(Allsongs);

            // 3.Pluck off meta data from selected songs
            await PopulateSongList(randomsongs);

            StartCooldown();
            //State Management
            InstructionTextBlock.Text = "Get ready ...";
            ResultTextBlock.Text = "";
            TitleTextBlock.Text = "";
            ArtistTextBlock.Text = "";
            AlbumTextBlock.Text = "";

            _totalscore = 0;
            _round = 0;
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            startupProgresssRing.IsActive = true;

            Allsongs = await setupMusicList();
            await PrepareNewGame();

            startupProgresssRing.IsActive = false;

            StartCooldown();
        }
        private void StartCooldown()
        {
            _playingMusic = false;
            SolidColorBrush brush = new SolidColorBrush(Colors.Blue);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = string.Format("Get ready for round {0} ....", _round + 1);
            InstructionTextBlock.Foreground = brush;
            Countdown.Begin();

        }
        private void StartCountdown()
        {
            _playingMusic = true;
            SolidColorBrush brush = new SolidColorBrush(Colors.Red);
            MyProgressBar.Foreground = brush;
            InstructionTextBlock.Text = "GO";
            InstructionTextBlock.Foreground = brush;
            Countdown.Begin();
           
        }
        private async void Countdown_Completed(object sender, object e)
        {
            if (!_playingMusic)
            {
                // Start playing some music
                var song = pickSongs();
                MyMediaElement.SetSource(await song.SongFile.OpenAsync(FileAccessMode.Read), song.SongFile.ContentType);
                // Start countdown 
                StartCountdown();
            }

        }
        private Song pickSongs()
        {
            Random random = new Random();
            var unusedSongs = Songs.Where(p => p.Used == false);
            var count = unusedSongs.Count();
            var RandomNumber = random.Next(count);
            var RandomSongs = unusedSongs.ElementAt(RandomNumber);
            RandomSongs.Selected = true;
            return RandomSongs;
        }
    }
}
