using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace ReceiveAttachmentBot
{
    public class ClownScore
    {
        public double Nose { get; set; }
        public double Bowtie { get; set; }
        public double Normal { get; set; }
        public double? Happiness { get; set; }
    }
    public class CognitiveServiceClient 
    {
        private readonly string _customVisionKey;
        private readonly string _customVisionModel;
        private readonly string _emotionKey;

        private Uri postUri;


        public CognitiveServiceClient(string customVisionKey, string customVisionModel, string emotionKey)
        {
            _customVisionKey = customVisionKey;
            _customVisionModel = customVisionModel;
            _emotionKey = emotionKey;
        }

        private HttpClient GetCustomVisionWebClient()
        {
            
            HttpClient retVal;
            //if (forImage)
            //{
                this.postUri = new Uri($@"{ConfigurationManager.AppSettings["CustomVisionURL"]}{_customVisionModel}/image");
                retVal = new HttpClient { BaseAddress = this.postUri };
            //}
            //else
            //{
            //    this.postUri = new Uri($@"{ConfigurationManager.AppSettings["CustomVisionURL"]}{_customVisionModel}/url");
            //    retVal = new HttpClient { BaseAddress = this.postUri };
            //}

            retVal.DefaultRequestHeaders.Add(@"Prediction-Key", _customVisionKey);
            return retVal;
        }

        private HttpClient GetEmotionWebClient()
        {

            HttpClient retVal;
            this.postUri = new Uri($@"https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize");
            retVal = new HttpClient { BaseAddress = this.postUri };

            retVal.DefaultRequestHeaders.Add(@"Ocp-Apim-Subscription-Key", _emotionKey);
            return retVal;
        }

        public async Task<ClownScore> AnalyzeAsync(Stream imageStream)
        {
            ClownScore clownScore = null;
            Stream secondStream = new MemoryStream();
            await imageStream.CopyToAsync(secondStream);
            imageStream.Position = 0;
            secondStream.Position = 0;
            using (var customVisionClient = GetCustomVisionWebClient())
            using (var content = new StreamContent(imageStream))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(@"application/octet-stream");

                var response = await customVisionClient.PostAsync(this.postUri, content);

                clownScore = await ProcessCustomVisionResponse(response);
            }

            using (var emotionClient = GetEmotionWebClient())
            using (var content = new StreamContent(secondStream))
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(@"application/octet-stream");

                var response = await emotionClient.PostAsync(this.postUri, content);

                var happiness = await ProcessEmotionResponse(response);
                clownScore.Happiness = happiness;
            }


            return clownScore;
        }

        //public async Task<ClownScore> AnalyzeAsync(string imageUri)
        //{
        //    ClownScore clownScore = null;
        //    try
        //    {
        //        using (var customVisionClient = GetCustomVisionWebClient())
        //        using (var content = new StringContent($@"{{ ""url"" : ""{imageUri}"" }}"))
        //        {
        //            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(@"application/json");

        //            var response = await customVisionClient.PostAsync(this.postUri, content);

        //            clownScore = await ProcessCustomVisionResponse(response);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Trace.TraceError(ex.ToString());
        //        clownScore =  null;
        //    }
        //    return clownScore;
        //}

        private async Task<double?> ProcessEmotionResponse(HttpResponseMessage response)
        {
            List<double> scores = new List<double>();
            if (response.IsSuccessStatusCode)
            {
                JArray result = JArray.Parse(await response.Content.ReadAsStringAsync());
                foreach (JObject tag in result)
                {
                    scores.Add(tag["scores"]["happiness"].Value<double>());
                }
                if (scores.Count == 0)
                    return null;
                else
                    return scores.ToArray().Sum() / scores.Count;
            }
            else
            {
                return null;
            }
        }
        private async Task<ClownScore> ProcessCustomVisionResponse(HttpResponseMessage response)
        {
            ClownScore score = new ClownScore();
            if (response.IsSuccessStatusCode)
            {
                JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
                foreach (JObject tag in (JArray)result.Root["Predictions"])
                {
                    if (tag["Tag"].Value<string>()=="bowtie")
                    {
                        score.Bowtie = tag["Probability"].Value<double>();
                    }else if (tag["Tag"].Value<string>() == "nose")
                    {
                        score.Nose = tag["Probability"].Value<double>();
                    }else
                    {
                        score.Normal = tag["Probability"].Value<double>();
                    }
                }
                return score;
            }
            else
            {
                return null;
            }
        }
    }
}