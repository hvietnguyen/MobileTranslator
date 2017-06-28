using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoApp.Model;

namespace DemoApp
{
    public interface IAzureEasyTableClient
    {
        string BaseAddress { get; set; }
        string TargetAPI { get; set; }
        string StringResponse { get; set; }
        Task<string> GetDataListAsync();
        Task<bool> PostDataAsync(TranslationModel model);
        Task PutDataAsync(TranslationModel model);
        Task<bool> DeleteDataAsync(string id);
    }
}
