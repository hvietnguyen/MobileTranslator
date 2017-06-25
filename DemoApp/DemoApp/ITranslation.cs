using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public interface ITranslation
    {
        string Key { get; set; }
        string Text { get; set; }
        string Target { get; set; }
        string StringJsonReponse { get; set; }
        Task Translate();
        Task<string> CallWatsonPronunciationAPI(string tex);
    }
}
