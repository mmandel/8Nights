//
//  Trigger LightEffect from MIDI events
//

using UnityEngine;
using System.Collections;

public class TriggerLightEffect : MonoBehaviour 
{

   public EightNightsMgr.GroupID MIDIGroup;

   public LightEffect[] EffectsToTrigger;

	void Start () 
   {
      if (EightNightsMIDIMgr.Instance != null)
      {
         EightNightsMIDIMgr.Instance.OnHueNoteOn += OnHueMIDIEvent;
         EightNightsMIDIMgr.Instance.OnLightJamsNoteOn += OnLightJamsMIDIEvent;
      }
	}

   bool IsGroupPlaying()
   {
      float groupFader = (EightNightsAudioMgr.Instance != null) ? EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(MIDIGroup) : 1.0f;
      return groupFader > 0.0f;
   }

   void OnHueMIDIEvent(object sender, EightNightsMIDIMgr.EightNightsMIDIEventArgs e)
   {
      if (e.Group == MIDIGroup)
      {
         foreach (LightEffect effect in EffectsToTrigger)
         {
            if ((effect.LightGroup == MIDIGroup) && effect.ControlsHueLight() && IsGroupPlaying())
            {
               effect.TriggerEffect();
            }
         }
      }
   }

   void OnLightJamsMIDIEvent(object sender, EightNightsMIDIMgr.EightNightsMIDIEventArgs e)
   {
      if (e.Group == MIDIGroup)
      {
         foreach (LightEffect effect in EffectsToTrigger)
         {
            if ((effect.LightGroup == MIDIGroup) && effect.ControlsLightJamsLight() && IsGroupPlaying())
            {
               effect.TriggerEffect();
            }
         }
      }
   }   
	
}
