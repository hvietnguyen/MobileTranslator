using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public interface IRecorder
    {
        string WavFilePath { get; }
        void StartRecording();
        void StopRecording();
    }
}
