using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VPTwitch
{
	/// <summary>
	/// tools to create webrequests that correspond to twitch's documentation
	/// </summary>
	public abstract class Request
	{
		/// <summary>
		/// called when the request is completed
		/// </summary>
		protected Action<JSONObject> _onCompleted;

		/// <summary>
		/// the request operation
		/// </summary>
		protected UnityWebRequestAsyncOperation _operation;
		
		/// <summary>
		/// use this to yield the request in the coroutine
		/// </summary>
		public UnityWebRequestAsyncOperation yield => _operation;

		/// <summary>
		/// accessor to the request, can be yielded
		/// </summary>
		public UnityWebRequestAsyncOperation operation => _operation;

		/// <summary>
		/// result of the request
		/// </summary>
		public string sResult => _operation.webRequest.downloadHandler.text;

		/// <summary>
		/// callback called when the operation is finished
		/// </summary>
		/// <param name="obj"></param>
		protected void OnCompleted(AsyncOperation _)
		{
			_onCompleted?.Invoke(JSONObject.Create(sResult));
			_onCompleted = null;
		}

		/// <summary>
		/// adds twitch's basic header.
		/// </summary>
		protected virtual void AddHeaders(UnityWebRequest webRequest, Client client, bool bIncludeContentType)
		{
			webRequest.SetRequestHeader("Authorization", $"Bearer {client.sOAuth}");
			webRequest.SetRequestHeader("Client-Id", $"{client.sClientID}");
			if(bIncludeContentType)
				webRequest.SetRequestHeader("Content-Type", "application/json");
		}

		protected string AddQueryParameters(string sBaseURL, (string, string)[] queryParameters)
        {
			if (queryParameters == null || queryParameters.Length == 0) return sBaseURL;
			//build the URL
			StringBuilder urlBuilder = new StringBuilder();
			urlBuilder.Append(sBaseURL);
			urlBuilder.Append("?");
			foreach ((string, string) param in queryParameters)
			{
				urlBuilder.Append(param.Item1);
				urlBuilder.Append("=");
				urlBuilder.Append(UnityWebRequest.EscapeURL(param.Item2));
				urlBuilder.Append("&");
			}
			return urlBuilder.ToString();
		}

	}

	/// <summary>
	/// a POST request
	/// </summary>
	public class PostRequest : Request
	{
		/// <summary>
		/// creates and starts a POST request
		/// </summary>
		public PostRequest(Client client, string sURL, JSONObject parameters, Action<JSONObject> onCompleted, params (string, string)[] queryParameters) : this(client, sURL, parameters.ToString(false), onCompleted, queryParameters) {}

		/// <summary>
		/// creates and starts a POST request
		/// </summary>
		public PostRequest(Client client, string sURL, string sContents, Action<JSONObject> onCompleted, params (string, string)[] queryParameters)
		{
			this._onCompleted = onCompleted;

			var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(sContents));
			sURL = AddQueryParameters(sURL, queryParameters);


			var request = new UnityWebRequest(sURL, "POST", new DownloadHandlerBuffer(), uploadHandler);


			AddHeaders(request, client, true);
			_operation = request.SendWebRequest();
			_operation.completed += OnCompleted;
		}
	}

	public abstract class SimpleRequest : Request
	{
		/// <summary>
		/// creates and sends a PATCH request
		/// </summary>
		/// <param name="queryParameters">array of tuple parameters added to the URL</param>
		public SimpleRequest(Client client, string sBaseURL, Action<JSONObject> onCompleted, string sType, params (string, string)[] queryParameters)
		{
			this._onCompleted = onCompleted;

			string sURL = AddQueryParameters(sBaseURL, queryParameters);

			//create the request
			var request = new UnityWebRequest(sURL, sType, new DownloadHandlerBuffer(), null);
			
			AddHeaders(request, client, false);
			
			//start it!
			_operation = request.SendWebRequest();
			_operation.completed += OnCompleted;
		}
	}

	/// <summary>
	/// a GET request
	/// </summary>
	public class GetRequest : SimpleRequest
	{
		/// <summary>
		/// creates and sends a GET request
		/// </summary>
		/// <param name="queryParameters">array of tuple parameters added to the URL</param>
		public GetRequest(Client client, string sBaseURL, Action<JSONObject> onCompleted, params (string sKey, string sValue)[] queryParameters) 
			: base (client, sBaseURL, onCompleted, "GET", queryParameters) {}
	}

	/// <summary>
	/// a PATCH request
	/// </summary>
	public class PatchRequest : SimpleRequest
	{
		/// <summary>
		/// creates and sends a PATCH request
		/// </summary>
		/// <param name="queryParameters">array of tuple parameters added to the URL</param>
		public PatchRequest(Client client, string sBaseURL, Action<JSONObject> onCompleted, params (string, string)[] queryParameters)
			: base(client, sBaseURL, onCompleted, "PATCH", queryParameters) {}
	}


	/// <summary>
	/// extension class to easily create new requests from a client
	/// </summary>
	public static class PostRequestExtension
	{
		public static PostRequest SendPostRequest(this Client client, string sUrl, JSONObject parameters, Action<JSONObject> onCompleted, params (string, string)[] queryParameters)
		{
			return new PostRequest(client, sUrl, parameters, onCompleted, queryParameters);
		}
		public static GetRequest SendGetRequest(this Client client, string sUrl, Action<JSONObject> onCompleted, params (string, string)[] queryParameters)
		{
			return new GetRequest(client, sUrl, onCompleted, queryParameters);
		}
		public static PatchRequest SendPatchRequest(this Client client, string sUrl, Action<JSONObject> onCompleted, params (string, string)[] queryParameters)
		{
			return new PatchRequest(client, sUrl, onCompleted, queryParameters);
		}
	}
}