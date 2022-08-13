using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;

using Twilio.AspNet.Common;
using Twilio.AspNet.Mvc;
using Twilio.TwiML;

namespace TextDoctor.Controllers
{

    public static class StringExtensions
    {
        public static bool ContainsCaseInsensitive(this string source, string substring)
        {
            return source?.IndexOf(substring, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }

    public struct ClientState
    {
        public string CurrentState;
        public string LastResponse;
        public string LastAdviceType;
        public int CurrentAdviceIndex;
        public string MessageResponse;
    }

    public struct NarrativeElements
    {
        public static string SWELLING = "Swelling";
        private void GetNextSwellingMessage(ref ClientState client)
        {
            if (client.CurrentAdviceIndex < 0 || client.CurrentAdviceIndex >= SwellingMessages.Count())
            {
                client.CurrentAdviceIndex = 0;
            }
            client.MessageResponse = SwellingMessages[client.CurrentAdviceIndex] + "\n\r" + DidThisHelp;
            client.LastAdviceType = SWELLING;
            client.CurrentAdviceIndex++;
        }

        private void YoureWelcomeClient(ref ClientState client)
        {
            client.MessageResponse = YayCongrats;
            client.CurrentAdviceIndex = 0;
            client.CurrentState = "";
        }

        public ClientState HandleClientState(ClientState client)
        {
            switch(client.CurrentState)
            {
                case "didhelp":
                    Debug.Write("Handling didhelp state\n\r");
                    if (client.LastResponse.ContainsCaseInsensitive("Y") || client.LastResponse.ContainsCaseInsensitive("yes"))
                    {
                        YoureWelcomeClient(ref client);                   
                    }
                    else if (client.LastResponse.ContainsCaseInsensitive("N") || client.LastResponse.ContainsCaseInsensitive("no"))
                    {
                        if(client.LastAdviceType == SWELLING)
                            GetNextSwellingMessage(ref client);
                        else
                            GetNextSwellingMessage(ref client);
                    }
                    else
                    {
                        Debug.Write("Handling doesn't get yes or no questions\n\r");
                        client.MessageResponse = DontUnderstandYesNoResponse(client.LastResponse);
                    }
                    break;
                case "choosing":
                    Debug.Write("Handling choosing state\n\r");
                    if (client.LastResponse.ContainsCaseInsensitive(SWELLING))
                    {
                        Debug.Write("Handling swelling message\n\r");
                        GetNextSwellingMessage(ref client);
                        client.CurrentState = "didhelp";
                    }
                    else
                    {
                        Debug.Write("Handling a confused client\n\r");
                        client.MessageResponse = DontUnderstandCategoryResponse(client.LastResponse);
                    }
                    break;
                default:
                    Debug.Write("Handling a new client\n\r");
                    client.CurrentState = "choosing";
                    client.CurrentAdviceIndex = 0;
                    client.MessageResponse = WelcomeMessages[0] + " " + WelcomeMessages[1];
                    break;
            }
            return client;
        }

        static List<string> WelcomeMessages = new List<string>
        {
            "Welcome to the Chronic Illness Advisor app!",
            "Please respond with the type of symptom you're suffering from: 'Swelling', 'Fatigue', 'Pain', or 'Brainfog'! ",
        };

        static string DontUnderstandCategoryResponse(string response)
        {
            return "I don't understand what you mean by " + response + ". Please try again, your options are 'Swelling', 'Fatigue', 'Pain', or 'Brainfog'.";
        }

        static string DontUnderstandYesNoResponse(string response)
        {
            return "I don't understand what you mean by " + response + ". Please try again, your options are 'Y', or 'N'.";
        }

        static string DidThisHelp = "Did this help solve the issue you're having? If not, respond with the letter N, otherwise respond with the letter Y.";

        static string YayCongrats = "We're glad we were able to help you answer those difficult to solve chronic illness questions! Goodbye!";

        static List<string> AllStates = new List<string>
        {
            "choosing",
            "didhelp",
            SWELLING,
            "fatigue",
            "pain",
            "brainfog"
        };

        static List<string> SwellingMessages = new List<string>
        {
            "swelling advice 1",
            "swelling advice 2",
            "swelling advice 3",
            "swelling advice 4"
        };

        static List<string> FatigueMessages = new List<string>
        {
            "fatigue advice 1",
            "fatigue advice 2",
            "fatigue advice 3",
            "fatigue advice 4"
        };

        static List<string> PainMessages = new List<string>
        {
            "pain advice 1",
            "pain advice 2",
            "pain advice 3",
            "pain advice 4"
        };

        static List<string> BrainfogMessages = new List<string>
        {
            "brainfog advice 1",
            "brainfog advice 2",
            "brainfog advice 3",
            "brainfog advice 4"
        };
    }

    public static class Services
    {
        private static Dictionary<string, ClientState> cache;
        private static object cacheLock = new object();

        public static Dictionary<string, ClientState> AppCache
        {
            get
            {
                lock (cacheLock)
                {
                    if (cache == null)
                    {
                        cache = new Dictionary<string, ClientState>();
                    }
                    return cache;
                }
            }
        }
    }

    public class SmsController : TwilioController
    {
        
        public TwiMLResult Index(SmsRequest incomingMessage)
        {
            //List<String> messages = new List<string>();
            //messages.Add("My sister's friend's brother's daughter's friend once did yoga and it helped her feel better. Have you tried that?");
            //messages.Add("Have you tried Advil?");
            //messages.Add("My sister's friend's brother's daughter's cousin went to India and learned to levitate and it healed him. Have you tried that?");

            var messagingResponse = new MessagingResponse();
            NarrativeElements narrative = new NarrativeElements();
            
            if (!Services.AppCache.ContainsKey(incomingMessage.From))
            {
                ClientState client = new ClientState();
                Services.AppCache.Add(incomingMessage.From, client);
            }

            ClientState currentClient = Services.AppCache[incomingMessage.From];
            currentClient.LastResponse = incomingMessage.Body;
            Services.AppCache[incomingMessage.From] = currentClient;

            Services.AppCache[incomingMessage.From] = narrative.HandleClientState(Services.AppCache[incomingMessage.From]);

            messagingResponse.Message(Services.AppCache[incomingMessage.From].MessageResponse);
            

            return TwiML(messagingResponse);
        }
    }
}