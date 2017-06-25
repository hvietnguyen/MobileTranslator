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

[assembly: Dependency(typeof(DemoApp.Droid.SpeechRecognition))]
namespace DemoApp.Droid
{
    class SpeechRecognition : ISpeechRecognition
    {
        //static System.Threading.Thread thread = null;
        //static System.Threading.ThreadStart delegateThread = null;
        System.Threading.Tasks.Task task = null;
        public System.Threading.Tasks.Task Task
        {
            get
            {
                return task;
            }
            set
            {
                task = value;
            }
        }
        public string AudioFile { get; set; }

        public System.Net.HttpWebRequest HttpRequest { get; set; }
       
        public string Token { get; }

        private string httpStringResponse;

        public string HttpStringResponse
        {
            get { return httpStringResponse; }
        }

        /// <summary>
        /// Authenticate and get a JSON Web Token (JWT) from the token service.
        /// </summary>
        /// <param name="subscription_key"></param>
        public void Authenticate(string subscription_key)
        {
            DemoApp.Droid.Authentication authentication = new DemoApp.Droid.Authentication(subscription_key);
            Task = System.Threading.Tasks.Task.Factory.StartNew(()=>authentication.Authenticate());
            //delegateThread = new System.Threading.ThreadStart(authentication.Authenticate);
            //thread = new System.Threading.Thread(delegateThread);
            //thread.Start();

            if (HttpRequest == null)
            {
                throw new ArgumentException("HttpWebRequest is null!");
            }

            HttpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(@"https://speech.platform.bing.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US");
            HttpRequest.SendChunked = true;
            HttpRequest.Accept = @"application/json;text/xml";
            HttpRequest.Method = "POST";
            HttpRequest.ProtocolVersion = System.Net.HttpVersion.Version11;
            HttpRequest.Host = @"speech.platform.bing.com";
            HttpRequest.ContentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";
            HttpRequest.Headers["Authorization"] = "Bearer " + Authentication.Token;
        }

        /// <summary>
        /// Set the proper request header and send the request to Bing Speech API REST end point.
        /// </summary>
        public void SendRequest()
        {
            if (String.IsNullOrEmpty(AudioFile))
            {
                throw new ArgumentException("AudioFile is missing or empty!");
            }

            using (var fs = new System.IO.FileStream(AudioFile, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                /*
                * Open a request stream and write 1024 byte chunks in the stream one at a time.
                */
                byte[] data = null;
                int bytesRead = 0;
                using (System.IO.Stream requestStream = HttpRequest.GetRequestStream())
                {
                    /*
                    * Read 1024 raw bytes from the input audio file.
                    */
                    data = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                    while ((bytesRead = fs.Read(data, 0, data.Length)) != 0)
                    {
                        requestStream.Write(data, 0, bytesRead);
                    }
                    // Flush
                    requestStream.Flush();
                }
            }
        }

        /// <summary>
        /// Get response to get your transcribed text.
        /// </summary>
        /// <returns> data as json string format</returns>
        public string GetResponse()
        {
            string data = String.Empty;
            try
            {
                using (System.Net.WebResponse response = HttpRequest.GetResponse())
                {
                    Console.WriteLine(((System.Net.HttpWebResponse)response).StatusCode);

                    using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        data = sr.ReadToEnd();
                    }
                }
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return data;
        }
    }
}