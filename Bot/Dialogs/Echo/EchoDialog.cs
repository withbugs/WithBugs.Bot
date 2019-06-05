using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WithBugs.Bot.Builder;

namespace Bot.Dialogs.Echo
{
    public class EchoDialog : ComponentDialog
    {
        private class DialogOptions
        {
            public bool IsInLoop { get; set; }
        }

        public EchoDialog(UserState userState, ConversationState conversationState)
            : base(nameof(EchoDialog))
        {
            InitialDialogId = nameof(EchoDialog);

            var steps = new WaterfallStep[]
            {
                step1Async,
                step2Async,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> step1Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var prompt = $"I can repeat what you say and type {Bot.INTERRUPTION_COMMAND_QUIT} whenever you want to exit this skill.";
            if (sc.Options != null &&
                sc.Options is DialogOptions options &&
                options.IsInLoop)
            {
                prompt = String.Empty;
            }

            return await sc.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions
                {
                    
                    Prompt = MessageFactoryEx.Text(prompt),
                });
        }

        private async Task<DialogTurnResult> step2Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var value = (sc.Result as String);

            await sc.Context.SendActivityAsync(value);
            return await sc.ReplaceDialogAsync(nameof(EchoDialog), new DialogOptions { IsInLoop = true });
        }
    }
}
