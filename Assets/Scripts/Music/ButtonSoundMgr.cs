//
// Deals with triggering button sounds for each group
//

using UnityEngine;
using System.Collections;

public class ButtonSoundMgr : MonoBehaviour 
{
   public ButtonConfig[] ButtonConfigs;

   [System.Serializable]
   public class ButtonConfig
   {
      public EightNightsMgr.GroupID Group;
      public SimpleMusicPlayer MusicPlayer;
      public bool AlignToDownBeat = false;
      public float DownBeatMoment = 1.5f;

      public void ScheduleForDownBeat() 
      { 
         if (!_scheduling)
         { 
            _scheduling = true; 
            _scheduleStartTime = BeatClock.Instance.elapsedSecs; 
            _scheduleStartBeat = BeatClock.Instance.elapsedBeats;

            float beatsPerMeasure = (float)BeatClock.Instance.beatsPerMeasure;
            float beatsTillNextDownBeat = (beatsPerMeasure - (_scheduleStartBeat % beatsPerMeasure));
            _nextDownBeat = _scheduleStartBeat + beatsTillNextDownBeat;
            //Debug.Log(" CurBeat = " + _scheduleStartBeat + " next DownBeat: " + _nextDownBeat + " beatsTillDownBeat = " + beatsTillNextDownBeat);
         } 
      }

      public void Update()
      {
         if (_scheduling)
         {
            float curBeat = BeatClock.Instance.elapsedBeats;
            float beatsTillNextDownBext = (_nextDownBeat - curBeat);
            float secsTillNextDownBeat = (60.0f / BeatClock.Instance.bpm) * beatsTillNextDownBext;
            if (secsTillNextDownBeat <= DownBeatMoment) //should we trigger yet?
            {
               //how far do we fast forward in?
               float startTime = DownBeatMoment - secsTillNextDownBeat;

               MusicPlayer.audio.Stop(); //TODO: fade in?
               MusicPlayer.audio.timeSamples = (int)(startTime * (float)MusicPlayer.audio.clip.frequency);
               MusicPlayer.Play();

               _scheduling = false;
            }
         }
      }

      private bool _scheduling = false;
      private float _scheduleStartTime = -1.0f;
      private float _scheduleStartBeat = -1.0f;
      private float _nextDownBeat = -1.0f;
   }

   public void TriggerSoundForGroup(EightNightsMgr.GroupID group)
   {
      ButtonConfig c = FindButtonConfig(group);
      if ((c != null) && (c.MusicPlayer != null))
      {

         if (!c.AlignToDownBeat) //just fire off one-off
         {
            //TODO: should spawn these things so we don't have to cut anything off
            c.MusicPlayer.Stop();
            c.MusicPlayer.Play();
         }
         else
         {
            c.ScheduleForDownBeat();
         }
      }
   }

   void Update()
   {
      foreach (ButtonConfig c in ButtonConfigs)
      {
         c.Update();
      }
   }

   ButtonConfig FindButtonConfig(EightNightsMgr.GroupID group)
   {
      foreach (ButtonConfig c in ButtonConfigs)
      {
         if (c.Group == group)
            return c;
      }
      return null;
   }
}
