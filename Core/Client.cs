using System;

/*
* Thanks to Hellcat for TwitchOAuth
* Thanks to Matt Schoen for JSONObject
* Thanks to lexonegit for Unity-Twitch-Chat
*/

namespace VPTwitch
{

	/// <summary>
	/// a twitch client, containing the infos required to do requests
	/// </summary>
	public class Client
	{
		/// <summary>
		/// the clientID of your application
		/// </summary>
		public readonly string sClientID;

		/// <summary>
		/// the clientSecret of your application
		/// </summary>
		public readonly string sClientSecret;

		/// <summary>
		/// the OAuth key of your user. Protect it with your life.
		/// </summary>
		public string sOAuth => _sOAuth;

		private string _sOAuth;

		/// <summary>
		/// user informations of the client
		/// </summary>
		public User broadcasterInfos { private set; get; } = null;


		/// <summary>
		/// do we have the user's infos? if not, use <see cref="GetBroadcasterInfos"/>
		/// </summary>
		public bool bHasBroadcasterInfos => broadcasterInfos != null;

		/// <summary>
		/// does the user have an OAuth key? If this is false, EVERYTHING will be rejected
		/// </summary>
		public bool bHasOAuth => sOAuth != null;

		/// <summary>
		/// use this constructor if you don't have an OAuth. it is recommended to IMMEDIATELY request an OAuth using <see cref="GetOAuth"/>
		/// </summary>
		/// <param name="sClientID">your application's clientID</param>
		/// <param name="sClientSecret">your application's client secret</param>
		public Client(string sClientID, string sClientSecret)
		{
			this.sClientID = sClientID;
			this.sClientSecret = sClientSecret;
		}

		/// <summary>
		/// use this constructor if you already have an OAuth
		/// </summary>
		/// <param name="sClientID">your application's clientID</param>
		/// <param name="sClientSecret">your application's client secret</param>
		/// <param name="sOAuth">the oauth of the client. If the client hasn't authentified, use the constructor without it</param>
		public Client(string sClientID, string sClientSecret, string sOAuth) : this(sClientID, sClientSecret)
		{
			this._sOAuth = sOAuth;
		}

		/// <summary>
		/// if the broadcaster's infos (name, channel...) aren't available, this will update/get them.
		/// Note : broadcaster infos are required to do action on the user's channel
		/// Note : requires an oauth key
		/// </summary>
		public void GetBroadcasterInfos()
		{
			broadcasterInfos = null;
			User.GetClientUserInfos(this, OnBroadcasterInfosGet);
		}

        private void OnBroadcasterInfosGet(User myself)
        {
			broadcasterInfos = myself;
        }

		/// <summary>
		/// gets an OAuth for the user. REQUIRED TO FUNCTION.
		/// Special thanks to HELLCAT for this
		/// </summary>
		/// <param name="scopes">list of the scopes the oauth asks for. These can be found as public const parameters. For example, Poll.READ_SCOPE to be able to read polls.</param>
		public void GetOAuth(params string[] scopes)
		{
			new TwitchOAuthGetter(sClientID, sClientSecret, OnOAuthTokenRecieved, scopes);
		}

		/// <summary>
		/// returns a string that can be used to rebuild this client. NOTE : User's secret informations such as oauth key are in this NON-ENCRYPTED string.
		/// </summary>
		public string GetSaveString()
        {
			JSONObject json = new JSONObject();
			json.AddField("soauth", sOAuth);
			json.AddField("uinfo", broadcasterInfos.ToJSON());
			return json.ToString(false);
		}

		/// <summary>
		/// use this constructor to build a client from a save, obtained by using client.GetSaveString()
		/// </summary>
		public Client(string sClientID, string sClientSecret, string sClientSave, bool bLoadFromSave) : this(sClientID, sClientSecret)
		{
			JSONObject jsonObject = JSONObject.Create(sClientSave);
			jsonObject.GetField(out _sOAuth, "soauth", sOAuth);

			broadcasterInfos = new User();
			
			broadcasterInfos.ParseJSON(jsonObject.GetField("uinfo"));
		}

		/// <summary>
		/// this is called back by TheHellCat's code with the OAuth2 token.
		/// </summary>
		private void OnOAuthTokenRecieved(ApiCodeTokenResponse response)
		{
			_sOAuth = response.access_token;
		}
	}

    [System.Serializable]
	public class User
	{
		public string id;
		public string login;
		public string display_name;
		public string broadcaster_type;

		/// <summary>
		/// gets the client's infos
		/// </summary>
		public static void GetClientUserInfos(Client client, Action<User> onUserGet)
		{
			client.SendGetRequest("https://api.twitch.tv/helix/users", (JSONObject json)=>
            {
				User user = new User();
				user.ParseJSON(json);
				onUserGet(user);
            });
		}

        public void ParseJSON(JSONObject obj)
		{
			var data = obj.GetField("data").list[0];
			data.GetField(out id, "id", id);
			data.GetField(out login, "login", login);
			data.GetField(out display_name, "display_name", display_name);
			data.GetField(out broadcaster_type, "broadcaster_type", "");
		}

		public JSONObject ToJSON()
		{
			//user json
			JSONObject json = new JSONObject();
			JSONObject userData = new JSONObject(JSONObject.Type.ARRAY);
			json.AddField("data", userData);

			JSONObject user = new JSONObject();
			user.AddField("id", id);
			user.AddField("login", login);
			user.AddField("display_name", display_name);
			user.AddField("broadcaster_type", broadcaster_type);

			userData.Add(user);

			return json;
		}
	}
}