﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HudlRT.Common
{
    public struct TeamContextResponse
    {
        public long? teamID { get; set; }
        public long? seasonID { get; set; }
    }

    public struct LastViewedResponse
    {
        public String name { get; set; }
        public String timeStamp { get; set; }
        public long? ID { get; set; }
    }

    class AppDataAccessor
    {
        public static string AUTH_TOKEN = "hudl-authtoken";
        public static string USER_ID = "userId";
        public static string LAST_VIEWED_NAME = "hudl-lastViewedCutupName";
        public static string LAST_VIEWED_TIMESTAMP = "hudl-lastViewedCutupTimestamp";
        public static string LAST_VIEWED_ID = "hudl-lastViewedCutupId";
        public static string TEAM_ID = "hudl-teamID";
        public static string SEASON_ID = "hudl-seasonID";
        public static string USERNAME = "UserName";
        public static string PLAYBACK = "hudl-playbackType";

        private static T GetRoamingSetting<T>(string keyName)
        {
            return (T)Windows.Storage.ApplicationData.Current.RoamingSettings.Values[keyName];
        }

        private static void SetRoamingSetting<T>(string keyName, T value)
        {
            Windows.Storage.ApplicationData.Current.RoamingSettings.Values[keyName] = value;
        }

        private static bool RoamingSettingExists(string keyName)
        {
            return Windows.Storage.ApplicationData.Current.RoamingSettings.Values[keyName] != null;
        }

        public static String GetAuthToken()
        {
            return (String)GetRoamingSetting<String>(AUTH_TOKEN);
        }

        public static void SetAuthToken(String token)
        {
            SetRoamingSetting<String>(AUTH_TOKEN, token);
        }

        public static String GetUsername()
        {
            return GetRoamingSetting<String>(USERNAME);
        }

        public static void SetUsername(String username)
        {
            SetRoamingSetting<String>(USERNAME, username);
        }

        public static bool TeamContextSet()
        {
            return RoamingSettingExists(SEASON_ID) && RoamingSettingExists(TEAM_ID);
        }

        public static TeamContextResponse GetTeamContext() {
            TeamContextResponse response = new TeamContextResponse();
            response.seasonID = GetRoamingSetting<long?>(SEASON_ID);
            response.teamID = GetRoamingSetting<long?>(TEAM_ID);
            return response;
        }

        public static void SetTeamContext(long seasonID, long teamID)
        {
            SetRoamingSetting<long?>(SEASON_ID, seasonID);
            SetRoamingSetting<long?>(TEAM_ID, teamID);
        }

        public static bool LastViewedSet()
        {
            bool response = true;
            response = RoamingSettingExists(USERNAME);
            string username = GetUsername();
            return RoamingSettingExists(username+LAST_VIEWED_NAME) && RoamingSettingExists(username+LAST_VIEWED_TIMESTAMP) && RoamingSettingExists(username+LAST_VIEWED_ID);
        }

        public static LastViewedResponse GetLastViewed()
        {
            string username = GetUsername();
            LastViewedResponse response = new LastViewedResponse();
            response.name = GetRoamingSetting<String>(username+LAST_VIEWED_NAME);
            response.timeStamp = GetRoamingSetting<String>(username+LAST_VIEWED_TIMESTAMP);
            response.ID = GetRoamingSetting<long?>(username+LAST_VIEWED_ID);
            return response;
        }

        public static void SetLastViewed(String name, String time, long ID)
        {
            string username = GetUsername();
            SetRoamingSetting<String>(username+LAST_VIEWED_NAME, name);
            SetRoamingSetting<String>(username + LAST_VIEWED_TIMESTAMP, time);
            SetRoamingSetting<long>(username + LAST_VIEWED_ID, ID);
        }

        public static bool PlaybackTypeSet()
        {
            return RoamingSettingExists(PLAYBACK);
        }

        public static int? GetPlaybackType()
        {
            return GetRoamingSetting<int?>(PLAYBACK);
        }

        public static void SetPlaybackType(int type)
        {
            SetRoamingSetting<int>(PLAYBACK, type);
        }
    }
}
