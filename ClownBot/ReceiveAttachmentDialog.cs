namespace ReceiveAttachmentBot
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System.Configuration;

    [Serializable]
    internal class ReceiveAttachmentDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            if (message.Attachments != null && message.Attachments.Any())
            {
                var attachment = message.Attachments.First();
 
                using (HttpClient httpClient = new HttpClient())
                {

                    var responseMessage = await httpClient.GetAsync(attachment.ContentUrl);

                    var contentLenghtBytes = responseMessage.Content.Headers.ContentLength;
                    CognitiveServiceClient customVision = new CognitiveServiceClient(ConfigurationManager.AppSettings["CustomVisionKey"], 
                                                                                     ConfigurationManager.AppSettings["CustomVisionModel"],
                                                                                     ConfigurationManager.AppSettings["EmotionAPIKey"]);
                    var result = await customVision.AnalyzeAsync(await responseMessage.Content.ReadAsStreamAsync());
                  
                    string response = "Let me see...";
                    if (result.Nose > 0.8 && result.Bowtie > 0.8)
                        response += " There is a lot of clown in you, well done.";
                    else if (result.Nose > 0.8)
                        response += " That is a respectable nose.";
                    else if (result.Bowtie > 0.8)
                        response += " Nice bowtie there";
                    else
                        response += " Well, there's nothing interesting about this photo that I want to comment";
                    if (result.Happiness.HasValue)
                    {
                        response += " By the way, the overall happiness score of this photo is: " + Math.Round(result.Happiness.Value*100).ToString() + "%";
                        if (result.Happiness.Value<0.5)
                            response += " That doesn't make me happy at all";
                        else if (result.Happiness.Value < 0.8)
                            response += " That is a pretty ok photo";
                        else
                            response += " OMG THERE'S SO MUCH HAPPINESS HERE THAT I CAN'T EVEN";
                        

                    }


                    await context.PostAsync( response);

                }
            }
            else
            {
                await context.PostAsync("Less talk, more photos. Send me your photo and let me judge your clown factor.");
            }

            context.Wait(this.MessageReceivedAsync);
        }
    }
}