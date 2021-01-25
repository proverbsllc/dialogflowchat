using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HermesTranslationChat
{
    public partial class _Default : Page
    {
        const string COGNITIVE_SERVICES_KEY = "5ef5adebfb0243b1ba9fec6a4b1ea3be";
        // Endpoints for Translator Text and Bing Spell Check
        public static readonly string TEXT_TRANSLATION_API_ENDPOINT = "https://api.cognitive.microsofttranslator.com/{0}?api-version=3.0";
        // An array of language codes
        private string[] languageCodes;

        // Dictionary to map language codes from friendly name (sorted case-insensitively on language name)
        private SortedDictionary<string, string> languageCodesAndTitles =
            new SortedDictionary<string, string>(Comparer<string>.Create((a, b) => string.Compare(a, b, true)));

        protected global::System.Web.UI.WebControls.DropDownList ddlLanguageSelect;
        protected global::System.Web.UI.WebControls.Button echo;
        protected global::System.Web.UI.WebControls.Button sendmessage;
        protected global::System.Web.UI.WebControls.TextBox message;

        protected void Page_Load(object sender, EventArgs e)
        {
            GetLanguagesForTranslate();

            PopulateLanguageMenus();

            UpdateScreenText();
        }
        
        private void UpdateScreenText()
        {
            ddlLanguageSelect.Attributes["title"] = TranslateTextAsync("Select your language", "en");
            echo.Attributes["title"] = TranslateTextAsync("Translate your message to the \nreceiver's language and then \nback to your language", "en");
            sendmessage.Attributes["title"] = TranslateTextAsync("Translate your message to the \nreceiver's language and transmit", "en");
            echo.Text = TranslateTextAsync("Echo", "en");
            sendmessage.Text = TranslateTextAsync("Send", "en");
            message.Attributes["placeholder"] = TranslateTextAsync("Type message to be translated and press Enter to send...", "en");
            int count = echo.Text.Length;
            echo.Width = Math.Max(count * 14 + 4, 60);
            count = sendmessage.Text.Length;
            sendmessage.Width = Math.Max(count * 14 + 4, 60);
        }

        private void PopulateLanguageMenus()
        {
            String[] userLang = Request.UserLanguages;
  
            // Add option to automatically detect the source language
            int count = languageCodesAndTitles.Count;
            foreach (string menuItem in languageCodesAndTitles.Keys)
            {
                ddlLanguageSelect.Items.Add(new ListItem(menuItem, languageCodesAndTitles[menuItem]));
                if (!IsPostBack && userLang.Length > 0 && userLang[0].Substring(0, 2) == languageCodesAndTitles[menuItem])
                {
                    ddlLanguageSelect.SelectedValue = languageCodesAndTitles[menuItem];
                }
            }
        }

        private void GetLanguagesForTranslate()
        {
            // Send a request to get supported language codes
            string uri = String.Format(TEXT_TRANSLATION_API_ENDPOINT, "languages") + "&scope=translation";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
            WebRequest.Headers.Add("Accept-Language", "en");
            WebResponse response = null;
            // Read and parse the JSON response
            response = WebRequest.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream(), UnicodeEncoding.UTF8))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(reader.ReadToEnd());
                var languages = result["translation"];

                languageCodes = languages.Keys.ToArray();
                foreach (var kv in languages)
                {
                    if (kv.Value["name"] == kv.Value["nativeName"])
                    {
                        languageCodesAndTitles.Add(kv.Value["name"], kv.Key);
                    }
                    else
                    {
                        languageCodesAndTitles.Add(kv.Value["name"] + " (" + kv.Value["nativeName"] + ')', kv.Key);
                    }
                }
            }
        }

        private string TranslateTextAsync(string TextToTranslate, string fromLanguageCode)
        {
            string textToTranslate = TextToTranslate.Trim();

            string toLanguageCode = ddlLanguageSelect.SelectedValue.ToString();

            // Spell-check the source text if the source language is English
            //if (fromLanguageCode == "en")
            //{
            //    if (textToTranslate.StartsWith("-"))    // don't spell check in this case
            //        textToTranslate = textToTranslate.Substring(1);
            //    else
            //    {
            //        textToTranslate = CorrectSpelling(textToTranslate);
            //        TextToTranslate.Text = textToTranslate;     // put corrected text into input field
            //    }
            //}

            // Handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromLanguageCode == toLanguageCode)
            {
                return textToTranslate;
            }

            // Send translation request
            string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);

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
                return translation;
            }
        }

        protected void ddlLanguageSelect_Change(object sender, EventArgs e)
        {
            UpdateScreenText();
        }

    }
}