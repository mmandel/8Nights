//
// Exposes audio spectrogram data each group
// Currently these are calculated offline and loaded for evaluation at runtime
//

using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class SpectrogramMgr : MonoBehaviour
{
   public SpectroConfig[] SpectroConfigs;

   public static SpectrogramMgr Instance { get; private set; }

   [System.Serializable]
   public class SpectroConfig
   {
      public EightNightsMgr.GroupID Group;
      public string PathToSpecFile = "Spectrograms/Humans 1.spec";
      public bool DebugDraw = false;

      public static float[] ReadFloats(BinaryReader b)
      {
         int arrayLen = b.ReadInt32();
         float[] data = new float[arrayLen];
         for (int i = 0; i < arrayLen; i++)
            data[i] = b.ReadSingle();
         return data;
      }

      public static string ReadString(BinaryReader b)
      {
         int strLen = b.ReadInt32();
         char[] chars = b.ReadChars(strLen);
         string str = new string(chars);
         return str;
      }

      public void Load()
      {
         string filePath = Application.streamingAssetsPath + "/" + PathToSpecFile;

         FileStream fs = File.OpenRead(filePath);
         BinaryReader b = new BinaryReader(fs);

         int numCurves = b.ReadInt32();
         float timeBetweenSamples = b.ReadSingle();
         _spectroCurves = new AnimationCurve[numCurves];
         for (int i = 0; i < numCurves; i++)
         {
            AnimationCurve curve = new AnimationCurve();
            _spectroCurves[i] = curve;

            float time = 0.0f;
            float[] curveValues = ReadFloats(b);
            for (int j = 0; j < curveValues.Length; j++)
            {
               curve.AddKey(time, curveValues[j]);
               time += timeBetweenSamples;
            }
         }

         _fftFreqBands = ReadFloats(b);
      }

      //draw spectrogram
      public void GUIDraw()
      {
         if (!DebugDraw)
            return;

         Vector2 startPos = new Vector2(10, 150);
         float boxWidth = 20;
         float boxSpacing = 3;
         float boxHeight = 100;

         Vector2 curPos = startPos;
         int idx = 0;
         float fftTime = SpectrogramMgr.Instance.GetFFTTime();
         float stemVolume = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(Group);
         foreach (AnimationCurve c in _spectroCurves)
         {
            float curveVal = Mathf.Clamp01(c.Evaluate(fftTime));
            GUI.Box(new Rect(curPos.x, curPos.y, boxWidth, stemVolume*curveVal * boxHeight + 20), idx.ToString());
            
            curPos.x += boxWidth + boxSpacing;
            idx++;
         }
      }

      public int NumBands() { return (_spectroCurves != null) ? _spectroCurves.Length : 0; }

      //get info on frequency range of the given band
      public float GetCenterFreq(int idx) { return _fftFreqBands[idx]; }

      //get the intensity of a particular band at the given time
      public float GetBandValue(int idx, float time) { return _spectroCurves[idx].Evaluate(time); }

      AnimationCurve[] _spectroCurves;  //a curve for each bin of the FFT
      float[] _fftFreqBands;
   }

   void Awake()
   {
      Instance = this;
   }

   void Start()
   {
      foreach (SpectroConfig c in SpectroConfigs)
         c.Load();
   }

   public float GetFFTTime()
   {
      float timeToSample = BeatClock.Instance.elapsedSecs;

      //WHY?!
      timeToSample -= BeatClock.Instance.LatencySecs();

      return timeToSample;
   }

   void Update()
   {

   }

   void OnGUI()
   {
      foreach (SpectroConfig c in SpectroConfigs)
         c.GUIDraw();
   }

   public SpectroConfig GetSpectroDataForGroup(EightNightsMgr.GroupID group)
   {
      foreach (SpectroConfig c in SpectroConfigs)
      {
         if (c.Group == group)
            return c;
      }
      return null;
   }
}
