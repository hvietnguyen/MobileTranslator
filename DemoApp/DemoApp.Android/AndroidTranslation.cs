using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using System.Threading.Tasks;

[assembly: Dependency(typeof(DemoApp.Droid.AndroidTranslation))]
namespace DemoApp.Droid
{
    class AndroidTranslation : ITranslation
    {
        #region Properties
        string key = String.Empty;
        public string Key
        {
            get
            {
                return key;
            }

            set
            {
                key = value;
            }
        }

        string target = String.Empty;
        public string Target
        {
            get
            {
                return target;
            }

            set
            {
                target = value;
            }
        }

        string text = String.Empty;
        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                text = value;
            }
        }

        public string StringJsonReponse { get; set; }
        #endregion

        public async Task Translate()
        {
            await CallGoogleTranslateAPI();
           
        }

        /// <summary>
        /// Call Google translation API and set response string data to StringJsonResponse property
        /// </summary>
        /// <returns></returns>
        private async Task CallGoogleTranslateAPI()
        {
            if(String.IsNullOrEmpty(Key)|| String.IsNullOrEmpty(Text)|| String.IsNullOrEmpty(Target))
            {
                StringJsonReponse = String.Empty;
                return;
            }

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            client.BaseAddress = new Uri(@"https://translation.googleapis.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            System.Net.Http.HttpResponseMessage response = await client.PostAsync($@"language/translate/v2/?key={Key}&q={Text}&target={Target}", null);
            if (response.IsSuccessStatusCode)
            {
                StringJsonReponse = await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Call Watson Pronunciation API
        /// </summary>
        /// <param name="text">string text need to be pronuonced</param>
        /// <returns>string json</returns>
        public async Task<string> CallWatsonPronunciationAPI(string text)
        {
            string value = String.Empty;
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            client.BaseAddress = new Uri(@"https://watson-api-explorer.mybluemix.net");
            client.DefaultRequestHeaders.Accept.Clear();
            System.Net.Http.HttpResponseMessage response = await client.GetAsync($@"/text-to-speech/api/v1/pronunciation?text={text}&voice=ja-JP_EmiVoice&format=ipa");
            if (response.IsSuccessStatusCode)
            {
                value = await response.Content.ReadAsStringAsync();
            }

            return value;
        }
    }
}