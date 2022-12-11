using System;
using System.Collections.Generic;
using UnityEngine;

namespace VPTwitch
{
	/// <summary>
	/// extensions for poll
	/// </summary>
	public static class PollExtension
	{
		/// <summary>
		/// launches a new poll on the client's user
		/// </summary>
		/// <param name="nDurationSeconds">must be between 15 and 1800</param>
		/// <param name="sTitle">must be below 60 characters</param>
		/// <param name="sChoices">less than 5 choices, each below 25 characters</param>
		public static Poll CreatePoll(this Client client, string sTitle, int nDurationSeconds, params string[] sChoices)
		{
			return new Poll(client, sTitle, nDurationSeconds, sChoices);
		}

		public static bool CanCreatePoll(this Client client)
        {
			return client.broadcasterInfos.broadcaster_type != "";
        }
	}

	public class Poll
	{
		/// <summary>
		/// scope to be able to read polls
		/// </summary>
		public const string READ_SCOPE = "channel:read:polls";
		/// <summary>
		/// scope to be able to create and end polls
		/// </summary>
		public const string MANAGE_SCOPE = "channel:manage:polls";

		/// <summary>
		/// an answer to the poll
		/// </summary>
		public class PollAnswer
        {
			public string sTitle { private set; get; }
			public string sId = null;
			public int nVotes;
			public PollAnswer(string sTitle) 
			{
				this.sTitle = sTitle;
			}

			/// <summary>
			/// returns true if the id matches and the votes count has been updated, false otherwise
			/// </summary>
			public bool Update(JSONObject json)
			{
				json.GetField(out string sID, "id", "");
				json.GetField(out string sTitle, "title", "");
				if (sID == sId || //if the ID matches 
					sId == null && this.sTitle == sTitle) //or if we don't have an id yet, if the titles match
				{
					sId = sID;
					json.GetField(out nVotes, "votes", nVotes);
					return true;
				}
				return false;
			}
        }

		public readonly string sTitle;
		public readonly PollAnswer[] choices;
		public readonly int nDuration;

		/// <summary>
		/// is the poll currently running ? estimated on time
		/// </summary>
		public bool bOnGoing => bStarted && _fStartTime + nDuration > Time.realtimeSinceStartup;

		/// <summary>
		/// is the poll started?
		/// </summary>
		public bool bStarted => _fStartTime > 0;

		/// <summary>
		/// is this object currently doing a webrequest?
		/// </summary>
		public bool bHasOngoingRequest { private set; get; } = false;

		/// <summary>
		/// Time.realtimeSinceStartup when the poll creation was answered, -1 if the creation isn't finished
		/// </summary>
		private float _fStartTime = -1;

		private string _sId;
		private Client _client;


		//callbacks used by the user
		private Action<Poll> _onUpdateFinished;
		private Action<Poll> _onPollManuallyEndedFinished;
		private JSONObject _lastError;
		/// <summary>
		/// creates a new poll
		/// </summary>
		public Poll(Client client, string sTitle, int nDurationSeconds, params string[] sChoices)
		{
			//check if all the variables are correct
			if (!client.bHasOAuth) throw new ArgumentException("client doesn't have oauth");
			if (!client.bHasBroadcasterInfos) throw new ArgumentException("client doesn't have user infos");
			if (nDurationSeconds < 15 || nDurationSeconds > 1800) throw new ArgumentException("nDurationSeconds must be between 15 and 1800 seconds");
			if (sTitle.Length > 60) throw new ArgumentException("sTitle must be below 60");
			if (sChoices.Length<2||sChoices.Length>5) throw new ArgumentException("there can be only 2 to 5 sChoices");

			//create the objects
			this.sTitle = sTitle;
			this.nDuration = nDurationSeconds;
			this.choices = new PollAnswer[sChoices.Length];
			_client = client;
            for (int i = 0; i < sChoices.Length; i++)
			{
				if (sChoices[i].Length > 25) throw new ArgumentException("choices must be below 25 characters");
				this.choices[i] = new PollAnswer(sChoices[i]);
            }

			//generate the json
			JSONObject json = new JSONObject();
			json.AddField("broadcaster_id", client.broadcasterInfos.id);
			json.AddField("title", sTitle);

			//create the array of choices
			JSONObject jsonChoices = new JSONObject(JSONObject.Type.ARRAY);
			foreach (var s in sChoices)
			{
				JSONObject jsonChoice = new JSONObject();
				jsonChoice.AddField("title", s);
				jsonChoices.Add(jsonChoice);
			}

			//add the array of choices to the main json
			json.AddField("choices", jsonChoices);
			
			//add the duration
			json.AddField("duration", nDurationSeconds);


			//send the request
			bHasOngoingRequest = true;
			client.SendPostRequest("https://api.twitch.tv/helix/polls",json, OnCreateCompleted, OnError);
		}

