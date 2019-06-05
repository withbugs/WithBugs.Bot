// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.ConfigOptions;
using Bot.Dialogs.CarRecognition;
using Bot.Dialogs.Echo;
using Bot.Dialogs.Main;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;

namespace Bot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class Bot : ActivityHandler
    {
        public const string INTERRUPTION_COMMAND_QUIT = ":q";

        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private DialogSet _dialogSet;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="userState">Bot user state.</param>
        /// <param name="conversationState">Bot conversation state.</param>
        /// <param name="customVisionOptionsAccessor">Configuration for Custom Vision.</param>
        public Bot(UserState userState
                 , ConversationState conversationState
                 , IOptionsMonitor<CustomVisionOptions> customVisionOptionsAccessor)
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _dialogSet = new DialogSet(_conversationState.CreateProperty<DialogState>(nameof(Bot)));
            _dialogSet.Add(new MainDialog(_userState, _conversationState));
            _dialogSet.Add(new EchoDialog(_userState, _conversationState));
            _dialogSet.Add(new CarRecognitionDialog(_userState, _conversationState, customVisionOptionsAccessor));
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var dc = await _dialogSet.CreateContextAsync(turnContext);
            await dc.BeginDialogAsync(nameof(MainDialog));

            // save any state changes made to your state objects.
            await _conversationState.SaveChangesAsync(turnContext);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var dc = await _dialogSet.CreateContextAsync(turnContext);

            // at first, handling global interruption
            var text = turnContext.Activity?.Text ?? String.Empty;
            if (text.Equals(INTERRUPTION_COMMAND_QUIT, StringComparison.InvariantCultureIgnoreCase))
            {
                await dc.CancelAllDialogsAsync();
                await turnContext.SendActivityAsync($"See ya!");
                await turnContext.SendActivityAsync($"Say anything to continue.");
            }
            else
            {
                // process conversation when interruption command is not found.
                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                    switch (result.Status)
                    {
                        case DialogTurnStatus.Cancelled:
                        case DialogTurnStatus.Empty:
                            await turnContext.SendActivityAsync($"Not yet implemented.");
                            break;

                        case DialogTurnStatus.Complete:
                            await turnContext.SendActivityAsync($"Say anything to continue.");
                            break;

                        case DialogTurnStatus.Waiting:
                            break;
                    }
                }
                else
                {
                    await dc.BeginDialogAsync(nameof(MainDialog), new MainDialog.DialogOptions { IsRepeater = true });
                }
            }

            // save any state changes made to your state objects.
            await _conversationState.SaveChangesAsync(turnContext);
        }

        protected override async Task OnUnrecognizedActivityTypeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync($"{turnContext.Activity.Type} activity detected");
        }
    }
}
