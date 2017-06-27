using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public interface ISpeechRecognition
    {
        //System.Threading.Tasks.Task Task { get; set; }
        string AudioFile { get; set; }
        System.Net.HttpWebRequest HttpRequest { get; set; }
        string Token { get; }
        //string HttpStringResponse { get;}
        Task Authenticate(string subscription_key);
        void SendRequest();
        string GetResponse();
    }
}
