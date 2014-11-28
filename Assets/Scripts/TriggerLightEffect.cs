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


      public void Trigger(TriggerLightEffect parentEffect)
      {
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
            spawnedLightEffect.AutoDestroy = true;
            spawnedLightEffect.AutoTrigger = true;
            spawnedLightEffect.TriggerEffect(); //redundant, I know

            //LightEffectToTrigger.TriggerEffect();
         }
      }
   }

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
	}

   bool IsGroupPlaying()
   {
      float groupFader = (EightNightsAudioMgr.Instance != null) ? EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(MIDIGroup) : 1.0f;
      return groupFader > 0.0f;
   }

   List<EffectEntry> PickEffects(EightNightsMIDIMgr.EightNightsMIDIEventArgs midiEvent)
   {
      if (EffectsToTrigger.Length > 0)
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
      if ((e.Group == MIDIGroup) && IsGroupPlaying())
      {
         //pick an effect to trigger
         List<EffectEntry> effectsToTrigger = PickEffects(e);

         foreach (EffectEntry effectEntry in effectsToTrigger)
         {
            if (effectEntry.LightEffectToTrigger != null)
               effectEntry.Trigger(this);
         }
      }
   }
	
}
