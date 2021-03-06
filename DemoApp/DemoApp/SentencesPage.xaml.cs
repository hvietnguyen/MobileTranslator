﻿using System;
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
        List<TranslationModelView> models = null;
        Label id = null;
        /// <summary>
        /// Property use to binding to IsRefreshing property of listview to turn off the
        /// refreshing icon when loading data completed.
        /// </summary>
        bool isRefreshing = false;
        public bool IsRefreshing
        {
            get
            {
                return isRefreshing;
            }
            set
            {
                isRefreshing = value;
            }
        }

        public SentencesPage()
        {
            InitializeComponent();
            azureEasyTableClient = DependencyService.Get<IAzureEasyTableClient>();
            listView.SetBinding(ListView.IsRefreshingProperty, "IsRefreshing");
            models = new List<TranslationModelView>();
            //var translationViewListModel = new TranslationViewList();
            //BindingContext = translationViewListModel;
        }

        private void ListViewData(List<TranslationModelView> models)
        {
            DataTemplate dataTemplate = null;
            if (models == null || models.Count <= 0)
            {
                DisplayAlert("Message", "No Data", "Ok");
                return;
            }

            listView.ItemsSource = models;

            dataTemplate = new DataTemplate(() =>
            {
                // Create label components and binding data to them 
                id = new Label();
                id.IsVisible = false;
                id.SetBinding(Label.TextProperty, "ID");

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

                // A ViewCell is wrapping layouts and components
                return new ViewCell
                {
                    View = new StackLayout
                    {
                        Padding = new Thickness(0, 5),
                        Orientation = StackOrientation.Vertical,
                        Spacing = 1,
                        Children =
                        {
                            id,
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

            // Set dataTemplate to ItemTemplate property of listview
            listView.ItemTemplate = dataTemplate;

            this.Content = new StackLayout
            {
                Children =
                {
                    listView
                }
            };
        }

        /// <summary>
        /// Get Data from Easy Table in MS Azure service
        /// </summary>
        /// <param name="models">List of TranslationModel to hold data</param>
        /// <returns></returns>
        private async Task GetData(List<TranslationModelView> models)
        {
            models.Clear(); // Clear list of TranslationModel
            // Call Azure service to get data from Easy Table
            azureEasyTableClient.BaseAddress = @"http://mobiletranslator.azurewebsites.net/";
            azureEasyTableClient.TargetAPI = @"tables/TranslatedText";
            string data = await azureEasyTableClient.GetDataListAsync();

            data = "{\"data\":" + data + "}"; // Adding prefix and postfix to returning data from API

            Newtonsoft.Json.Linq.JObject jobject = Newtonsoft.Json.Linq.JObject.Parse(data);
            if (jobject.HasValues)
            {
                // Geting data from JObject and adding to TranslationModel List 
                var elements = (from j in jobject["data"] select j).GetEnumerator();
                while (elements.MoveNext())
                {
                    var id = elements.Current.Value<string>("id");
                    var text = elements.Current.Value<string>("Text");
                    var translatedText = elements.Current.Value<string>("TransltedText");
                    var pronunciation = elements.Current.Value<string>("Pronunciation");
                    TranslationModelView model = new TranslationModelView()
                    {
                        ID = id,
                        Text = text,
                        TranslatedText = translatedText,
                        Pronunciation = pronunciation
                    };

                    models.Add(model);
                }
            }
        }

        /// <summary>
        /// Refreshing event of list view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void listView_Refreshing(object sender, EventArgs e)
        {
            IsRefreshing = true;
            await GetData(models);
            ListViewData(models);
            IsRefreshing = false;  
        }

        private void listView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            //((ListView)sender).SelectedItem = null;
        }

        private async void listView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            try
            {
                var action = await DisplayActionSheet("Menus", "Cancel", null, "Delete");
                
                if (action.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    bool result = await azureEasyTableClient.DeleteDataAsync(id.Text);
                    if (result)
                    {
                        IsRefreshing = true;
                        await GetData(models);
                        ListViewData(models);
                        IsRefreshing = false;

                        await DisplayAlert("Message", "Delete item successfully!", "Ok");
                    }
                    else
                    {
                        await DisplayAlert("Alert", "Delete item fail!", "Ok");
                    }
                }
            }
            catch(Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Ok");
            }
            
        }
    }
}