using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SpotifyController
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// "Paused" label
        /// </summary>
        private const string PAUSED = "[ PAUSE ] ";

        /// <summary>
        /// Refresh interval configuration key
        /// </summary>
        private const string REFRESHINTERVALCONFIGKEY = "refreshIntervalMs";

        /// <summary>
        /// Local Spotify Controller instance
        /// </summary>
        private static SpotifyAPI.Local.SpotifyLocalAPI SpotifyController { get; set; } = new SpotifyAPI.Local.SpotifyLocalAPI();

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            ButtonsList.Visibility = Visibility.Collapsed;

            if (SpotifyController.Connect())
            {
                LaunchBackgroundTask();
            }
        }

        /// <summary>
        /// Launch the monitoring Background Task
        /// </summary>
        /// <returns></returns>
        public async Task LaunchBackgroundTask()
        {
            int refreshInterval;
            if(!int.TryParse(ConfigurationManager.AppSettings[REFRESHINTERVALCONFIGKEY], out refreshInterval))
            {
                refreshInterval = 250;
            }

            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var status = SpotifyController.GetStatus();

                    if (status != null && status.Track != null)
                    {
                        Application.Current?.Dispatcher.BeginInvoke((ThreadStart)delegate
                        {
                            string playingStatus = status.Playing ? string.Empty : PAUSED;
                            this.PlayButton.Visibility = status.Playing ? Visibility.Collapsed : Visibility.Visible;
                            this.PauseButton.Visibility = status.Playing ? Visibility.Visible : Visibility.Collapsed;
                            this.TrackProgress.Value = this.TrackProgress.Maximum = this.TrackProgress.Minimum = 0;

                            if (status.Track.ArtistResource != null && status.Track.TrackResource != null && status.Track.AlbumResource != null)
                            {
                                this.TrackProgress.Maximum = status.Track.Length;
                                this.TrackProgress.Value = status.PlayingPosition;

                                this.SpotifyWindowTitle.Text = $"{playingStatus}{status.Track.ArtistResource.Name} - {status.Track.TrackResource.Name}";
                                this.SpotifyWindowTitle_Tooltip.Content = $"{status.Track.ArtistResource.Name} - {status.Track.TrackResource.Name}\n{status.Track.AlbumResource.Name}";
                            }
                            else
                            {
                                if (status.Track.IsAd())
                                {
                                    this.SpotifyWindowTitle_Tooltip.Content = this.SpotifyWindowTitle.Text = $"{playingStatus}[ PUB ]";
                                }
                                else
                                {
                                    this.SpotifyWindowTitle_Tooltip.Content = this.SpotifyWindowTitle.Text = string.Empty;
                                }
                            }
                        });
                    }

                    // refresh time: 500ms
                    await Task.Delay(250);
                }
            }, TaskCreationOptions.LongRunning);
        }

        #region EVENTS

        /// <summary>
        /// OnRender Event
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.Top = 0D;
            this.Height = 1D;
        }

        /// <summary>
        /// Click on "Play/Pause" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PlayPause_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SpotifyController.GetStatus().Playing)
            {
                await SpotifyController.Pause();
            }
            else
            {
                await SpotifyController.Play();
            }
        }

        /// <summary>
        /// Click on "Previous" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            SpotifyController.Previous();
        }

        /// <summary>
        /// Click on "Next" Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Next_Button_Click(object sender, RoutedEventArgs e)
        {
            SpotifyController.Skip();
        }

        /// <summary>
        /// MouseEnter on window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            // expands the window
            var anim = new DoubleAnimation(60D, new Duration(new TimeSpan(0, 0, 0, 0, 200)));

            this.BeginAnimation(MinHeightProperty, anim);
            ButtonsList.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// MouseLeave on window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // collapse the window
            var anim = new DoubleAnimation(2D, new Duration(new TimeSpan(0, 0, 0, 0, 200)));

            this.MaxHeight = 1D;
            this.BeginAnimation(MinHeightProperty, anim);
            ButtonsList.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Click on "Close" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion EVENTS
    }
}
