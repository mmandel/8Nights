//
//  Drive a light by processing a signal derived from the corresponding stem's FFT (spectrogram data)
//

using UnityEngine;
using System.Collections;

public class FFTLightEffect : MonoBehaviour 
{
   public EightNightsMgr.GroupID Group;
   public EightNightsMgr.LightID Light;
   public bool FadeWithStemVolume = true;
   public bool DisableDuringCrescendos = true;
   [Range(0, 39)]
   public int MinFFTBin = 0;
   [Range(0, 39)]
   public int MaxFFTBin = 39;

   [Header("ADSR")]
   public bool EnableADSR = false;
   [Range(0, 3)]
   public float Attack = 0.0f;
   [Range(0, 3)]
   public float Hold = 0.0f;
   [Range(0, 3)]
   public float Release = 0.0f;
   [Range(0, 1)]
   public float SmoothingAmt = 0.0f;
   [Range(0, 5)]
   public float Gain = 1.0f;
   public bool AutoDynamicRange = false;
   public float UpdateRangeInterval = 4.0f;
   public float RangeAdjustSpeed = .25f;

   //ADSR state
   private float _prevVal = 0.0f;    //last processed output
   private float _freqRangeMax = 1.0f;
   private float _freqRangeTotal = 1.0f;
   private float _holdRemaining = -1.0f;
   private float _holdVal = -1.0f;
   private float _prevTime = 0.0f;
   //range updating
   private float _targetMin = 0.0f;
   private float _targetMax = 1.0f;
   private float _curMin = 0.0f;
   private float _curMax = 1.0f;
   private float _tmpMin = 1.0f;
   private float _tmpMax = 0.0f;
   private float _timeTillRangeUpdate = -1.0f;
   private float _flatLiningTime = 0.0f;

	void Start () 
   {
	
	}
	
	void Update () 
   {
      if (SpectrogramMgr.Instance == null)
         return;

      if (DisableDuringCrescendos && ButtonSoundMgr.Instance.IsGroupCrescendoing(Group))
         return;

      SpectrogramMgr.SpectroConfig fftData =  SpectrogramMgr.Instance.GetSpectroDataForGroup(Group);
      float timeToSample = BeatClock.Instance.elapsedSecs + EightNightsMgr.Instance.GetLatency(Group, Light);

      //average the frequency bins to get our signal value
      float curSignal = 0.0f;
      int numSamples = (MaxFFTBin - MinFFTBin) + 1;
      for (int i = MinFFTBin; i <= MaxFFTBin; i++)
      {
         curSignal += fftData.GetBandValue(i, timeToSample);
      }
      curSignal = Mathf.Clamp01(Gain * (curSignal / numSamples));

      //fade with volume
      float groupFader = 1.0f;
      if(EightNightsAudioMgr.Instance != null)
      {
         if (FadeWithStemVolume)
            groupFader = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(Group);
      }

      if (EnableADSR)
      {
         curSignal = EvaluateADSR(curSignal);
      }

      curSignal *= groupFader;

      EightNightsMgr.Instance.SetLight(Group, Light, curSignal, EightNightsMgr.Instance.GetDefaultColor(Group), 0.0f);
	}

