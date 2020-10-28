using Bot.ConfigOptions;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WithBugs.Bot.Builder;

namespace Bot.Dialogs.CarRecognition
{
    public class CarRecognitionDialog : ComponentDialog
    {
        private const string KEY_IMAGE_BYTES = "KEY_IMAGE_BYTES";

        private class DialogOptions
        {
            public bool IsInLoop { get; set; }
        }

        public CustomVisionOptions CustomVisionOptions { get; set; }

        public CarRecognitionDialog(UserState userState
                                  , ConversationState conversationState
                                  , IOptionsMonitor<CustomVisionOptions> customVisionOptionsAccessor)
            : base(nameof(CarRecognitionDialog))
        {
            CustomVisionOptions = customVisionOptionsAccessor.CurrentValue;

            InitialDialogId = nameof(CarRecognitionDialog);

            var steps = new WaterfallStep[]
            {
                step1Async,
                step2Async,
                step3Async,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt), ValidateAttachmentAsync));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> step1Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var prompt = $"I can recognize car model. Could you upload a photo of a car? Type {Bot.INTERRUPTION_COMMAND_QUIT} whenever you want to exit this skill.";
            if (sc.Options != null &&
                sc.Options is DialogOptions options &&
                options.IsInLoop)
            {
                prompt = String.Empty;
            }

            var retryPrompt = $"Let's retry or type {Bot.INTERRUPTION_COMMAND_QUIT} to exit.";

            return await sc.PromptAsync(
                nameof(AttachmentPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactoryEx.Text(prompt),
                    RetryPrompt = MessageFactoryEx.Text(retryPrompt),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> step2Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var attachmentList = (sc.Result as IList<Attachment>);

            async Task<byte[]> readFileAsync()
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(attachmentList[0].ContentUrl))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return await response.Content.ReadAsByteArrayAsync();
                        }
                    }
                }
                return null;
            }

            var result = default(ImagePrediction);
            var imageBytes = await readFileAsync();

            using (var imageData = new MemoryStream(imageBytes))
            {
                var predictionClient = new CustomVisionPredictionClient()
                {
                    ApiKey = CustomVisionOptions.PredictionKey,
                    Endpoint = CustomVisionOptions.RegionEndpoint
                };

                result = await predictionClient.ClassifyImageAsync(new Guid(CustomVisionOptions.ProjectId), CustomVisionOptions.PublishedName, imageData);
            }

            var tagName = String.Empty;

            foreach (var item in result.Predictions)
            {
                if (item.Probability < CustomVisionOptions.ProbabilityThreshold)
                {
                    continue;
                }

                tagName = item.TagName;
                break;
            }

            if (String.IsNullOrEmpty(tagName))
            {
                sc.Values[KEY_IMAGE_BYTES] = imageBytes;

                return await sc.PromptAsync(
                    nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text($"I'm not sure what it is. Could you tell me the car model?"),
                    });
            }
            else
            {
                await sc.Context.SendActivityAsync($"Wow! Nice {tagName}!");
                return await sc.ReplaceDialogAsync(nameof(CarRecognitionDialog), new DialogOptions { IsInLoop = true });
            }
        }

        private async Task<DialogTurnResult> step3Async(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var tagName = (sc.Result as String);
            var imageBytes = sc.Values[KEY_IMAGE_BYTES] as byte[];

            await sc.Context.SendActivityAsync($"Thank you! I'll keep learning to be able to distingish {tagName}.");

            // TODO: save tagName and imageBytes in CosmosDB, and periodically re-train model using Azure Functions. 

            return await sc.ReplaceDialogAsync(nameof(CarRecognitionDialog), new DialogOptions { IsInLoop = false });
        }

        // validators
        private async Task<bool> ValidateAttachmentAsync(PromptValidatorContext<IList<Attachment>> vc, CancellationToken cancellationToken)
        {
            if (!vc.Recognized.Succeeded)
            {
                await vc.Context.SendActivityAsync($"I'm sorry, I do not understand that. Please upload a jpeg image of a car.");
                return false;
            }

            var attachmentList = vc.Recognized.Value;

            if (attachmentList == null || 
                attachmentList.Count > 1 ||
                attachmentList[0].ContentType != "image/jpeg")
            {
                await vc.Context.SendActivitiesAsync(
                    new Activity[]
                    {
                        MessageFactory.Text($"Sorry, we can only take one jpeg file."),
                        vc.Options.RetryPrompt,
                    });

                return false;
            }

            return true;
        }
    }
}
