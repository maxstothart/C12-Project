using Microsoft.VisualBasic;
using NAudio.CoreAudioApi; // This is the one that contains the ShareMode enum
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Audio
{
    public class Music
    {
        public static string GetNoteFromFrequency(float frequency)
        {
            if (frequency <= 0) return "Unknown";

            // 1. Calculate how many semitones away from A4 (440Hz)
            double semitonesFromA4 = 12 * Math.Log(frequency / 440.0, 2);

            // 2. Round to the nearest whole semitone
            int noteIndex = (int)Math.Round(semitonesFromA4);

            // 3. Names of the 12 notes in an octave
            string[] noteNames = { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };

            // 4. Find the note name and octave
            // (noteIndex % 12) handles the note name, +48 handles the octave offset from MIDI
            int index = (noteIndex % 12 + 12) % 12;
            int octave = (int)Math.Floor((noteIndex + 9) / 12.0) + 4;

            return $"{noteNames[index]}{octave}";
        }
    }
    public class AudioObject
    {
        public Dictionary<int, float[]> AudioData = new Dictionary<int, float[]>();
        public int SampleRate;
        public Decimal DurationSec;
        public String Duration;
        public AudioObject(float[] Data, int SR)
        {
            AudioData.Add(0, Data);
            SampleRate = SR;
            DurationSec = (Decimal)Data.Length / SR;
            Duration = $"{(int)DurationSec / 60}:{(int)DurationSec % 60m}:{(DurationSec % 1m).ToString()[2..5]}";
        }
        public AudioObject(Dictionary<int, List<float>> Data, int SR)
        {
            for (int i = 0; i < Data.Count; i++)
            {
                AudioData.Add(i, Data[i].ToArray());
            }
            SampleRate = SR;
            DurationSec = (Decimal)Data[0].Count / SR;
            Duration = $"{(int)DurationSec / 60}:{(int)DurationSec % 60m}:{(DurationSec % 1m).ToString()[2..5]}";
        }
        public AudioObject(String fname, int? EndSample = null)
        {
            var w = new Wav(fname, EndSample);
            for (int i = 0; i < w.Data.Count; i++)
            {
                AudioData.Add(i, w.Data[i].ToArray());
            }
            SampleRate = w.fmt["SampleRate"];
            DurationSec = w.DurationSec;
            Duration = w.Duration;
        }
        public float[] GetInterleaved()
        {
            List<float> Output = new List<float>();
            for (int i = 0; i < AudioData[0].Length; i++)
            {
                for (int j = 0; j < AudioData.Count; j++)
                {
                    Output.Add(AudioData[i][j]);
                }
            }
            return Output.ToArray();
        }
        public void Reverse(int StartSample = 0, int? EndSample = null)
        {
            if (!EndSample.HasValue) { EndSample = AudioData[0].Length - 1; }
            Dictionary<int, List<float>> newData = new Dictionary<int, List<float>>();

            for (int i = 0; i < AudioData.Count; i++)
            {
                newData.Add(i, new List<float>());
                for (int j = EndSample.Value; j > StartSample; j--)
                {
                    newData[i].Add(AudioData[i][j]);
                }
            }
            for (int i = 0; i < AudioData.Count; i++)
            {
                AudioData[i] = newData[i].ToArray();
            }
        }
    }
    public class Wav
    {
        public Dictionary<String, byte[]> Chunks;
        public Dictionary<String, int> fmt;
        public Dictionary<int, List<float>> Data;
        public decimal DurationSec;
        public String Duration;
        public Wav(String fname, int? Length = null)
        {
            var br = new BinaryReader(File.Open(fname, FileMode.Open));

            Chunks = new Dictionary<string, byte[]>();
            fmt = new Dictionary<string, int>();

            if (Encoding.ASCII.GetString(br.ReadBytes(4)) == "RIFF")
            {
                br.ReadBytes(4);
                if (Encoding.ASCII.GetString(br.ReadBytes(4)) == "WAVE")
                {
                    while (true)
                    {
                        if (br.BaseStream.Length < br.BaseStream.Position + 8) { break; }

                        String CID = new String(br.ReadChars(4));
                        uint CSize = br.ReadUInt32();
                        Console.WriteLine(CID + ", " + CSize);
                        Chunks.Add(CID, br.ReadBytes(((int)CSize)));
                    }
                }
            }

            //Read "fmt " Chunk
            br = new BinaryReader(new MemoryStream(Chunks["fmt "]));
            fmt.Add("AudioFormat", (int)br.ReadInt16());
            fmt.Add("NumChannels", (int)br.ReadInt16());
            fmt.Add("SampleRate", (int)br.ReadUInt32());
            fmt.Add("ByteRate", (int)br.ReadUInt32());
            fmt.Add("BlockAlign", (int)br.ReadInt16());
            fmt.Add("BPS", (int)br.ReadInt16());

            //Read "data" Chunk
            br = new BinaryReader(new MemoryStream(Chunks["data"]));

            Data = new Dictionary<int, List<float>>();
            for (int i = 0; i < fmt["NumChannels"]; i++)
            {
                Data[i] = new List<float>();
            }
            while (br.BaseStream.Length > br.BaseStream.Position + 4)
            {
                if (Length != null && Length.Value * 4 * fmt["SampleRate"] <= br.BaseStream.Position) { break; }

                switch (fmt["BPS"])
                {
                    case 16:
                        for (int i = 0; i < fmt["NumChannels"]; i++)
                        {
                            Data[i].Add(br.ReadInt16() / 32768f);
                        }
                        break;
                    case 24:
                        for (int i = 0; i < fmt["NumChannels"]; i++)
                        {
                            byte[] sampleBytes = br.ReadBytes(3);
                            int sampleInt = (sampleBytes[0] << 8) | (sampleBytes[1] << 16) | (sampleBytes[2] << 24);
                            Data[i].Add(sampleInt / 2147483648f);
                        }
                        break;
                    case 32:
                        for (int i = 0; i < fmt["NumChannels"]; i++)
                        {
                            Data[i].Add(br.ReadSingle());
                        }
                        break;
                }
            }

            DurationSec = Data[0].Count * 2 / (decimal)fmt["SampleRate"];
            Duration = TimeSpan.FromSeconds((double)DurationSec).ToString(@"mm\:ss\.fff");
        }
        public void generateHeader(Stream File, int filesize)
        {
            BinaryWriter bw = new BinaryWriter(File);
            bw.Write("RIFF");
            bw.Write((UInt32)filesize);
            bw.Write("WAVE");


            bw.Write("fmt ");
            bw.Write((Int16)16);

            bw.Write((Int16)fmt["AudioFormat"]);
            bw.Write((Int16)fmt["NumChannels"]);
            bw.Write((UInt32)fmt["SampleRate"]);
            bw.Write((UInt32)fmt["ByteRate"]);
            bw.Write((Int16)fmt["BlockAlign"]);
            bw.Write((Int16)fmt["BPS"]);
        }


    }
    public class Player
    {
        WasapiOut outputDevice;
        public Player()
        {
            outputDevice = new WasapiOut(AudioClientShareMode.Shared, 100);
        }
        public Player(MMDevice selectedDevice)
        {
            outputDevice = new WasapiOut(
                selectedDevice,
                AudioClientShareMode.Shared,
                useEventSync: true,
                latency: 100
            );
        }
        public void Play(RawSourceWaveStream Data)
        {
            outputDevice.Init(Data);
            Console.WriteLine("Playing File");
            outputDevice.Play();

            // Wait for it to finish
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine("Finished File");
        }

        public RawSourceWaveStream ToStream(float[] Data, int SampleRate, int Channels = 2)
        {
            byte[] byteBuffer = new byte[Data.Length * 4];
            Buffer.BlockCopy(Data, 0, byteBuffer, 0, byteBuffer.Length);
            return new RawSourceWaveStream(new MemoryStream(byteBuffer), WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels));
        }
        public RawSourceWaveStream ToStream(Dictionary<int, float[]> Data, int SampleRate)
        {
            List<float> Output = new List<float>();
            for (int i = 0; i < Data[0].Length; i++)
            {
                for (int j = 0; j < Data.Count; j++)
                {
                    Output.Add(Data[j][i]);
                }
            }
            var InterlacedData = Output.ToArray();

            byte[] byteBuffer = new byte[InterlacedData.Length * 4];
            Buffer.BlockCopy(InterlacedData, 0, byteBuffer, 0, byteBuffer.Length);

            return new RawSourceWaveStream(new MemoryStream(byteBuffer), WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Data.Count));
        }
    }
}

