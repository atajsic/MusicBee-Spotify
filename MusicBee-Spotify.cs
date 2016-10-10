using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Collections.Generic;

using SpotifyAPI.Web.Models;


namespace MusicBeePlugin {
    public partial class Plugin {

        //declarations
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();
        private SpotifyController SpotifySession;
        private FullTrack SpotifyCurrentTrack;
        private bool inMyLibrary = false;
        private Control panel;
        private int panelHeight;

        //mbs init
        public PluginInfo Initialise(IntPtr apiInterfacePtr) {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "MusicBee-Spotify";
            about.Description = "This plugin allows users to add the song currently playing to their Spotify library.";
            about.Author = "Andrew Tajsic";
            about.TargetApplication = "Spotify";
            about.Type = PluginType.PanelView;
            about.VersionMajor = 1;
            about.VersionMinor = 0;
            about.Revision = 0;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 20;
            return about;
        }

        //mbs configuration
        public bool Configure(IntPtr panelHandle) {
            return false;
        }
       
        //mbs savesettings
        public void SaveSettings() {
        }

        //mbs disabled or mb shutting down
        public void Close(PluginCloseReason reason) {
        }

        // uninstall mbs
        public void Uninstall() {
        }

        // mbs notifications
        public void ReceiveNotification(string sourceFileUrl, NotificationType type) {
            switch (type) {
                case NotificationType.PluginStartup:
                    //start a new spotify session
                    SpotifySession = new SpotifyController();
                    break;
                case NotificationType.TrackChanged:
                    //build our search query
                    string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
                    string title = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
                    string q = (artist + " " + title);
                    SpotifyCurrentTrack = SpotifySession.Search(q);
                    //reset and check if current track is in spotify library
                    inMyLibrary = false;
                    if (SpotifyCurrentTrack != null) {
                        inMyLibrary = SpotifySession.CheckTracks(SpotifyCurrentTrack.Id);
                    }
                    //refresh panel
                    panel.Invalidate();
                    break;
            }
        }

        //mbs gui panel
        public int OnDockablePanelCreated(Control panel) {

            //display settings
            float dpiScaling = 0;
            using (Graphics g = panel.CreateGraphics()) {
                dpiScaling = g.DpiY / 96f;
            }

            //build ui
            panel.Paint += DrawPanel;
            panel.Click += PanelClick;

            //declare
            this.panel = panel;
            this.panelHeight = Convert.ToInt32(50 * dpiScaling);
            return this.panelHeight;
        }

        private void DrawPanel(object sender, PaintEventArgs e) {

            //default colours
            Color bg = panel.BackColor;
            Color text1 = panel.ForeColor; 
            Color text2 = text1;
            Color highlight = Color.FromArgb(2021216);

            //defaults
            e.Graphics.Clear(bg);

            //draw track
            if (SpotifyCurrentTrack != null) {

                //show album art
                string imageUrl = SpotifyCurrentTrack.Album.Images[0].Url;
                WebClient wc = new WebClient();
                byte[] bytes = wc.DownloadData(imageUrl);
                System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(bytes));
                image = new Bitmap(image, new Size(this.panelHeight, this.panelHeight));
                e.Graphics.DrawImage(image, new Point(0,0));

                //show title
                //if track in library, title is colour
                if (inMyLibrary) {
                    text1 = highlight;
                }
                TextRenderer.DrawText(e.Graphics, SpotifyCurrentTrack.Name, new Font(panel.Font.FontFamily, 10), new Point(this.panelHeight + 4, 6), text1);

                //show artist(s)
                string artists = string.Join(", ", from item in SpotifyCurrentTrack.Artists select item.Name);
                TextRenderer.DrawText(e.Graphics, artists, new Font(panel.Font.FontFamily, 9), new Point(this.panelHeight + 5, 25), text2);

                //Pointer!
                panel.Cursor = Cursors.Hand;
            }

            //no track playing, or no track found
            else {
                TextRenderer.DrawText(e.Graphics, "Track Not Found.", panel.Font, new Point(10, 10), text2);
            }
            
        }

        //msb drop down menu
        public List<ToolStripItem> GetHeaderMenuItems() {

            List<ToolStripItem> list = new List<ToolStripItem>();

            ToolStripMenuItem add = new ToolStripMenuItem("Add to 'My Library'");
            ToolStripMenuItem remove = new ToolStripMenuItem("Remove from 'My Library'");

            add.Click += addToLibrary;
            remove.Click += removeFromLibrary;

            list.Add(add);
            list.Add(remove);

            return list;
        }

        //msb add to spotify library
        private void addToLibrary(object sender, EventArgs e) {
            if (SpotifyCurrentTrack != null) {
                SpotifySession.addToLibrary(SpotifyCurrentTrack.Id);
                inMyLibrary = true;
                panel.Invalidate();
            } else {
                MessageBox.Show("Sorry, this track could not be found on Spotify...", about.Name);
            }
        }

        //msb remove from spotify library
        private void removeFromLibrary(object sender, EventArgs e) {
            if (SpotifyCurrentTrack != null) {
                SpotifySession.removeFromLibrary(SpotifyCurrentTrack.Id);
                inMyLibrary = false;
                panel.Invalidate();
            } else {
                    MessageBox.Show("Sorry, this track could not be found on Spotify...", about.Name);
            }
        }

        //msb panel click - open spotify to track
        private void PanelClick(object sender, EventArgs e) {
            if (SpotifyCurrentTrack != null) {
                System.Diagnostics.Process.Start(SpotifyCurrentTrack.Uri);
            }
        }
    }
}