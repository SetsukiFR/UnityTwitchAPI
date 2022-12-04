using System;
using UnityEngine;

namespace VPTwitch
{
 
    public static class Chat
    {
        public enum AnnouncementColor { blue, green, orange, purple, primary }
        public const string READ_SCOPE = "chat:read";
        public const string EDIT_SCOPE = "chat:edit";
        public const string MAKE_ANNOUNCEMENTS_SCOPE = "moderator:manage:announcements";


        /// <summary>
        /// makes an announcement in the user's chat
        /// </summary>
        /// <param name="sText">over 500 characters will be truncated</param>
        public static void MakeAnnouncement(this Client client, string sText, AnnouncementColor eColor = AnnouncementColor.primary, Action<JSONObject> onCompleted = null)
        {
            JSONObject json = new JSONObject();
            json.AddField("message", sText);
            json.AddField("color", eColor.ToString());
            client.SendPostRequest("https://api.twitch.tv/helix/chat/announcements", json, onCompleted, ("broadcaster_id", client.broadcasterInfos.id), ("moderator_id", client.broadcasterInfos.id));
        }

        public static TwitchIRC CreateTwitchIRCListener(this Client client)
        {
            GameObject goTwitchIRC = new GameObject("Twitch IRC Listener");
            goTwitchIRC.hideFlags = HideFlags.DontSave;
            GameObject.DontDestroyOnLoad(goTwitchIRC);
            
            TwitchIRC twitchIRC = goTwitchIRC.AddComponent<TwitchIRC>();
            twitchIRC.twitchDetails = new TwitchIRC.TwitchDetails() { channel = client.broadcasterInfos.login, nick = client.broadcasterInfos.display_name, oauth = client.sOAuth };
            twitchIRC.settings = new TwitchIRC.Settings() { autoConnectOnStart = true, debugIRC = true, parseBadges = true, parseTwitchEmotes = true };
            return twitchIRC;
        }
    }
}