		public void OnError(JSONObject error)
		{
			_lastError = error;
			bHasOngoingRequest = false;
		}

		/// <summary>
		/// called when the "create" request is finished
		/// </summary>
		private void OnCreateCompleted(JSONObject result)
		{
			bHasOngoingRequest = false;
			ParsePoll(result);
			_fStartTime = Time.realtimeSinceStartup;
		}

		/// <summary>
		/// updates a poll ; note, the poll can be over and the function will still work
		/// </summary>
		/// <returns> true if the update request could be launched, false otherwise </returns>
		public bool Update(Action<Poll> onUpdateFinished = null, Action<JSONObject> onError = null)
		{
			if (bHasOngoingRequest || !bStarted) return false;
			onError += OnError;
			_onUpdateFinished = onUpdateFinished;
			bHasOngoingRequest = true;
			_client.SendGetRequest("https://api.twitch.tv/helix/polls", OnUpdateCompleted, OnError, ("broadcaster_id", _client.broadcasterInfos.id), ("id", _sId));
			return true;
		}

		/// <summary>
		/// manually ends a poll ; note : the results will be updated
		/// </summary>
		/// <returns> true if the end request could be launched, false otherwise </returns>
		public bool End(Action<Poll> onPollManuallyEndedFinished = null, Action<JSONObject> onError = null)
		{
			if (bHasOngoingRequest || !bStarted) return false;
			onError += OnError;
			_onPollManuallyEndedFinished = onPollManuallyEndedFinished;
			bHasOngoingRequest = true;
			_client.SendPatchRequest("https://api.twitch.tv/helix/polls", OnManualEndCompleted, onError,
				("broadcaster_id", _client.broadcasterInfos.id), 
				("id", _sId),
				("status", "TERMINATED"));
			return true;
		}

		/// <summary>
		/// called when an "end" request is finished
		/// </summary>
        private void OnManualEndCompleted(JSONObject result)
		{
			bHasOngoingRequest = false;
			ParsePoll(result);
			_onPollManuallyEndedFinished?.Invoke(this);
			_onPollManuallyEndedFinished = null;
			_fStartTime = float.PositiveInfinity; //it's finished ; starttime is infinite in order to be considered not ongoing
		}


		/// <summary>
		/// called when an "Update" request is finished
		/// </summary>
		private void OnUpdateCompleted(JSONObject result)
		{
			bHasOngoingRequest = false;
			ParsePoll(result);
			_onUpdateFinished?.Invoke(this);
			_onUpdateFinished = null;
		}

		/// <summary>
		/// parses a response from the server describing a poll
		/// </summary>
		private void ParsePoll(JSONObject result)
        {
            var data = result.GetField("data").list[0];
            data.GetField(out _sId, "id", _sId);

            JSONObject jsonChoices = data.GetField("choices");

            for (int i = 0; i < choices.Length; ++i)
            {
				for (int j = 0; j < jsonChoices.list.Count; ++j)
				{
					if(choices[i].Update(jsonChoices.list[j]))
                    {
						break;
                    }
				}
            }
        }
    }
}
