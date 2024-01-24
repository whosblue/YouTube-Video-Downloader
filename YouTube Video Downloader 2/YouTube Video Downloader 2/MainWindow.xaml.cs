using System.Diagnostics;
using System.IO.Packaging;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using NReco;
using NReco.Converting;

namespace YouTube_Video_Downloader_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool downloadAudioOnly = false;

        String RemoveIllegalCharacters(String dirtyString) // Remove illegal characters from video name
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                dirtyString = dirtyString.Replace(invalidChar.ToString(), "");
            }
            return dirtyString;
        }

        async void DownloadAudio(IStreamInfo streamInfo, YoutubeClient youtube, String title) // Download audio stream and convert to mp3
        {
            var stream = await youtube.Videos.Streams.GetAsync(streamInfo);

            // Create directories
            Directory.CreateDirectory("Downloads");
            Directory.CreateDirectory("Downloads\\Audio");

            label1.Content = "Downloading..."; // Update the user
            String filePath = $"Downloads\\Audio\\{RemoveIllegalCharacters(title)}";

            await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{filePath}.{streamInfo.Container}");
            label1.Content = "Converting to MP3...";

            // Convert downloaded stream to MP3 with NReco
            var convert = new NReco.VideoConverter.FFMpegConverter();
            convert.ConvertMedia($"{filePath}.{streamInfo.Container}", $"{filePath}.mp3", "mp3");

            // Delete unconverted stream
            if (File.Exists($"{filePath}.{streamInfo.Container}"))
            {
                File.Delete($"{filePath}.{streamInfo.Container}");
            }

            Process.Start("explorer.exe", $"Downloads\\Audio"); // Open the Windows File Explorer into the download directory.
            label1.Content = "Video completed. Opened File Explorer. Enjoy!";
        }

        async void DownloadVideo(IStreamInfo streamInfo, YoutubeClient youtube, String title) // Download YouTube Video
        {
            // Get the actual stream
            var stream = await youtube.Videos.Streams.GetAsync(streamInfo);
            label1.Content = "Downloading..."; // Update the user
            // Create directories
            Directory.CreateDirectory("Downloads");
            Directory.CreateDirectory("Downloads\\Videos");

            //Download the video
            String filePath = $"Downloads\\Videos\\{RemoveIllegalCharacters(title)}";
            await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{filePath}.{streamInfo.Container}");

            Process.Start("explorer.exe", $"Downloads\\Videos"); // Open the Windows File Explorer into the download directory.
            label1.Content = "Video completed. Opened File Explorer. Enjoy!";
        }

        async void DownloadYouTubeVideo(String youtubeVideoURL)
        {
            var youtube = new YoutubeClient();
            label1.Content = "Loading...";

            // Gather Metadata
            var video = await youtube.Videos.GetAsync(youtubeVideoURL);

            var title = video.Title;
            var author = video.Author.ChannelTitle;

            videoNameStat.Content = $"Title: {title}";
            creatorNameStat.Content = $"Creator: {author}";

            label1.Content = $"Detected video '{title}'";

            // Gather available streams (download qualities)
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(youtubeVideoURL);

            // Get and download the highest quality version, depending on what type of download the user wants
            if (downloadAudioOnly)
            {
                try
                {
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                    DownloadAudio(streamInfo, youtube, title);
                }
                catch
                {
                    label1.Content = "Unknown error occurred. Please be sure the link is correct, or contact developer.";
                }
            }
            else
            {
                try
                {
                    var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                    DownloadVideo(streamInfo, youtube, title);
                }
                catch
                {
                    label1.Content = "Unknown error occurred. Please be sure the link is correct, or contact developer.";
                }
            }
        }

        private void EnableDownloadAsVideo(object sender, RoutedEventArgs e) // Video download radio button
        {
            downloadAudioOnly = false;
        }

        private void EnableDownloadAsAudio(object sender, RoutedEventArgs e) // Audio download radio button
        {
            downloadAudioOnly = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DownloadYouTubeVideo(textBox1.Text);
        }

    }
}