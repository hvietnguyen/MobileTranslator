﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using DemoApp.Model;

[assembly:Dependency(typeof(DemoApp.Droid.AndroidAzureEasyTableClient))]
namespace DemoApp.Droid
{
    class AndroidAzureEasyTableClient : IAzureEasyTableClient
    {
        HttpClient client = null;
        HttpResponseMessage reponse = null;
        public string BaseAddress { get; set; }

        public string StringResponse
        {
            get;set;
        }

        public string TargetAPI { get; set; }


        public async Task<bool> DeleteDataAsync(TranslationModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetDataListAsync()
        {
            string data = String.Empty;
            client = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseAddress + TargetAPI));
            requestMessage.Headers.Add("ZUMO-API-VERSION", "2.0.0");
            this.reponse = await client.SendAsync(requestMessage);
            if (reponse.IsSuccessStatusCode)
            {
                data = await this.reponse.Content.ReadAsStringAsync();
            }
            return data;
        }

        public async Task<bool> PostDataAsync(TranslationModel model)
        {
            if (model == null || BaseAddress == null || String.IsNullOrEmpty(TargetAPI))
            {
                return false;
            }

            client = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseAddress + TargetAPI));
            //requestMessage.Headers.Add(("Content-Type","application/json");
            requestMessage.Headers.Add("ZUMO-API-VERSION", "2.0.0");

            string data = "{" + $"\"Text\":\"{model.Text}\", "+
                $"\"TransltedText\":\"{model.TranslatedText}\", "+
                $"\"Pronunciation\":\"{model.Pronunciation}\""+"}";

            requestMessage.Content = new StringContent(data, Encoding.UTF8, "application/json");
            this.reponse = await client.SendAsync(requestMessage);
            return reponse.IsSuccessStatusCode ? true : false;
        }

        public async Task PutDataAsync(TranslationModel model)
        {
            throw new NotImplementedException();
        }
    }
}