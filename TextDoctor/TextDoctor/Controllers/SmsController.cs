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
        public string City;
        public string Number;
        public string CurrentState;
        public string LastResponse;
        public string LastAdviceType;
        public int CurrentAdviceIndex;
        public int LastTipIndex;
        public int LastHowDidWeDoIndex;
        public string MessageResponse;
    }

    public struct NarrativeElements
    {
        public static string GENERAL_PAIN       = "1";
        public static string GENERAL_FATIGUE    = "2";
        public static string HYSTERIA           = "3";
        public static string MALAISE            = "4";

        private void GetNextMessageForList(ref ClientState client, List<string> messages)
        {
            client.MessageResponse = "";
            if (client.CurrentAdviceIndex < 0 || client.CurrentAdviceIndex >= messages.Count())
            {
                client.CurrentAdviceIndex = 0;
            }
            if(client.CurrentAdviceIndex > 0)
            {
                client.MessageResponse = "Need more? ";
            }
            
            client.MessageResponse += GetIndexFromList(OkMessages, ref client.LastTipIndex) + " " + messages[client.CurrentAdviceIndex] + "\n\r" + GetIndexFromList(DidThisHelpMessages, ref client.LastHowDidWeDoIndex) + " " + RespondWith;
            client.CurrentAdviceIndex++;
        }

        private void YoureWelcomeClient(ref ClientState client)
        {
            client.MessageResponse = YayCongrats;
            client.CurrentAdviceIndex = 0;
            client.CurrentState = "";
        }

        private void GetNextContextMessage(ref ClientState client, string LastActionToEvaluate)
        {
            if (LastActionToEvaluate.ContainsCaseInsensitive(GENERAL_PAIN))
            {
                Debug.Write("Handling general pain message\n");
                GetNextMessageForList(ref client, GeneralPainMessages);
                client.CurrentState = "didhelp";
            }
            else if (LastActionToEvaluate.ContainsCaseInsensitive(GENERAL_FATIGUE))
            {
                Debug.Write("Handling general fatigue message\n");
                GetNextMessageForList(ref client, GeneralFatigueMessages);
                client.CurrentState = "didhelp";
            }
            else if (LastActionToEvaluate.ContainsCaseInsensitive(MALAISE))
            {
                Debug.Write("Handling malaise message\n");
                GetNextMessageForList(ref client, MailaiseMessages);
                client.CurrentState = "didhelp";
            }
            else if (LastActionToEvaluate.ContainsCaseInsensitive(HYSTERIA))
            {
                Debug.Write("Handling hysteria message\n");
                GetNextMessageForList(ref client, HysteriaMessages);
                client.CurrentState = "didhelp";
            }
            else
            {
                Debug.Write("Handling a confused client\n");
                client.MessageResponse = DontUnderstandCategoryResponse(client.LastResponse);
            }
        }

        public ClientState HandleClientState(ClientState client)
        {
            switch(client.CurrentState)
            {
                case "didhelp":                    
                    if (client.LastResponse.ContainsCaseInsensitive("yes"))
                    {
                        Debug.Write("Handling yes state\n");
                        YoureWelcomeClient(ref client);                   
                    }
                    else if (client.LastResponse.ContainsCaseInsensitive("no"))
                    {
                        Debug.Write("Handling no state\n");
                        GetNextContextMessage(ref client, client.LastAdviceType);
                    }
                    else if (client.LastResponse.ContainsCaseInsensitive("more"))
                    {
                        Debug.Write("Handling more state\n");
                        client.CurrentState = "choosing";
                        client.CurrentAdviceIndex = 0;
                        client.MessageResponse = WelcomeMessages[0] + " " + WelcomeMessages[1];
                    }
                    else
                    {
                        Debug.Write("Handling doesn't get yes or no questions\n");
                        client.MessageResponse = DontUnderstandYesNoResponse(client.LastResponse);
                    }
                    break;
                case "choosing":
                    Debug.Write("Handling choosing state\n");
                    GetNextContextMessage(ref client, client.LastResponse);
                    client.LastAdviceType = client.LastResponse;
                    break;
                default:
                    Debug.Write("Handling a new client\n");
                    client.CurrentState = "choosing";
                    client.CurrentAdviceIndex = 0;
                    client.MessageResponse = WelcomeMessages[0] + " " + WelcomeMessages[1];
                    break;
            }
            Debug.Write("Input: " + client.LastResponse + "\nOutput: " + client.MessageResponse + "\nCity: " + client.City + " Phone: " + client.Number + "\n");
            return client;
        }

        static List<string> WelcomeMessages = new List<string>
        {
            "Welcome to Chronic Illness Advice, a personalized messaging service!",
            "\nPlease respond with the type of symptom you need our helpful advice on: \n'1' - for General Pain \n'2' - for General Fatigue \n'3' - for Hysteria \n'4' - for Malaise.",
        };

        static string DontUnderstandCategoryResponse(string response)
        {
            return response + " isn't a supported choice. Please try again, your options are:'\n'1' - for General Pain \n'2' - for General Fatigue \n'3' - for Hysteria \n'4' - for Malaise.";
        }

        static string DontUnderstandYesNoResponse(string response)
        {
            return "I don't understand what you mean by " + response + ". Please try again, your options are 'Yes', or 'No'.";
        }

        static string GetIndexFromList(List<string> list, ref int index)
        {
            index++;
            if (index < 0 || index >= list.Count())
            {
                index = 0;
            }
            return list[index];
        }
       
        static List<string> OkMessages = new List<string>
        {
            "Easy. ",
            "No problem. ",
            "Here's a tip. ",
            "There's a fix for this! "
        };
        
        static List<string> DidThisHelpMessages = new List<string>
        {
            "Was this super helpful?",
            "Did we solve your problem?",
            "Is this something you would try?"
        };

        static string RespondWith = "(Respond with Yes, No, or More)";

        static string YayCongrats = "We're glad we were able to help you find a solution. Enjoy feeling better!";

        static List<string> AllStates = new List<string>
        {
            "choosing",
            "didhelp",
            GENERAL_PAIN,
            "fatigue",
            "pain",
            "brainfog"
        };

        static List<string> GeneralPainMessages = new List<string>
        {
            "Try excluding gluten, yeast, dairy, meat, fish, eggs, corn, soy, alcohol, caffeine, and sugar from your diet for pain relief.",
            "Add turmeric to your diet. Start with a sprinkle on your morning latte to start reducing inflammation immediately.",
            "Stand up straight! Chronic pain often comes from poor spine health.",
            "Thinking about your pain makes it feel worse. Try thinking about more pleasant things.",
            "Get moving! Even if you don't feel like it, strap on those running shoes and go for a jog. If exercise increases your pain, pay attention to your form. It may be you just aren't doing it right.",
            "Check that BMI - are you carrying excess weight? Try healthier food choices. If you aren't sure what to eat, ask a slim friend for tips!",
            "Have you tried acupuncture? This ancient treatment has stuck around for a reason!",
            "Try placing a purring cat on the areas of your body that hurt.",
            "Be sure to find the right specialist. You want someone who listens with empathy, answers all of your questions, is available when you need them, communicates regularly with your medical team, and accepts your insurance.",
            "Have you tried signing up for drug trials? Who knows - you might get lucky!"
        };

        static List<string> GeneralFatigueMessages = new List<string>
        {
            "Try fasting for a metabolic 'reset'.",
            "Try adding vitamin B6, vitamin D, iron, Ashwaganda, Ginseng, Peppermint, Ginger, and Green Algae to your daily routine.",
            "Dizziness or trouble standing probably just means low blood pressure. Try eating more salt. Swelling can be caused by fluid retention. Trying eating less salt.",
            "Chronic fatigue is directly tied to gut health. Try skipping the burgers and caffeine and add Kimchi and omega-3's to your diet.",
            "Learn to love exercise with a daily yoga practice. Fatigue and weight gain is often the result of a sedentary lifestyle. Loosening up is energizing for mind and body!",
            "Go to bed earlier. Poor sleep hygiene is the number one reason people feel tired and unmotivated during the day.",
            "Your fatigue is probably linked to depression. Try CBT for a mood boost.",
            "Make time to relax. If you can't take a vacation right now, find moments during the day for 'me time'.",
            "There are promising gene therapy studies being conducted in Switzerland. Have you tried that?",
            "Go outside! Fresh air and sunshine can be very invigorating. Side effect? A positive attitude!"
        };

        static List<string> MailaiseMessages = new List<string>
        {
            "If you haven't already, ditch the junk food, lose the booze, and quit smoking.",
            "Feeling sluggish? Everyone feels drained sometimes. Try a Vitamin D lamp for extra energy.",
            "Don't assume everything is part of an 'illness'. Symptoms like pain and fatigue are just part of being human!",
            "Get off the couch! Lethargy is a definite culprit when it comes to pain, fatigue, and depression. Start with yoga and mindfulness meditation.",
            "Everyone has brain fog - it's called getting older! Don't be so hard on yourself. It's just a natural part of life.",
            "Look for the positive. Can't work? Trouble keeping weight on? Remember, lots of people would love to have your 'problems'! Keep some perspective and look for the silver lining.",
            "Keep a detailed journal of your symptoms. It's possible you just haven't looked hard enough. Maybe the answer has been there the whole time!",
            "Just because there isn't a cure yet, be positive! New research is happening all the time.",
            "Try smiling more - your mind and body are sure to follow!",
            "Have you tried Cryotherapy? The cold really energizes all the cells.",
        };

        static List<string> HysteriaMessages = new List<string>
        {
            "If you have no appetite or struggle to keep weight on, try fatty foods and larger portion sizes. Who doesn't want to eat ice cream every day??",
            "Try massage to loosen, relax, and restore body and mind. Sometimes all you need is some time for yourself.",
            "You're probably just anemic. Try eating more spinach and red meat.",
            "Try mindfulness meditation. Dwelling on negative emotions can make your symptoms seem worse than they are.",
            "Get moving! Exercise is Mother Nature's healer. Try bike riding to boost your mood and stay toned.",
            "Try getting your hormone levels checked. It's possible you're just 'off'.",
            "Remember: joint pain is common. Take heart that you're not alone!",
            "If you have a mobility aid, ask yourself how much you really need it. Perhaps it's become a habit or you use it as a way to get attention. Be honest with yourself.",
            "Have you tried petting goats? They can be very therapeutic.",
            "Try Himalayan salt cave therapy. It's very relaxing and not just for rich people."
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
            
            try
            {
                if (!Services.AppCache.ContainsKey(incomingMessage.From))
                {
                    ClientState client = new ClientState();
                    client.City = incomingMessage.FromCity;
                    client.Number = incomingMessage.From;
                    Services.AppCache.Add(incomingMessage.From, client);
                }
                ClientState currentClient = Services.AppCache[incomingMessage.From];
                currentClient.LastResponse = incomingMessage.Body;
                Services.AppCache[incomingMessage.From] = currentClient;

                Services.AppCache[incomingMessage.From] = narrative.HandleClientState(Services.AppCache[incomingMessage.From]);

                messagingResponse.Message(Services.AppCache[incomingMessage.From].MessageResponse);

            }
            catch (System.ArgumentNullException ex)
            {
                messagingResponse.Message(ex.Message.ToString());
            }

            return TwiML(messagingResponse);
        }
    }
}