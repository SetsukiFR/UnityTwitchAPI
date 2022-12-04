using System;
using System.Collections;
using UnityEngine;
using VPTwitch;

/*
* Thanks to Hellcat for TwitchOAuth
* Thanks to Matt Schoen for JSONObject
* Thanks to lexonegit for Unity-Twitch-Chat
*/

public class Example : MonoBehaviour
{
    private const string CLIENTID = "my application's client id when I created my application on dev.twitch.tv with URL as http://localhost:8080";
    private const string CLIENTSECRET = "my application's secret";

    //an instance of the prefab provided in lexonegit's Unity Twitch Chat library, with the "autostart" disabled
    public TwitchIRC twitchIRC;

    /// <summary>
    /// this coroutine is started with the gameObject
    /// </summary>
    IEnumerator Start()
    {
        Client client;

        //Get the saved client data if it exists
        string sClientSave = PlayerPrefs.GetString("twitch", null);

        if (string.IsNullOrWhiteSpace(sClientSave))
        {
            //we don't have any saved token, we need to ask twitch for one:

            // 1 - create the twitch client
            client = new Client(CLIENTID, CLIENTSECRET);

            // 2 - get an authentification token with a list of rights. you can add or remove as many as you'd want or need
            client.GetOAuth(
                Poll.READ_SCOPE,                //Authorization to read the polls 
                Poll.MANAGE_SCOPE,              //Authorization to create a poll
                Chat.MAKE_ANNOUNCEMENTS_SCOPE,  //Authorization to make an announcement in the chat
                Chat.READ_SCOPE);               //Authorization to read the chat


            //3 - wait for the token to arrive
            while (!client.bHasOAuth) yield return null;

            //4 - get the broadcaster's infos : name, login, channel...
            client.GetBroadcasterInfos();

            //5 - wait for the broadcaster's infos to arrive
            while (!client.bHasBroadcasterInfos) yield return null;

            //save the token so we don't ask for it next time
            PlayerPrefs.SetString("twitch", client.GetSaveString());
            PlayerPrefs.Save();
        }
        else //we HAVE a saved client
        {
            client = new Client("6f18ei5vns2von3klyajxkqvswgyo4", "7qsplxx2zn2kgdiruuplu2gkgyviq5", sClientSave, true);
        }

        ConnectToChat(client);


        //USAGE EXAMPLES :


        /* Make an announcement
         
        client.MakeAnnouncement("This is an announcement", Chat.AnnouncementColor.blue);
        */



        /* create and update regularly a poll
         
        if(client.CanCreatePoll())
        {
            //create a poll
            var poll = client.CreatePoll("Question?", 60, "Answer 1", "Answer 2");
            while (!poll.bStarted) yield return null;
            
            //update regularly
            while(poll.bOnGoing)
            {
                yield return new WaitForSecondsRealtime(5);
                poll.Update();
                while (poll.bHasOngoingRequest) yield return null;
            }

            //the poll is finished, get the final results
            poll.Update();
            while (poll.bHasOngoingRequest) yield return null;

            int answerAResult = poll.choices[0].nVotes;
            int answerBResult = poll.choices[1].nVotes;
        }
        */
    }

    private void ConnectToChat(Client client)
    {
        //we fill the twitchIRC's details to connect to this user's channel
        twitchIRC.twitchDetails.oauth = client.sOAuth;
        twitchIRC.twitchDetails.nick = client.broadcasterInfos.display_name;
        twitchIRC.twitchDetails.channel = client.broadcasterInfos.login;

        //attach a callback
        twitchIRC.newChatMessageEvent.AddListener(OnChatMessage);

        //Don't destroy the twitchIRC object
        GameObject.DontDestroyOnLoad(twitchIRC.gameObject);

        //Connect
        twitchIRC.IRC_Connect();
    }

    /// <summary>
    /// called when twitchIRC recieves a chat
    /// </summary>
    private void OnChatMessage(Chatter chatter)
    {
        Debug.Log($"{chatter.login}:{chatter.message}");
    }
}
