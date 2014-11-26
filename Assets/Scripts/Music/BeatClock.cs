//
//  Keeps track of the current beat time
//

using UnityEngine;
using System.Collections;
using System;

public class BeatClock : MonoBehaviour {

	public enum Quantization
	{
		kSixteenth,
		kEighth,
		kBeat,
		kMeasure,
      kHalf
	}

	//Singleton
   public static BeatClock Instance { get;  private set; }

   //public inputs
   public float bpm = 120.0f;
   public int beatsPerMeasure = 4; //time sig (assuming quarter note gets the beat tho)
   [Tooltip("Use this to adjust sync issues of audio to visuals")]
   public int LatencyMs = 0; 

   //public outputs
   public float elapsedSecs = 0.0f;
   public float elapsedBeats = 0.0f;
   public float deltaBeats = 0.0f;
   public float deltaSecs = 0.0f;
   public int curBeat;
   public int cur16th;
   public int cur8th;
   public int curMeasure;
   public int curHalf;
   public int curTick;

   //private
   private const float kTicksPerBeat = 480.0f;
   private float startTime;
   private AnimationCurve beatMap = new AnimationCurve();
   private float prevBeat;
   private float prevSecs;

   private void ResetClock()
   {
      startTime = Time.time;
      prevBeat = 0.0f;
      prevSecs = 0.0f;
      elapsedBeats = 0.0f;
      elapsedSecs = 0.0f;
      deltaBeats = 0.0f;
      curBeat = 0;
      curMeasure = 0;
      curTick = 0;
      cur16th = 0;
      cur8th = 0;
	}

   // Add a beat to the current song's beat map.
   public void AddBeat(int index, float time) {
      beatMap.AddKey (time, (float)index);
      
      //beatMap.SmoothTangents(index, 0f); //do we need this?
   }

   public void ClearBeats()
   {
      while (beatMap.length > 0)
         beatMap.RemoveKey(0);
   }
   
   public float SecondsToBeats(float seconds)
   {
      return beatMap.Evaluate(seconds) - 1.0f;
   }

   public float LastBeat()
   {
      return beatMap.keys.Length;
   }

   public float LastBeatInSeconds()
   {
      return beatMap.keys[beatMap.keys.Length - 1].time;
   }
   
   //given a beat #, how many seconds into the song would that be?
   public float BeatsToSeconds(float beat)
   {
      if (beatMap.length > 0) {

         //uh, weird, but to be reversible with SecondsToBeats we gotta do this
         beat += 1.0f;

         //NOTE: assuming there is an entry in the beatmap EVERY beat
         //so we can do this lookup FAST
         int beatNum = Math.Min(Math.Max(0, (int) beat), beatMap.keys.Length - 1);
         int nextBeatNum = Math.Min(Math.Max(0, beatNum + 1), beatMap.keys.Length - 1);
         float frac = beat - (float)((int)beat);

         Keyframe prevK = beatMap.keys[beatNum];
         Keyframe nextK = beatMap.keys[nextBeatNum];

         return Mathf.Lerp(prevK.time, nextK.time, frac);

         /*Keyframe prevK = new Keyframe();
         prevK.value = 0.0f;
         foreach(Keyframe k in beatMap.keys)
         {
            if(k.value >= beat)
            {
               float u = Mathf.InverseLerp(prevK.value, k.value, beat);
               return Mathf.Lerp(prevK.time, k.time, u);
            }
            
            prevK = k;
         }
         return 0.0f;*/
      } else {
         return (60.0f / bpm)* beat;
      }   
   }

   public float GetSongTime()
   {
      float songTime = 0.0f;
      if (EightNightsAudioMgr.Instance != null) //get elapsed secs from song if we have one
         songTime = EightNightsAudioMgr.Instance.GetElapsedSecs();
      else //normal elapsed secs
         songTime = Time.time - startTime;

      return songTime + LatencySecs();
   }

   public float SecsPerBeat()
   {
      return (60.0f / bpm);
   }

	// Use this for initialization
	void Start () {
      ResetClock();
	}

   float LatencySecs()
   {
      return (LatencyMs / 1000.0f);
   }

   /*public BeatClock()
   {
      Instance = this;
      ResetClock();
   }*/

	void Awake() 
	{
      ResetClock();
		Instance = this;
	}

	// Update is called once per frame
	void Update () {
      elapsedSecs = GetSongTime();

      if (beatMap.length > 0) {
         elapsedBeats = SecondsToBeats(elapsedSecs);
         if(elapsedBeats < 0.0)
         	elapsedBeats = 0.0f;
         if(elapsedBeats>0 && elapsedBeats<(beatMap.length-1)) {
            float lastBeatTime = beatMap[Mathf.FloorToInt(elapsedBeats)].time;
            float nextBeatTime = beatMap[1+Mathf.FloorToInt(elapsedBeats)].time;
            bpm = 60f / (nextBeatTime-lastBeatTime);
         }
      } else {
         elapsedBeats = elapsedSecs * bpm / 60.0f;
      }

      deltaBeats = elapsedBeats - prevBeat;
      if (deltaBeats < 0.0f)
         deltaBeats = 0.0f;

      deltaSecs = elapsedSecs - prevSecs;
      if (deltaSecs < 0.0f)
         deltaSecs = 0.0f;
      
      //Debug.Log (elapsedSecs + " should equal " + BeatsToSeconds(elapsedBeats));

      int newBeat = (int)elapsedBeats % beatsPerMeasure;
      int newMeasure = (int)(elapsedBeats / beatsPerMeasure);
      float fractionalBeat = elapsedBeats - (int)elapsedBeats;
      int newTick = (int)(fractionalBeat * kTicksPerBeat);
      int new16th = (int)(elapsedBeats * 4.0f)%16;
      int new8th = (int)(elapsedBeats * 2.0f)%8;
      int newHalf = (int)(elapsedBeats / 2.0f);

      //update properties and send out event notifications
      if (newMeasure != curMeasure)
      {
         curMeasure = newMeasure;
      }
      if (newBeat != curBeat)
      {
         curBeat = newBeat;
      }
      if (new8th != cur8th)
      {
         cur8th = new8th;
      }
      if (new16th != cur16th)
      {
         cur16th = new16th;
      }
      if (newHalf != curHalf)
      {
         curHalf = newHalf;
      }
      if (newTick != curTick)
         curTick = newTick;

      prevBeat = elapsedBeats;
      prevSecs = elapsedSecs;
	}

	public float GetProgressToNextQuantum(Quantization q)
	{
		switch (q) 
		{
		case Quantization.kMeasure:
         return (elapsedBeats / beatsPerMeasure) % 1.0f;
      case Quantization.kHalf:
         return (2.0f * elapsedBeats / beatsPerMeasure) % 1.0f;
		case Quantization.kBeat:
			return elapsedBeats%1.0f;
		case Quantization.kEighth:
			return (elapsedBeats*2.0f)%1.0f;
		case Quantization.kSixteenth:
			return (elapsedBeats*4.0f)%1.0f;
		default:
			Debug.Log ("Unknown quantization");
			return 0.0f;
		}
	}


   public void SetAsGlobalBeatProvider()
   {
      Instance = this;
   }

   static public float QuantizationToBeats(BeatClock.Quantization q)
   {
      switch (q)
      {
         case Quantization.kMeasure:
            return 4.0f;
         case Quantization.kBeat:
            return 1.0f;
         case Quantization.kEighth:
            return .5f;
         case Quantization.kSixteenth:
            return .25f;
         case Quantization.kHalf:
            return 2.0f;
         default:
            Debug.Log("Unknown quantization");
            return 1.0f;
      }
   }
}
