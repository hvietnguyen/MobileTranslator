using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DemoApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TranslatorPage : ContentPage
    {
        IRecorder recorder;
        ISpeechRecognition speechRecognition;
        ITranslation tranlation;
        bool isRecording;
        const string SPEECH_API_KEY = "e9e5578f906f4217b24db8f406d0734b";
        const string GOOGLE_TRANSLATE_API = "AIzaSyAUB0cxDbShIOAqDwdEgGuGQ1jEpUvW0xg";

        public TranslatorPage()
        {
            InitializeComponent();
            recorder = DependencyService.Get<IRecorder>();
            speechRecognition = DependencyService.Get<ISpeechRecognition>();
            tranlation = DependencyService.Get<ITranslation>();
            isRecording = false;
        }

        private void Play_Clicked(object sender, EventArgs e)
        {
            isRecording = true;
            RecordedText.Text = String.Empty;
            TranslatedText.Text = String.Empty;
            TranslatedText.Detail = String.Empty;
            try
            {
                if (isRecording)
                {
                    SwitchPlayOrStop(isRecording);
                    recorder.StartRecording();
                }
            }
            catch (ArgumentException ex)
            {
                DisplayAlert("Error", ex.Message, "Cancel");
            }
        }

        private async void Stop_Clicked(object sender, EventArgs e)
        {
            isRecording = false;
            try
            {
                if (!isRecording)
                {
                    SwitchPlayOrStop(isRecording);
                    recorder.StopRecording();
                    // Request Speech REST API
                    speechRecognition.HttpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(
                     @"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US");
                    // Authenticate and get a JSON Web Token (JWT) from the token service.
                    if (speechRecognition.Task == null || speechRecognition.Task.IsCompleted)
                        speechRecognition.Authenticate(SPEECH_API_KEY);
                    // Send the request to Bing Speech API REST end point.
                    var displayText = "Could not detect, please try again!";
                    if (String.IsNullOrEmpty(recorder.WavFilePath))
                    {
                        await DisplayAlert("Message", displayText, "Cancel");
                        return;
                    }
                    speechRecognition.AudioFile = recorder.WavFilePath;
                    speechRecognition.SendRequest();
                    // Get response to get your transcribed text.
                    Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(speechRecognition.GetResponse());
                    //Get Display Text Values
                    if (json.Count > 1)
                    {
                        displayText = json.Value<string>("DisplayText");
                        // Translate the display text
                        tranlation.Key = GOOGLE_TRANSLATE_API;
                        tranlation.Text = displayText;
                        tranlation.Target = "ja";
                        await tranlation.Translate();
                        // Get translated text from response
                        json = Newtonsoft.Json.Linq.JObject.Parse(tranlation.StringJsonReponse);
                        json = json.Value<Newtonsoft.Json.Linq.JObject>("data");
                        var translatedText = (from j in json["translations"] select (string)j["translatedText"]).First<string>().ToString();
                        // Get pronunciation for translated text
                        var pronunciation = await tranlation.CallWatsonPronunciationAPI(translatedText);
                        json = Newtonsoft.Json.Linq.JObject.Parse(pronunciation);
                        
                        TranslatedText.Text = translatedText;
                        TranslatedText.Detail = json.Value<string>("pronunciation");
                    }
                    RecordedText.Text = displayText.ToString();
                }
            }
            catch (ArgumentException ex)
            {
                await DisplayAlert("Error", ex.Message, "Cancel");
            }
        }

        /// <summary>
        /// Switch bwteen buttons play and stop 
        /// </summary>
        /// <param name="isRecording"></param>
        private void SwitchPlayOrStop(bool isRecording)
        {
            Play.IsVisible = Play.IsEnabled = !isRecording;
            Stop.IsVisible = Stop.IsEnabled = isRecording;
        }
       
    }
}