using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace WithBugs.Bot.Builder
{
    public static class MessageFactoryEx
    {
        public static Activity Text(string text, string ssml = null, string inputHint = null)
        {
            // In Web Chat control, MessageFactory.Text(String.Empty) displays empty bot's speech bubble.
            // To avoid that, return null explicitly.
            return (text == String.Empty) ? null : MessageFactory.Text(text, ssml, inputHint);
        }
    }
}
