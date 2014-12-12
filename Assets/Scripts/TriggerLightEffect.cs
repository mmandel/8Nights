//
//  Trigger LightEffect from MIDI events
//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TriggerLightEffect : MonoBehaviour 
{
   public EightNightsMgr.GroupID MIDIGroup; //what midi stream do we listen to?

   public TriggerMode TriggerRule = TriggerMode.Sequential; //how do we pick an effect to trigger?

   public bool ApplyNoteVelocity = false; //scale effect intensity with note velocity
   [Range(0.0f, 1.0f)]
   public float MinNoteVelocity = 0.0f;

   public EffectEntry ButtonEffect = null;

   public EffectEntry[] EffectsToTrigger = new EffectEntry[1]; //the actual effects

   public MIDINoteMapping[] NoteMappings = new MIDINoteMapping[0]; //optional mappings from midi notes to particular effects

   private int _lastPickedIdx = -1;
   private int _lastMIDINote = -1;
   private List<EffectEntry> _lastPickedEffects = new List<EffectEntry>();

   public enum TriggerMode
   {
      AllAtOnce = 0, //we just blast them all every time
      Sequential, //we always trigger one effect after the other, and loop around
      Random,    //we randomly pick an effect to trigger every time
      FollowPitch //we try to move forward + backward through the effects based on the pitch contour of the MIDI notes
   }

   [System.Serializable]
   public class EffectEntry
   {
      public LightEffect LightEffectToTrigger;
      public bool EnableLightOverride = false;
      public EightNightsMgr.LightID LightOverride;

      private GameObject _spawnedObj = null;


      public void Trigger(TriggerLightEffect parentEffect, bool forceLooping = false, EightNightsMIDIMgr.EightNightsMIDIEventArgs midiEvent = null)
      {
         //if we have a looping effect playing, ignore...
         if (_spawnedObj != null)
            return;

         if (LightEffectToTrigger != null)
         {
            //instatiate new effect and override light if specified
            GameObject spawnedLightObj = Instantiate(LightEffectToTrigger.gameObject) as GameObject;
            spawnedLightObj.transform.parent = parentEffect.transform;
            LightEffect spawnedLightEffect = spawnedLightObj.GetComponent<LightEffect>();
            if (EnableLightOverride)
            {
               foreach (LightEffect.EffectKeyframe k in spawnedLightEffect.Keyframes)
               {
                  foreach (LightEffect.LightState s in k.LightKeys)
                  {
                     s.Light = LightOverride;
                  }
               }
            }
            spawnedLightEffect.LightGroup = parentEffect.MIDIGroup;
            spawnedLightEffect.MasterFader = parentEffect.ApplyNoteVelocity ? Mathf.Lerp(parentEffect.MinNoteVelocity, 1.0f, midiEvent.Velocity) : 1.0f;
            spawnedLightEffect.AutoTrigger = true;            
            spawnedLightEffect.TriggerEffect(); //redundant, I know
            if (forceLooping)
            {
               KillEffect(); //kill any previous version of this effect

               spawnedLightEffect.Loop = true;
               _spawnedObj = spawnedLightObj;
               spawnedLightEffect.AutoDestroy = false;
               spawnedLightEffect.FadeWithStemVolume = false;
               spawnedLightEffect.FadeWithButtonCrescendo = true;
              
            }
            else //if we aren't looping then we auto destroy
            {
               spawnedLightEffect.FadeWithStemVolume = true;
               spawnedLightEffect.FadeWithButtonCrescendo = false;
               spawnedLightEffect.AutoDestroy = true;
               _spawnedObj = null;

               //Debug.Log("Note " + midiEvent.MidiNote + " velocity: " + midiEvent.Velocity);

               //send out event that we're triggering an effect on a particular light
               if ((EightNightsMgr.Instance != null) && (midiEvent != null))
               {
                  //assume all the lights its driving are the same
                  EightNightsMgr.LightID lID = spawnedLightEffect.Keyframes[0].LightKeys[0].Light;
                  //queue this up to send out once latency is elapsed
                  LightTriggerEvent delayedEvent = new LightTriggerEvent();
                  delayedEvent.Group = parentEffect.MIDIGroup;
                  delayedEvent.Light = lID;
                  delayedEvent.BeatTime = midiEvent.NoteBeat;
                  parentEffect.AddDelayedEvent(delayedEvent);
               }
            }

            //LightEffectToTrigger.TriggerEffect();
         }
      }

      public void KillEffect()
      {
         if (_spawnedObj != null)
         {
            DestroyObject(_spawnedObj);
            _spawnedObj = null;
         }
      }
   }

   //an event to send out at the time a light effect is expected to trigger (assuming latency has elapsed)
   public class LightTriggerEvent
   {
      public float BeatTime = 0.0f;
      public EightNightsMgr.GroupID Group;
      public EightNightsMgr.LightID Light;
   }

   private List<LightTriggerEvent> _triggeredEventsToTrack = new List<LightTriggerEvent>();
   public void AddDelayedEvent(LightTriggerEvent e) { _triggeredEventsToTrack.Add(e); }

   [System.Serializable]
   public class MIDINoteMapping
   {
      public int[] MIDINotes;
      public int EffectIdxToTrigger = 0;
   }

	void Start () 
   {
      if (EightNightsMIDIMgr.Instance != null)
      {

         bool isHue = false;
         //just assume that all the lights we control will be all hue or all light jams and subscribe accordingly
         foreach (EffectEntry e in EffectsToTrigger)
         {
            if(e.LightEffectToTrigger == null)
               continue;
            EightNightsMgr.LightID sampleLight = e.EnableLightOverride ? e.LightOverride : e.LightEffectToTrigger.Keyframes[0].LightKeys[0].Light;
            if (EightNightsMgr.Instance.IsHueLight(MIDIGroup, sampleLight))
            {
               isHue = true;
               break;
            }
         }
         if(isHue)
            EightNightsMIDIMgr.Instance.OnHueNoteOn += OnLightMIDIEvent;
         else
            EightNightsMIDIMgr.Instance.OnLightJamsNoteOn += OnLightMIDIEvent;
      }

      //register to find out about button crescendos
      if (ButtonSoundMgr.Instance != null)
      {
         ButtonSoundMgr.Instance.OnCrescendoBegin += OnCrescendoBegin;
         ButtonSoundMgr.Instance.OnCrescendoEnd   += OnCrescendoEnd;
      }
	}

   void Update()
   {
      //process delayed events
      if (EightNightsMgr.Instance != null)
      {
         float curBeat = BeatClock.Instance.elapsedBeats;

         List<LightTriggerEvent> eventsToRemove = new List<LightTriggerEvent>();
         foreach (LightTriggerEvent e in _triggeredEventsToTrack)
         {
            if (curBeat >= e.BeatTime)
            {
               EightNightsMgr.Instance.SendLightTriggeredEvent(e.Group, e.Light);
               eventsToRemove.Add(e);
            }
         }

         foreach (LightTriggerEvent e in eventsToRemove)
            _triggeredEventsToTrack.Remove(e);
      }
   }

   void OnCrescendoBegin(object sender, ButtonSoundMgr.ButtonCrescendoEventArgs e)
   {
      if (e.Group == MIDIGroup)
      {
         Debug.Log("Crescendo BEGIN for group '" + e.Group.ToString());

         //trigger looping button effect
         if (ButtonEffect != null)
            ButtonEffect.Trigger(this, true, null);

      }
   }

   void OnCrescendoEnd(object sender, ButtonSoundMgr.ButtonCrescendoEventArgs e)
   {
      if (e.Group == MIDIGroup)
      {
         Debug.Log("Crescendo END for group '" + e.Group.ToString());

         //stop looping button effect
         if (ButtonEffect != null)
         {
            //reset all lights in group, so nothing lingers...
            EightNightsMgr.Instance.SetAllLightsInGroup(MIDIGroup, 0.0f);
            ButtonEffect.KillEffect();
         }
      }
   }

   bool IsGroupPlaying()
   {
      float groupFader = (EightNightsAudioMgr.Instance != null) ? EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(MIDIGroup) : 1.0f;
      return groupFader > 0.0f;
   }

   List<EffectEntry> PickEffects(EightNightsMIDIMgr.EightNightsMIDIEventArgs midiEvent)
   {
      if ((EffectsToTrigger != null) && (EffectsToTrigger.Length > 0))
      {
         _lastPickedEffects.Clear();

         //TODO: check MIDI mappings!
         MIDINoteMapping midiMap = null;
         foreach (MIDINoteMapping m in NoteMappings)
         {
            if (midiMap != null)
               break;
            foreach (int note in m.MIDINotes)
            {
               if (note == midiEvent.MidiNote)
               {
                  midiMap = m;
                  break;
               }
            }
         }

         if (midiMap != null)
         {
            _lastMIDINote = midiEvent.MidiNote;
            _lastPickedIdx = midiMap.EffectIdxToTrigger;
            //Debug.Log("Mapped MIDI note " + _lastMIDINote + " to effect: " + _lastPickedIdx );
            _lastPickedEffects.Add(EffectsToTrigger[_lastPickedIdx]);
            return _lastPickedEffects;
         }
         else if (TriggerRule == TriggerMode.Sequential)
         {
            _lastPickedIdx = (_lastPickedIdx + 1) % EffectsToTrigger.Length;
            _lastPickedEffects.Add(EffectsToTrigger[_lastPickedIdx]);
            return _lastPickedEffects;
         }
         else if (TriggerRule == TriggerMode.AllAtOnce)
         {
            _lastPickedIdx = -1;
            int i = 0;
            foreach (EffectEntry e in EffectsToTrigger)
            {
               _lastPickedEffects.Add(EffectsToTrigger[i]);
               i++;
            }
            return _lastPickedEffects;
         }
         else if (TriggerRule == TriggerMode.Random)
         {
            _lastPickedIdx = Random.Range(0, EffectsToTrigger.Length);
            _lastPickedEffects.Add(EffectsToTrigger[_lastPickedIdx]);
            return _lastPickedEffects;
         }
         else if (TriggerRule == TriggerMode.FollowPitch)
         {
            if (_lastMIDINote == -1)
               _lastPickedIdx = 0;
            else
            {
               if (midiEvent.MidiNote > _lastMIDINote) //higher note, move up
                  _lastPickedIdx = (_lastPickedIdx + 1) % EffectsToTrigger.Length;
               else if (midiEvent.MidiNote < _lastMIDINote) //lower note, move down
               {
                  _lastPickedIdx--;
                  if (_lastPickedIdx < 0)
                     _lastPickedIdx = EffectsToTrigger.Length - 1;
               }
            }
            _lastMIDINote = midiEvent.MidiNote;
            //Debug.Log("FollowPitch mapped note " + _lastMIDINote + " to effect: " + _lastPickedIdx);

            _lastPickedEffects.Add(EffectsToTrigger[_lastPickedIdx]);
            return _lastPickedEffects;
         }
      }

      return null;
   }

   void OnLightMIDIEvent(object sender, EightNightsMIDIMgr.EightNightsMIDIEventArgs e)
   {
      if ((e.Group == MIDIGroup) && IsGroupPlaying() && !ButtonSoundMgr.Instance.IsGroupCrescendoing(MIDIGroup))
      {
         //pick an effect to trigger
         List<EffectEntry> effectsToTrigger = PickEffects(e);
         if (effectsToTrigger != null)
         {
            foreach (EffectEntry effectEntry in effectsToTrigger)
            {
               if (effectEntry.LightEffectToTrigger != null)
                  effectEntry.Trigger(this, false, e);
            }
         }
      }
   }
	
}
