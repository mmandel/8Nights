//
// Deals with triggering button sounds for each group
//

using UnityEngine;
using System.Collections;
using System;


public class ButtonSoundMgr : MonoBehaviour 
{
   public ButtonConfig[] ButtonConfigs;

   public static ButtonSoundMgr Instance { get; private set; }

   //events
   public event ButtonCrescendoHandler OnCrescendoBegin;
   public event ButtonCrescendoHandler OnCrescendoEnd; 
   public class ButtonCrescendoEventArgs : EventArgs
   {
      public ButtonCrescendoEventArgs(EightNightsMgr.GroupID g, float ct) { Group = g; CrescendoTime = ct; }
      public EightNightsMgr.GroupID Group;
      public float CrescendoTime = 0.0f;
   }
   public delegate void ButtonCrescendoHandler(object sender, ButtonCrescendoEventArgs e);

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
            Debug.Log(" CurBeat = " + _scheduleStartBeat + " next DownBeat: " + _nextDownBeat + " beatsTillDownBeat = " + beatsTillNextDownBeat);

            float crescendoTime = (60.0f / BeatClock.Instance.bpm) * beatsTillNextDownBeat;
            _scheduleEndTime = _scheduleStartTime + crescendoTime;

            ButtonSoundMgr.Instance.SendCrescendoBeginEvent(Group, crescendoTime);
         }
      }

      public float CrescendoProgress()
      {
         return _crescendoProgress;
      }

      public float CrescendoTimeRemaining()
      {
         if (_scheduleStartTime < 0.0f)
            return 0.0f;
         else
         {
            float curTime = BeatClock.Instance.elapsedSecs;
            float timeRemaining = _scheduleEndTime - curTime;
            if (timeRemaining < 0.0f)
               timeRemaining = 0.0f;
            return timeRemaining;
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

         //store crescendo progress
         if (_scheduleStartTime < 0.0f)
            _crescendoProgress = 0.0f;
         else
         {
            float curTime = BeatClock.Instance.elapsedSecs;
            float prevProgress = _crescendoProgress;
            _crescendoProgress = Mathf.InverseLerp(_scheduleStartTime, _scheduleEndTime, curTime);

            //done? then send out event
            if (!Mathf.Approximately(prevProgress, 1.0f) && Mathf.Approximately(_crescendoProgress, 1.0f))
               ButtonSoundMgr.Instance.SendCrescendoEndEvent(Group);
         }
      }

      private bool _scheduling = false;
      private float _scheduleStartTime = -1.0f;
      private float _scheduleEndTime = -1.0f;
      private float _scheduleStartBeat = -1.0f;
      private float _nextDownBeat = -1.0f;

      private float _crescendoProgress = 0.0f;
   }

   void Awake()
   {
      Instance = this;
   }

   public bool IsGroupCrescendoing(EightNightsMgr.GroupID group)
   {
      float progress = GetCrescendoProgressForGroup(group);
      return (progress > 0.0f) && (progress < 1.0f);
   }

   public float GetCrescendoProgressForGroup(EightNightsMgr.GroupID group)
   {
      ButtonConfig c = FindButtonConfig(group);
      if (c != null)
         return c.CrescendoProgress();

      return 0.0f;
   }

   public float GetCrescendoTimeRemainaingForGroup(EightNightsMgr.GroupID group)
   {
      ButtonConfig c = FindButtonConfig(group);
      if (c != null)
         return c.CrescendoTimeRemaining();

      return 0.0f;
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
   
   public void SendCrescendoBeginEvent(EightNightsMgr.GroupID group, float crescendoTime)
   {
      if (OnCrescendoBegin != null)
         OnCrescendoBegin(this, new ButtonCrescendoEventArgs(group, crescendoTime));
   }

   public void SendCrescendoEndEvent(EightNightsMgr.GroupID group)
   {
      if (OnCrescendoEnd != null)
         OnCrescendoEnd(this, new ButtonCrescendoEventArgs(group, 0.0f));
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
