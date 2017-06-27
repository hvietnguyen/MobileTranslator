using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using DemoApp.Model;

namespace DemoApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TranslatorPage : ContentPage
    {
        IRecorder recorder;
        ISpeechRecognition speechRecognition;
        ITranslation tranlation;
        IAzureEasyTableClient azureEasyTableClient;
        bool isRecording;
        const string SPEECH_API_KEY = "e9e5578f906f4217b24db8f406d0734b";
        const string GOOGLE_TRANSLATE_API = "AIzaSyAUB0cxDbShIOAqDwdEgGuGQ1jEpUvW0xg";

        public TranslatorPage()
        {
            InitializeComponent();
            // Dependency Injection - reference to classes in Android module
            recorder = DependencyService.Get<IRecorder>();
            speechRecognition = DependencyService.Get<ISpeechRecognition>();
            tranlation = DependencyService.Get<ITranslation>();
            azureEasyTableClient = DependencyService.Get<IAzureEasyTableClient>();

            isRecording = false;
        }

        /// <summary>
        /// click event of play button.
        /// Recording the voice from speaker by Mobile Mic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    SwitchPlayOrStop(isRecording, false);
                    recorder.StartRecording();
                }
            }
            catch (ArgumentException ex)
            {
                DisplayAlert("Error", ex.Message, "Cancel");
            }
        }

        /// <summary>
        /// Click event of stop button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task Stop_Clicked(object sender, EventArgs e)
        {
            isRecording = false;
            try
            {
                if (!isRecording)
                {
                    SwitchPlayOrStop(isRecording, true);
                    
                    recorder.StopRecording();
                    // Request Speech REST API
                    speechRecognition.HttpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(
                     @"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US");
                    // Authenticate and get a JSON Web Token (JWT) from the token service.
                    //if (speechRecognition.Task == null || speechRecognition.Task.IsCompleted)
                    await speechRecognition.Authenticate(SPEECH_API_KEY);
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
                    SwitchPlayOrStop(isRecording, false);
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
        /// /// <param name="isLoading"></param>
        private void SwitchPlayOrStop(bool isRecording, bool isLoading)
        {
            Play.IsVisible = !isRecording;
            Play.IsEnabled = !isLoading;
            Play.Image = isLoading ? "loading.gif" : "Play.png";
            Play.HeightRequest = Play.WidthRequest = isLoading ? 40 : 60;
            Stop.IsVisible = Stop.IsEnabled = isRecording;
        }

        /// <summary>
        /// Save original text and translated text to azure data base service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TranslatedText_Tapped(object sender, EventArgs e)
        {
            if(String.IsNullOrEmpty(RecordedText.Text) || String.IsNullOrEmpty(TranslatedText.Text) 
                || String.IsNullOrEmpty(TranslatedText.Detail))
            {
                return;
            }

            TranslationModel model = new TranslationModel()
            {
                Text=RecordedText.Text,
                TranslatedText = TranslatedText.Text,
                Pronunciation = TranslatedText.Detail
            };

            azureEasyTableClient.BaseAddress = @"http://mobiletranslator.azurewebsites.net/";
            azureEasyTableClient.TargetAPI = @"tables/TranslatedText";
            bool result = await azureEasyTableClient.PostDataAsync(model);

            if(result)
            {
                await DisplayAlert("Message", "Save data successful", "OK");
                RecordedText.Text = String.Empty;
                TranslatedText.Text = String.Empty;
                TranslatedText.Detail = String.Empty;
            }
        }
    }
}