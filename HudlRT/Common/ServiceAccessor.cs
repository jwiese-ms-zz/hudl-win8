using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using Windows.Storage;
using HudlRT.Models;
using Caliburn.Micro;
using Windows.Networking.Connectivity;
using HudlRT.ViewModels;
using HudlRT.Parameters;

namespace HudlRT.Common
{
    class Response
    {
        public SERVICE_RESPONSE status { get; set; }
    }

    public enum SERVICE_RESPONSE { SUCCESS, NO_CONNECTION, NULL_RESPONSE, DESERIALIZATION, CREDENTIALS, PRIVILEGE };

    struct LoginSender
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    class InitResponse : Response
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    class LoginResponse: Response
    {
    }

    class TeamResponse: Response
    {
        public BindableCollection<Team> teams { get; set; }
    }

    class GameResponse: Response
    {
        public BindableCollection<Game> games { get; set; }
    }

    class CategoryResponse: Response
    {
        public BindableCollection<Category> categories { get; set; }
    }

    class PlaylistResponse: Response
    {
        public BindableCollection<Playlist> playlists { get; set; }
    }

    class ClipResponse: Response
    {
        public string[] DisplayColumns { get; set; }
        public BindableCollection<Clip> clips { get; set; }
    }

    public class NoInternetConnectionException : Exception
    {

    }

    public class GeneralInternetException : Exception
    {
    }
    /// <summary>
    /// Class used make API calls.
    /// </summary>
    class ServiceAccessor
    {
        public static async Task<InitResponse> Init()
        {
            InitResponse response = new InitResponse();
            response.status = SERVICE_RESPONSE.NO_CONNECTION;
    try
            {
                var fileName = "debug.config";
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.GetFileAsync(fileName);
                var readthis = await FileIO.ReadTextAsync(file);
                string[] data = readthis.Split('|');

                URL_BASE = data[0];
                URL_BASE_SECURE = data[1];

                response.status = SERVICE_RESPONSE.SUCCESS;
                response.Username = data[2];
                response.Password = data[3];
            }
            catch { }
            return response;
        }

        private static string URL_BASE = "http://www.hudl.com/api/v2/";
        private static string URL_BASE_SECURE = "https://www.hudl.com/api/v2/";

        public const string URL_SERVICE_LOGIN = "login";
        public const string URL_SERVICE_GET_TEAMS = "teams";
        public const string URL_SERVICE_GET_SCHEDULE_BY_SEASON = "teams/{0}/categories?seasonId={1}";//returns games
        //public const string URL_SERVICE_GET_SCHEDULE_BY_SEASON = "teams/{0}/schedule?season={1}";//returns games
        public const string URL_SERVICE_GET_CATEGORIES_FOR_GAME = "games/{0}/categories";//returns categories
        public const string URL_SERVICE_GET_CUTUPS_BY_CATEGORY = "categories/{0}/playlists";//returns playlists
        public const string URL_SERVICE_GET_CLIPS = "playlists/{0}/clips?startIndex={1}";//returns clips
        public const string URL_SERVICE_GET_OTHER_ITEMS = "teams/{0}/categories?seasonId={1}&type=7"; //note the seven at the end is hard coded so that this returns "Other Items"
        public const string NO_CONNECTION = "NoConnection";

        public static bool ConnectedToInternet()
        {
            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            return !(InternetConnectionProfile == null || InternetConnectionProfile.GetNetworkConnectivityLevel() == 0);
        }

        public static async Task<LoginResponse> Login(string loginArgs)
        {
            //var loginResponse = await ServiceAccessor.MakeApiCallGet("athlete");

            if (!ConnectedToInternet())
            {
                return new LoginResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            var loginResponse = await ServiceAccessor.MakeApiCallPost(ServiceAccessor.URL_SERVICE_LOGIN, loginArgs, false);
            if (!string.IsNullOrEmpty(loginResponse))
            {
                var obj = JsonConvert.DeserializeObject<LoginResponseDTO>(loginResponse);
                AppDataAccessor.SetAuthToken(obj.Token);
                string urlExtension = "users/" + obj.UserId.ToString() + "/privileges/";
#if DEBUG
#else
                var privilegesResponse = await ServiceAccessor.MakeApiCallGet(urlExtension, false);
                if (!string.IsNullOrEmpty(privilegesResponse))
                {
                    if (privilegesResponse.Contains("Win8App"))
                    {
                        return new LoginResponse { status = SERVICE_RESPONSE.SUCCESS };
                    }
                    else
                    {
                        return new LoginResponse { status = SERVICE_RESPONSE.PRIVILEGE };
                    }
                }
                else
                {
                    return new LoginResponse { status = SERVICE_RESPONSE.PRIVILEGE };
                }
#endif
                return new LoginResponse { status = SERVICE_RESPONSE.SUCCESS };
            }
            else
            {
                return new LoginResponse { status = SERVICE_RESPONSE.CREDENTIALS };
            }
        }

        public static async Task<TeamResponse> GetTeams()
        {
            var teams = await MakeApiCallGet(ServiceAccessor.URL_SERVICE_GET_TEAMS, true);
            if (!string.IsNullOrEmpty(teams) && teams != NO_CONNECTION)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<List<TeamDTO>>(teams);
                    BindableCollection<Team> teamCollection = new BindableCollection<Team>();
                    foreach (TeamDTO teamDTO in obj)
                    {
                        teamCollection.Add(Team.FromDTO(teamDTO));
                    }
                    return new TeamResponse { status = SERVICE_RESPONSE.SUCCESS, teams = teamCollection };
                }
                catch (Exception)
                {
                    return new TeamResponse { status = SERVICE_RESPONSE.DESERIALIZATION };
                }
            }
            else if (teams == NO_CONNECTION)
            {
                return new TeamResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            else
            {
                return new TeamResponse { status = SERVICE_RESPONSE.NULL_RESPONSE };
            }
        }

