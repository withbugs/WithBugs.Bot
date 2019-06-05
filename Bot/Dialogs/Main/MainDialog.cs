using Bot.Dialogs.CarRecognition;
using Bot.Dialogs.Echo;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Dialogs.Main
{
    public class MainDialog : ComponentDialog
    {
        private const string SKILL_ECHO = "Echo";
        private const string SKILL_CAR_RECOGNITION = "Car Recognition";

        private readonly string[] _skills = new string[] { SKILL_ECHO, SKILL_CAR_RECOGNITION };

        public class DialogOptions
        {
            public bool IsRepeater { get; set; }
        }

        public MainDialog(UserState userState, ConversationState conversationState)
            : base(nameof(MainDialog))
        {
            InitialDialogId = nameof(MainDialog);

            var steps = new WaterfallStep[]
            {
                step1Async,
                step2Async,
                step3Async,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
        }

        private async Task<DialogTurnResult> step1Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // TODO: In web chat control, this method is called twice at the begining of a conversation. In emulator, works fine as expected.
            var prompt = $"Welcome to WithBugs' Bot!";
            if (sc.Options != null && 
                sc.Options is DialogOptions options && 
                options.IsRepeater)
            {
                prompt = $"Hi again.";
            }

            prompt += $" Please select a skill from the choices below or type {Bot.INTERRUPTION_COMMAND_QUIT} whenever you want to end dialog.";

            return await sc.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(prompt),
                    Choices = ChoiceFactory.ToChoices(_skills),
                });
        }

        private async Task<DialogTurnResult> step2Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var value = (sc.Result as FoundChoice)?.Value;

            switch (value)
            {
                case SKILL_ECHO:
                    return await sc.BeginDialogAsync(nameof(EchoDialog));

                case SKILL_CAR_RECOGNITION:
                    return await sc.BeginDialogAsync(nameof(CarRecognitionDialog));

                default:                    
                    break;
            }

            return await sc.NextAsync();
        }

        private async Task<DialogTurnResult> step3Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync();
        }
    }
}
