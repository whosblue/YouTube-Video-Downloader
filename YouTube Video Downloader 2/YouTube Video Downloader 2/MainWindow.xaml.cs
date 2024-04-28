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
        // Illegal characters are characters not allowed by the file system. For instance, you can't name a file with the character / in it.
        String RemoveIllegalCharacters(string dirtyString)
        {
            var invalidCharSet = new HashSet<char>(Path.GetInvalidFileNameChars());
            var cleanedString = new StringBuilder();

            foreach (char c in dirtyString)
            {
                if (!invalidCharSet.Contains(c))
                {
                    cleanedString.Append(c);
                }
            }

            return cleanedString.ToString();
        }

        async void DownloadAudio(IStreamInfo streamInfo, YoutubeClient youtube, String title) // Download audio stream and convert to .mp3
        {
            var stream = await youtube.Videos.Streams.GetAsync(streamInfo);

            // Create directories
            Directory.CreateDirectory("Downloads");
            Directory.CreateDirectory("Downloads\\Audio");

            label1.Content = "Downloading..."; // Update the user
            String filePath = $"Downloads\\Audio\\{RemoveIllegalCharacters(title)}"; // We remove illegal characters otherwise the program will crash.

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
            // Create directories if they don't already exist
            Directory.CreateDirectory("Downloads");
            Directory.CreateDirectory("Downloads\\Videos");

            //Download the video
            String filePath = $"Downloads\\Videos\\{RemoveIllegalCharacters(title)}"; // We remove illegal characters otherwise the program will crash.
            await youtube.Videos.Streams.DownloadAsync(streamInfo, $"{filePath}.{streamInfo.Container}");

            Process.Start("explorer.exe", $"Downloads\\Videos"); // Open the Windows File Explorer into the download directory.
            label1.Content = "Video completed. Opened File Explorer. Enjoy!";
        }

        bool userHasSelectedDownloadOption = false;
        async void DownloadYouTubeVideo(String youtubeVideoURL) // Determines how we should download the video, given the user settings. Also grabs & updates metadata.
        {
            var youtube = new YoutubeClient();
            label1.Content = "Loading...";

            if (!userHasSelectedDownloadOption)
            {
                label1.Content = "You didn't select a download mode! Select one and press the button again.";
                return;
            }

            if (youtubeVideoURL == "")
            {
                label1.Content = "You didn't input a URL! Try again.";
                return;
            }

            var title = "";
            // Gather Metadata
            try
            {
                var video = await youtube.Videos.GetAsync(youtubeVideoURL);

                title = video.Title;
                var author = video.Author.ChannelTitle;

                videoNameStat.Content = $"Title: {title}";
                creatorNameStat.Content = $"Creator: {author}";
                label1.Content = $"Detected video '{title}'";
            }
            catch(Exception e)
            {
                label1.Content = "An error occurred. You must have input an invald YouTube link. Try again!";
                return;
            }

            // Gather available streams (download qualities)
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(youtubeVideoURL);

            // Get and download the highest quality version, depending on what type of download the user wants
            if (downloadAudioOnly) // If the user wants an .mp3...
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
            else if (!downloadAudioOnly)// If the user wants an .mp4...
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
            userHasSelectedDownloadOption = true;
        }

        private void EnableDownloadAsAudio(object sender, RoutedEventArgs e) // Audio download radio button
        {
            downloadAudioOnly = true;
            userHasSelectedDownloadOption = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DownloadYouTubeVideo(textBox1.Text);
        }

    }
}