        public static async Task<GameResponse> GetGames(string teamId, string seasonId)
        {
            var games = await MakeApiCallGet(String.Format(ServiceAccessor.URL_SERVICE_GET_SCHEDULE_BY_SEASON, teamId, seasonId), true);
            if (!string.IsNullOrEmpty(games) && games != NO_CONNECTION)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<List<CategoryDTO>>(games);
                    BindableCollection<Game> gameCollection = new BindableCollection<Game>();
                    foreach (CategoryDTO gameDTO in obj)
                    {
                        gameCollection.Add(Game.FromDTO(gameDTO));
                    }
                    return new GameResponse { status = SERVICE_RESPONSE.SUCCESS, games = gameCollection };
                }
                catch (Exception)
                {
                    return new GameResponse { status = SERVICE_RESPONSE.DESERIALIZATION };
                }
            }
            else if (games == NO_CONNECTION)
            {
                return new GameResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            else
            {
                return new GameResponse { status = SERVICE_RESPONSE.NULL_RESPONSE };
            }
        }

        public static async Task<CategoryResponse> GetGameCategories(string gameId)
        {
            var categories = await MakeApiCallGet(String.Format(ServiceAccessor.URL_SERVICE_GET_CATEGORIES_FOR_GAME,gameId), true);
            if (!string.IsNullOrEmpty(categories) && categories != NO_CONNECTION)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<List<CategoryDTO>>(categories);
                    BindableCollection<Category> categoryCollection = new BindableCollection<Category>();
                    foreach (CategoryDTO categoryDTO in obj)
                    {
                        categoryCollection.Add(Category.FromDTO(categoryDTO));
                    }
                    return new CategoryResponse { status = SERVICE_RESPONSE.SUCCESS, categories = categoryCollection };
                }
                catch (Exception)
                {
                    return new CategoryResponse { status = SERVICE_RESPONSE.DESERIALIZATION };
                }
            }
            else if (categories == NO_CONNECTION)
            {
                return new CategoryResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            else
            {
                return new CategoryResponse { status = SERVICE_RESPONSE.NULL_RESPONSE };
            }
        }

        public static async Task<PlaylistResponse> GetCategoryPlaylists(string categoryId)
        {
            var playlists = await MakeApiCallGet(String.Format(ServiceAccessor.URL_SERVICE_GET_CUTUPS_BY_CATEGORY, categoryId), true);
            if (!string.IsNullOrEmpty(playlists) && playlists != NO_CONNECTION)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<List<PlaylistDTO>>(playlists);
                    BindableCollection<Playlist> playlistCollection = new BindableCollection<Playlist>();
                    foreach (PlaylistDTO playlistDTO in obj)
                    {
                        playlistCollection.Add(Playlist.FromDTO(playlistDTO));
                    }
                    return new PlaylistResponse { status = SERVICE_RESPONSE.SUCCESS, playlists = playlistCollection };
                }
                catch (Exception)
                {
                    return new PlaylistResponse { status = SERVICE_RESPONSE.DESERIALIZATION };
                }
            }
            else if (playlists == NO_CONNECTION)
            {
                return new PlaylistResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            else
            {
                return new PlaylistResponse { status = SERVICE_RESPONSE.NULL_RESPONSE };
            }
        }

        public static async Task<List<Clip>> GetAdditionalPlaylistClips(string playlistID, int startIndex)
        {
            var clips = await MakeApiCallGet(String.Format(ServiceAccessor.URL_SERVICE_GET_CLIPS, playlistID, startIndex.ToString()), false);
            if (clips != NO_CONNECTION)
            {
                var clipResponseDTO = JsonConvert.DeserializeObject<ClipResponseDTO>(clips);
                List<Clip> clipCollection = new List<Clip>();
                if (clipResponseDTO.ClipsList.Clips.Count == 100)
                {
                    foreach (ClipDTO clipDTO in clipResponseDTO.ClipsList.Clips)
                    {
                        Clip c = Clip.FromDTO(clipDTO, clipResponseDTO.DisplayColumns);
                        if (c != null)
                        {
                            clipCollection.Add(c);
                        }
                    }
                    var additionalClips = await GetAdditionalPlaylistClips(playlistID, startIndex + 100);
                    foreach (Clip c in additionalClips)
                    {
                        clipCollection.Add(c);
                    }
                    return clipCollection;
                }
                else
                {
                    foreach (ClipDTO clipDTO in clipResponseDTO.ClipsList.Clips)
                    {
                        Clip c = Clip.FromDTO(clipDTO, clipResponseDTO.DisplayColumns);
                        if (c != null)
                        {
                            clipCollection.Add(c);
                        }
                    }
                    return clipCollection;
                }
            }
            return new List<Clip>();
        }

        public static async Task<ClipResponse> GetPlaylistClipsAndHeaders(string playlistId)
        {
            var clips = await MakeApiCallGet(String.Format(ServiceAccessor.URL_SERVICE_GET_CLIPS, playlistId, "0"), true);
            if (!string.IsNullOrEmpty(clips) && clips != NO_CONNECTION)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<ClipResponseDTO>(clips);
                    BindableCollection<Clip> clipCollection = new BindableCollection<Clip>();
                    foreach (ClipDTO clipDTO in obj.ClipsList.Clips)
                    {
                        Clip c = Clip.FromDTO(clipDTO, obj.DisplayColumns);
                        if (c != null)
                        {
                            clipCollection.Add(c);
                        }
                    }
                    return new ClipResponse { status = SERVICE_RESPONSE.SUCCESS, clips = clipCollection, DisplayColumns = obj.DisplayColumns };
                }
                catch (Exception e)
                {
                    return new ClipResponse { status = SERVICE_RESPONSE.DESERIALIZATION };
                }
            }
            else if (clips == NO_CONNECTION)
            {
                return new ClipResponse { status = SERVICE_RESPONSE.NO_CONNECTION };
            }
            else
            {
                return new ClipResponse { status = SERVICE_RESPONSE.NULL_RESPONSE };
            }
        }

        /// <summary>
        /// Makes an API call to the base URL defined in AppData.cs using the GET method.
        /// </summary>
        /// <param name="url">The API function to hit.</param>
        /// <param name="jsonString">Any necesary data required to make the call.</param>
        /// <returns>The string response returned from the API call.</returns>
        public static async Task<string> MakeApiCallGet(string url, bool showDialog)
        {
            if (!ConnectedToInternet())
            {
                if (showDialog)
                {
                    APIExceptionDialog.ShowNoInternetConnectionDialog();
                }
                return NO_CONNECTION;
            }
            var httpClient = new HttpClient();
            Uri uri = new Uri(URL_BASE + url);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            httpRequestMessage.Headers.Add("hudl-authtoken", ApplicationData.Current.RoamingSettings.Values["hudl-authtoken"].ToString());
            httpRequestMessage.Headers.Add("User-Agent", "HudlWin8/1.0.0");
            var response = await httpClient.SendAsync(httpRequestMessage);
            if (showDialog)
            {
                if (!response.IsSuccessStatusCode)
                {
                    APIExceptionDialog.ShowStatusCodeExceptionDialog(response.StatusCode.ToString(), uri.ToString());
                    return null;
                }
            }

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Makes an API call to the base URL defined in AppData.cs using the POST method.
        /// </summary>
        /// <param name="url">The API function to hit.</param>
        /// <param name="jsonString">Any necesary data required to make the call.</param>
        /// <returns>The string response returned from the API call.</returns>
        public static async Task<string> MakeApiCallPost(string url, string jsonString, bool showDialog)
        {
            var httpClient = new HttpClient();
            Uri uri = new Uri(URL_BASE_SECURE + url);
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequestMessage.Headers.Add("User-Agent", "HudlWin8/1.0.0");
            httpRequestMessage.Content = new StringContent(jsonString);
            httpRequestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await httpClient.SendAsync(httpRequestMessage);
            //response.StatusCode 404 500 401
            if(!response.IsSuccessStatusCode)
            {
                APIExceptionDialog.ShowStatusCodeExceptionDialog(response.StatusCode.ToString(), uri.ToString());
                return null;
            }
            return await response.Content.ReadAsStringAsync();
        }
    }
}