   float EvaluateADSR(float curSignal)
   {
      float curTime = Time.time;
      float dt = curTime - _prevTime;

      float curVal = curSignal;

      if (AutoDynamicRange)
      {
         //update target dynamic range every so often
         if (_timeTillRangeUpdate > 0.0f)
         {
            //keep track of how long we "flatline" for, which indicates our range is off
            //float remappedVal = Mathf.InverseLerp(_curMin, _curMax, curVal);
            if (Mathf.Approximately(_prevVal, 0.0f) || Mathf.Approximately(_prevVal, 1.0f))
               _flatLiningTime += Time.deltaTime;
            else
               _flatLiningTime = 0.0f;

            if (curVal > _tmpMax)
               _tmpMax = curVal;
            if (curVal < _tmpMin)
               _tmpMin = curVal;
            _timeTillRangeUpdate -= Time.deltaTime;
            if ((_timeTillRangeUpdate <= 0.0f) || (_flatLiningTime > 1.0f))
            {
               if (_tmpMax > _tmpMin)
               {
                  _targetMax = _tmpMax;
                  _targetMin = _tmpMin;

                  //Debug.Log("   ** New TARGET min/max [" + _targetMin + ", " + _targetMax + "] at time = " + Time.time);

                  //make sure we have at least 10% of the full value range, to avoid amplifying noise
                  const float kMinDynamicRange = .1f;
                  float curDynamicRange = Mathf.Abs(_targetMax - _targetMin);
                  if (curDynamicRange < kMinDynamicRange)
                  {
                     float dynamicRangeLeft = kMinDynamicRange - curDynamicRange;
                     float minAdjust = Mathf.Min(.5f * dynamicRangeLeft, _targetMin); //try to adjust min by half the range, if there is room
                     _targetMin = Mathf.Clamp01(_targetMin - minAdjust);
                     dynamicRangeLeft -= minAdjust;
                     _targetMax = Mathf.Clamp01(_targetMax + dynamicRangeLeft); //add the rest to the max and clamp

                     //Debug.Log("     ** Adjusted min/max DYNAMIC RANGE to [" + _targetMin + ", " + _targetMax + "] at time = " + Time.time);
                  }
               }

               //schedule next update
               RescheduleRangeUpdate();
            }
         }
         else
            RescheduleRangeUpdate();

         //move towards target min/max
         float oldMin = _curMin;
         float oldMax = _curMax;
         if (!Mathf.Approximately(_curMax, _targetMax))
         {
            if (_curMax < _targetMax)
               _curMax = Mathf.Min(_curMax + (RangeAdjustSpeed * Time.deltaTime), _targetMax);
            else
               _curMax = Mathf.Max(_curMax - (RangeAdjustSpeed * Time.deltaTime), _targetMax);
         }
         if (!Mathf.Approximately(_curMin, _targetMin))
         {
            if (_curMin < _targetMin)
               _curMin = Mathf.Min(_curMin + (RangeAdjustSpeed * Time.deltaTime), _targetMin);
            else
               _curMin = Mathf.Max(_curMin - (RangeAdjustSpeed * Time.deltaTime), _targetMin);
         }
         //Debug.Log("Min " + oldMin + " -> " + _curMin + "   Max " + oldMax + " -> " + _curMax);

         //remap dynamic range
         curVal = Mathf.InverseLerp(_curMin, _curMax, curVal);
      }


      //smooth
      curVal = _prevVal * SmoothingAmt + curVal * (1.0f - SmoothingAmt);

      //apply attack
      float processedVal = curVal;
      if (curVal - _prevVal > .01f)
      {
         if (Attack > float.Epsilon) //move prev val towards cur val at Attack rate
         {
            processedVal = _prevVal + ((1.0f / Attack) * dt);
            processedVal = Mathf.Clamp(processedVal, 0.0f, curVal);
         }

         //reschedule any holds
         _holdVal = curVal; //this will be our next hold val tho
         _holdRemaining = (Hold > float.Epsilon) ? Hold : -1.0f;
      }
      //apply hold
      else if (_holdRemaining > 0.0f)
      {
         _holdRemaining -= dt;
         processedVal = _holdVal;
         if (_holdRemaining < 0.0) //are we done holding?
            _holdRemaining = -1.0f;
      }
      //apply release
      else
      {
         if (Release > float.Epsilon) //move prev val towards cur val at Release rate
         {
            processedVal = _prevVal - ((1.0f / Release) * dt);
            processedVal = Mathf.Clamp(processedVal, curVal, 1.0f);
         }

         _holdRemaining = -1.0f; //just to be sure...
      }

      _prevVal = processedVal;
      _prevTime = curTime;      


      return _prevVal;
   }

   void RescheduleRangeUpdate()
   {
      _timeTillRangeUpdate = UpdateRangeInterval;
      _tmpMin = 1.0f;
      _tmpMax = 0.0f;
   }
}
