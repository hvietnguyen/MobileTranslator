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
using System.Threading.Tasks;

namespace DemoApp.Droid
{
    /*
     * This class demonstrates how to get a valid O-auth token.
     */
    public class Authentication
    {
        public static readonly string FetchTokenUri = "https://api.cognitive.microsoft.com/sts/v1.0";
        public static string Token { get; set; }

        private string subscriptionKey;
        private System.Threading.Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes.
        private const int RefreshTokenDuration = 9;

        public Authentication(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        public async Task Authenticate()
        {
            Token = await FetchToken(FetchTokenUri, subscriptionKey);

            // renew the token on set duration.
            try
            {
                accessTokenRenewer = new System.Threading.Timer(new System.Threading.TimerCallback(OnTokenExpiredCallback),
                                          this,
                                          TimeSpan.FromMinutes(RefreshTokenDuration),
                                          TimeSpan.FromMilliseconds(-1));
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
        }

        //public string GetAccessToken()
        //{
        //    return this.token;
        //}

        private void RenewAccessToken()
        {
            Token = FetchToken(FetchTokenUri, this.subscriptionKey).Result;
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private async Task<string> FetchToken(string fetchUri, string subscriptionKey)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(fetchUri);
                uriBuilder.Path += "/issueToken";

                var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}