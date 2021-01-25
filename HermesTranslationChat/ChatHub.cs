using System;
using System.Web;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using System.Text;
using Newtonsoft.Json;

namespace SignalRChat
{
    public class ChatHub : Hub
    {
        const string COGNITIVE_SERVICES_KEY = "5ef5adebfb0243b1ba9fec6a4b1ea3be";
        // Endpoints for Translator Text and Bing Spell Check
        public static readonly string TEXT_TRANSLATION_API_ENDPOINT = "https://api.cognitive.microsofttranslator.com/{0}?api-version=3.0";
        // An array of language codes
        private string[] languageCodes;

        // Dictionary to map language codes from friendly name (sorted case-insensitively on language name)
        private SortedDictionary<string, string> languageCodesAndTitles =
            new SortedDictionary<string, string>(Comparer<string>.Create((a, b) => string.Compare(a, b, true)));

        public void Send(string name, string language, string message)
        {
            // Call the addNewMessageToPage method to update clients.
            Clients.All.addNewMessageToPage(name, language, message);
        }

        public void EchoSent(string fromCode, string toCode, string message)
        {
            string textToTranslate = message.Trim();

            // Handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromCode == toCode)
            {
                Clients.Caller.echoTranslate(message);
                return;
            }

            // Send translation request
            string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromCode, toCode);

            System.Object[] body = new System.Object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "eastus2");
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                var response = client.SendAsync(request);
                var responseBody = response.GetAwaiter().GetResult().Content.ReadAsStringAsync();//.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody.GetAwaiter().GetResult());
                var translation = result[0]["translations"][0]["text"];

                string uri2 = string.Format(endpoint + "&from={0}&to={1}", toCode, fromCode);

                // now translate back
                System.Object[] body2 = new System.Object[] { new { Text = translation } };
                var requestBody2 = JsonConvert.SerializeObject(body);
                using (var client2 = new HttpClient())
                using (var request2 = new HttpRequestMessage())
                {
                    request2.Method = HttpMethod.Post;
                    request2.RequestUri = new Uri(uri2);
                    request2.Content = new StringContent(requestBody2, Encoding.UTF8, "application/json");
                    request2.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
                    request2.Headers.Add("Ocp-Apim-Subscription-Region", "eastus2");
                    request2.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                    var response2 = client.SendAsync(request2);
                    var responseBody2 = response2.GetAwaiter().GetResult().Content.ReadAsStringAsync();//.Content.ReadAsStringAsync();

                    var result2 = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody2.GetAwaiter().GetResult());
                    var translation2 = result[0]["translations"][0]["text"];

                    // Update the translation field
                    Clients.Caller.echoTranslate(translation2);
                }
            }
        }

        public void Translate(string fromCode, string toCode, string name, string message)
        {
            string textToTranslate = message.Trim();

            // Handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromCode == toCode)
            {
                Clients.Caller.postMessageTranslation(name, message);
                return;
            }

            // Send translation request
            string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromCode, toCode);

            System.Object[] body = new System.Object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "eastus2");
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

                var response = client.SendAsync(request);
                var responseBody = response.GetAwaiter().GetResult().Content.ReadAsStringAsync();//.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody.GetAwaiter().GetResult());
                var translation = result[0]["translations"][0]["text"];

                // Update the translation field
                Clients.Caller.postMessageTranslation(name, translation);
            }
        }
    }
}