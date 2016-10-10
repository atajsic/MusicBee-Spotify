using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;

using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace MusicBeePlugin {
    class SpotifyController {

        private static SpotifyWebAPI _spotify;

        public SpotifyController() {
            _spotify = new SpotifyWebAPI();
            InitSpotify();
        }

        static async void InitSpotify() {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                 "http://localhost",
                 8000,
                 "da0e615bea86424fa9d34face59f2a18",
                 Scope.UserLibraryModify,
                 TimeSpan.FromSeconds(60)
            );

            try {
                _spotify = await webApiFactory.GetWebApi();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }

            if (_spotify == null)
                return;
        }

        public FullTrack Search(string q) {
            q = WebUtility.UrlEncode(q);
            q = q.Replace("%20", "+");
            System.Diagnostics.Debug.WriteLine(q);
            SearchItem search = _spotify.SearchItems(q, SearchType.Track, 1);
            if(search.Error != null) {
                //token expired, re init
                if (search.Error.Status == 401) {
                    InitSpotify();
                    search = _spotify.SearchItems(q, SearchType.Track, 1);
                    if (search.Error != null) {
                        return null;
                    }
                } else {
                    return null;
                }
            }
            if (search.Tracks.Total != 0) {
                return search.Tracks.Items[0];
            }
            return null;
        }

        public Boolean CheckTracks(string id) {
            ListResponse<bool> tracksSaved = _spotify.CheckSavedTracks(new List<String> { id });
            if (tracksSaved.List[0])
                return true;
            return false;
        }

        public void addToLibrary(string id) {
            ErrorResponse r = _spotify.SaveTrack(id);
        }

        public void removeFromLibrary(string id) {
            ErrorResponse r = _spotify.RemoveSavedTracks(new List<string> { id });
        }
    }
}
