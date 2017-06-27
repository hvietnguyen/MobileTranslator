using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Media;
using System.IO;

[assembly:Dependency(typeof(DemoApp.Droid.AndroidRecorder))]
namespace DemoApp.Droid
{
    class AndroidRecorder : IRecorder
    {
        protected System.Threading.ThreadStart delegateThreadStart = null;
        protected System.Threading.Thread audioThread = null;

        protected static AudioRecord audioRecord;
        protected static string wavFile = String.Empty;
        protected static string rawFile = String.Empty;
        protected static int bufferSize = 0;
        protected static bool isRecording = false;

        protected const int RECORDER_BPP = 16;
        protected const AudioSource AUDIO_SOURCE = AudioSource.Mic;
        protected const int SAMPLE_RATE = 44100;
        protected const ChannelIn CHANNEL_INPUT = ChannelIn.Stereo;
        protected const Android.Media.Encoding ENCODING = Android.Media.Encoding.Pcm16bit;

        public string WavFilePath
        {
            get { return wavFile; }
        }

        public AndroidRecorder()
        {
        }
        /// <summary>
        /// Start recorder
        /// </summary>
        public void StartRecording()
        {
            isRecording = true;
            try
            {
                // Start recorder
                bufferSize = AudioRecord.GetMinBufferSize(8000, ChannelIn.Mono, ENCODING);
                audioRecord = new AudioRecord(AUDIO_SOURCE, SAMPLE_RATE, CHANNEL_INPUT, ENCODING, bufferSize);

                if (audioRecord == null)
                {
                    throw new ArgumentException("No audio recorder activated!");
                }
                
                if (audioRecord != null && audioRecord.State == State.Initialized)
                    audioRecord.StartRecording();

                // Start audio thread to write audio data to temp .raw file
                delegateThreadStart = new System.Threading.ThreadStart(AndroidRecorder.WriteAudioDataToTempFile);
                audioThread = new System.Threading.Thread(delegateThreadStart);
                audioThread.Start();
            }
            catch(OutOfMemoryException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch(System.Threading.ThreadStateException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch (Exception e)
            {
                throw new System.ArgumentException(e.Message);
            }

        }

        /// <summary>
        /// Stop recorder
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
            try
            {
                if(audioRecord.State == State.Initialized)
                {
                    // Stop Audio Recorder
                    audioRecord.Stop();
                    audioRecord.Release();
                    audioRecord = null;
                    // Stop thread
                    audioThread.Abort();
                    audioThread = null;
                }

                // Create file path for .wav file
                wavFile = Android.OS.Environment.ExternalStorageDirectory.Path + "/AudioRecorderFile.wav";
                ConvertRawFileToWavFile(rawFile, wavFile);
                // Delete temp file
                new Java.IO.File(rawFile).Delete();
            }
            catch (Exception e)
            {
                throw new System.ArgumentException(e.Message);
            }

        }

        #region Private Functions
        /// <summary>
        /// Read data from audio stream to temp file
        /// </summary>
        protected static void WriteAudioDataToTempFile()
        {
            byte[] data = new byte[bufferSize];
            try
            {
                // Create temp .raw file
                rawFile = Android.OS.Environment.ExternalStorageDirectory.Path+"/TempAudioRecorderFile.raw";
                using (System.IO.Stream fileSream = System.IO.File.Open(rawFile, FileMode.OpenOrCreate))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileSream))
                    {
                        while (isRecording)
                        {
                            // Reading data from recorder device to variable data (byte[])
                            audioRecord.Read(data, 0, bufferSize);
                            // Write data to .raw file
                            binaryWriter.Write(data);
                        }
                    }
                }
            }
            catch(Java.IO.FileNotFoundException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch(System.IO.IOException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch(NullReferenceException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
                
             
        }

        /// <summary>
        /// Create a .wav file, then write 44 bytes wav file header and data from temp file to the .wav file
        /// </summary>
        /// <param name="rawFilePath"></param>
        /// <param name="wavFilePath"></param>
        protected void ConvertRawFileToWavFile(string rawFilePath,string wavFilePath)
        {
            long totalAudioLen = 0;
            long totalDatalen = 0;
            long longSampleRate = SAMPLE_RATE;
            int channels = 2;
            long byteRate = RECORDER_BPP * SAMPLE_RATE * channels / 8;

            byte[] data = new byte[bufferSize];
            try
            {
                using (System.IO.Stream rawSream = System.IO.File.Open(rawFilePath, FileMode.Open))
                {
                    using (System.IO.Stream wavSream = System.IO.File.Open(wavFilePath, FileMode.Create))
                    {
                        totalAudioLen = bufferSize;//rawSream.Length;
                        totalDatalen = totalAudioLen + 36;
                        using (BinaryWriter binaryWriter = new BinaryWriter(wavSream))
                        {
                            // Write file header to .wav file
                            WriteWaveFileHeader(binaryWriter, totalAudioLen, totalDatalen, longSampleRate, channels, byteRate);
                            using (BinaryReader binaryReader = new BinaryReader(rawSream))
                            {
                                // Reading data from .raw file to variable data (byte[])
                                while(binaryReader.Read(data, 0, bufferSize) > 0)
                                {
                                    // write data reading from .raw file to .wav file
                                    binaryWriter.Write(data);
                                }
                                
                            }
                        }
                    }
                }
            }
            catch(Java.IO.FileNotFoundException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            catch(System.IO.IOException ex)
            {
                throw new ArgumentException(ex.Message);
            }
            
        }

        /// <summary>
        /// Write .wav file header
        /// </summary>
        /// <param name="wavStream"></param>
        /// <param name="bWriter"></param>
        /// <param name="totalAudioLen"></param>
        /// <param name="totalDataLen"></param>
        /// <param name="longSampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="byteRate"></param>
        protected void WriteWaveFileHeader(System.IO.BinaryWriter bWriter, long totalAudioLen,long totalDataLen, long longSampleRate, int channels, long byteRate)
        {
            byte[] header = new byte[44];

            header[0] = (byte)'R'; // RIFF/WAVE header
            header[1] = (byte)'I';
            header[2] = (byte)'F';
            header[3] = (byte)'F';
            header[4] = (byte)(totalDataLen & 0xff);
            header[5] = (byte)((totalDataLen >> 8) & 0xff);
            header[6] = (byte)((totalDataLen >> 16) & 0xff);
            header[7] = (byte)((totalDataLen >> 24) & 0xff);
            header[8] = (byte)'W';
            header[9] = (byte)'A';
            header[10] = (byte)'V';
            header[11] = (byte)'E';
            header[12] = (byte)'f'; // 'fmt ' chunk
            header[13] = (byte)'m';
            header[14] = (byte)'t';
            header[15] = (byte)' ';
            header[16] = 16; // 4 bytes: size of 'fmt ' chunk
            header[17] = 0;
            header[18] = 0;
            header[19] = 0;
            header[20] = 1; // format = 1
            header[21] = 0;
            header[22] = (byte)channels;
            header[23] = 0;
            header[24] = (byte)(longSampleRate & 0xff);
            header[25] = (byte)((longSampleRate >> 8) & 0xff);
            header[26] = (byte)((longSampleRate >> 16) & 0xff);
            header[27] = (byte)((longSampleRate >> 24) & 0xff);
            header[28] = (byte)(byteRate & 0xff);
            header[29] = (byte)((byteRate >> 8) & 0xff);
            header[30] = (byte)((byteRate >> 16) & 0xff);
            header[31] = (byte)((byteRate >> 24) & 0xff);
            header[32] = (byte)(2 * 16 / 8); // block align
            header[33] = 0;
            header[34] = 16; // bits per sample
            header[35] = 0;
            header[36] = (byte)'d';
            header[37] = (byte)'a';
            header[38] = (byte)'t';
            header[39] = (byte)'a';
            header[40] = (byte)(totalAudioLen & 0xff);
            header[41] = (byte)((totalAudioLen >> 8) & 0xff);
            header[42] = (byte)((totalAudioLen >> 16) & 0xff);
            header[43] = (byte)((totalAudioLen >> 24) & 0xff);

            bWriter.Write(header, 0, 44);
        }
        #endregion
    }
}