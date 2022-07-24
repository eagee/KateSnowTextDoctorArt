using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Twilio.AspNet.Common;
using Twilio.AspNet.Mvc;
using Twilio.TwiML;

namespace TextDoctor.Controllers
{
    public static class Services
    {
        private static Dictionary<string, int> cache;
        private static object cacheLock = new object();
        public static Dictionary<string, int> AppCache
        {
            get
            {
                lock (cacheLock)
                {
                    if (cache == null)
                    {
                        cache = new Dictionary<string, int>();
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
            List<String> messages = new List<string>();
            messages.Add("My sister's friend's brother's daughter's friend once did yoga and it helped her feel better. Have you tried that?");
            messages.Add("Have you tried Advil?");
            messages.Add("My sister's friend's brother's daughter's cousin went to India and learned to levitate and it healed him. Have you tried that?");
            var messagingResponse = new MessagingResponse();
            Random r = new Random();

            

            if (!Services.AppCache.ContainsKey(incomingMessage.From)) Services.AppCache.Add(incomingMessage.From, 0);
            Services.AppCache[incomingMessage.From]++;

            messagingResponse.Message(messages[r.Next() % 3] + " Response#: " + Services.AppCache[incomingMessage.From].ToString());

            return TwiML(messagingResponse);
        }
    }
}