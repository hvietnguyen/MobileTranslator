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
    public partial class SentencesPage : ContentPage
    {
        IAzureEasyTableClient azureEasyTableClient = null;
        List<TranslationModel> models = null;
        
        public SentencesPage()
        {
            InitializeComponent();
            azureEasyTableClient = DependencyService.Get<IAzureEasyTableClient>();
            listView.IsRefreshing = false;
            models = new List<TranslationModel>();
            //var translationViewListModel = new TranslationViewList();
            //BindingContext = translationViewListModel;
        }

        private void ListViewData(List<TranslationModel> models)
        {
            if (models == null || models.Count <= 0) return;

            listView.ItemsSource = models;
            DataTemplate dataTemplate = new DataTemplate(() =>
            {
                Label recordedText = new Label();
                recordedText.FontSize = 14;
                recordedText.FontFamily = "Sans-Serif";
                recordedText.SetBinding(Label.TextProperty, "Text");

                Label translatedText = new Label();
                translatedText.FontSize = 14;
                translatedText.TextColor = Color.LightPink;
                translatedText.SetBinding(Label.TextProperty, "TranslatedText");

                Label pronunciation = new Label();
                pronunciation.FontSize = 12;
                pronunciation.TextColor = Color.LightGray;
                pronunciation.SetBinding(Label.TextProperty, "Pronunciation");



                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Vertical,
                        Spacing = 1,
                        Children =
                        {
                            recordedText,
                            new StackLayout
                            {
                                Orientation = StackOrientation.Horizontal,
                                Spacing = 1,
                                Children=
                                {
                                    translatedText,
                                    pronunciation
                                }
                            }

                        }
                    }
                };
            });

            listView.ItemTemplate = dataTemplate;
            this.Content = new StackLayout
            {
                Children =
                {
                    listView
                }
            };

            listView.EndRefresh();
        }

        private async Task GetData(List<TranslationModel> models)
        {
            azureEasyTableClient.BaseAddress = @"http://mobiletranslator.azurewebsites.net/";
            azureEasyTableClient.TargetAPI = @"tables/TranslatedText";
            string data = await azureEasyTableClient.GetDataListAsync();
            data = "{\"data\":" + data + "}";
            Newtonsoft.Json.Linq.JObject jobject = Newtonsoft.Json.Linq.JObject.Parse(data);
            if (jobject.HasValues)
            {
                var elements = (from j in jobject["data"] select j).GetEnumerator();
                while (elements.MoveNext())
                {
                    var text = elements.Current.Value<string>("Text");
                    var translatedText = elements.Current.Value<string>("TransltedText");
                    var pronunciation = elements.Current.Value<string>("Pronunciation");
                    TranslationModel model = new TranslationModel()
                    {
                        Text = text,
                        TranslatedText = translatedText,
                        Pronunciation = pronunciation
                    };

                    models.Add(model);
                }
            }
        }

        private async void listView_Refreshing(object sender, EventArgs e)
        {
            await GetData(models);
            ListViewData(models);
        }
    }
}