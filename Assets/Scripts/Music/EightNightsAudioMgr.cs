using UnityEngine;
using System.Collections;
using System;

public class EightNightsAudioMgr : MonoBehaviour 
{
   public bool ShowTestUI = true;

   [Space(10)]
   public EightNightsMusicPlayer MusicPlayer;
   public ButtonSoundMgr ButtonSoundManager;
   [Space(10)]
   public MusicTestData MusicTester = new MusicTestData();

   [Header("Tuning Values")]
   public float StemAttackTime = 1.0f;
   public float StemSustainTime = 10.0f;
   public float StemReleaseTime = 3.0f;


   public static EightNightsAudioMgr Instance { get; private set; }

   private GroupStateData[] _groupState = null;
   private GroupStateData _peakGroupState = null;

   //backing loop's state
   public enum StemLoopState
   {
      Off,
      Attacking,
      Sustaining,
      Releasing
   }

   public class GroupStateData
   {
      public EightNightsMgr.GroupID Group;
      public StemLoopState LoopState = StemLoopState.Off;


      public void CaptureTimestamp() { _timeStamp = Time.time; }
      public void SetTimestamp(float t) { _timeStamp = t; }
      public float Timestamp() { return _timeStamp; }
      private float _timeStamp = -1.0f;
   }

   [System.Serializable]
   public class MusicTestData
   {
      public bool EnableTestMode = false;
      [Space(10)]
      [Header("Backing Loop")]
      [Range(0.0f, 1.0f)]
      public float BackingLoopVolume = 1.0f;
      [Header("Group 1")]
      [Range(0.0f, 1.0f)]
      public float Rift1Volume = 0.0f;
      [Range(0.0f, 1.0f)]
      public float Room1Volume = 0.0f;
      [Header("Group 2")]
      [Range(0.0f, 1.0f)]
      public float Rift2Volume = 0.0f;
      [Range(0.0f, 1.0f)]
      public float Room2Volume = 0.0f;
      [Header("Group 3")]
      [Range(0.0f, 1.0f)]
      public float Rift3Volume = 0.0f;
      [Range(0.0f, 1.0f)]
      public float Room3Volume = 0.0f;
      [Header("Group 4")]
      [Range(0.0f, 1.0f)]
      public float Rift4Volume = 0.0f;
      [Range(0.0f, 1.0f)]
      public float Room4Volume = 0.0f;
   }

   void Awake()
   {
      Instance = this;

      _peakGroupState = new GroupStateData();
      _peakGroupState.LoopState = StemLoopState.Off;

      Array allGroups = Enum.GetValues(typeof(EightNightsMgr.GroupID));
      _groupState = new GroupStateData[allGroups.Length];
      int i = 0;
      foreach (EightNightsMgr.GroupID g in Enum.GetValues(typeof(EightNightsMgr.GroupID)))
      {
         GroupStateData newData = new GroupStateData();
         newData.LoopState = StemLoopState.Off;
         newData.Group = g;
         _groupState[i] = newData;
         i++;
      }
   }

	void Start () 
   {
      MusicPlayer.SetBackingLoopVolume(1.0f);
      MusicPlayer.SetPeakLoopVolume(0.0f);
	}

   public float GetElapsedSecs()
   {
      return MusicPlayer.GetElapsedSecs();
   }

   public bool IsPeakMode()
   {
      return _peakGroupState.LoopState != StemLoopState.Off;
   }

   void ResetAllStems()
   {
      foreach (GroupStateData g in _groupState)
      {
         g.LoopState = StemLoopState.Off;
      }
      _peakGroupState.LoopState = StemLoopState.Off;
   }

   void ResetAllStemTimestamps()
   {
      foreach (GroupStateData g in _groupState)
      {
         g.CaptureTimestamp();
      }
      _peakGroupState.CaptureTimestamp();
   }

   void OnGUI()
   {
      if (!ShowTestUI)
         return;

      Vector2 startPos = new Vector2(10, 10);
      float buttonVSpacing = 30;

      // Room Triggers
         Vector2 groupSize = new Vector2(100, buttonVSpacing*4 + 30);
         GUI.Box(new Rect(startPos.x, startPos.y, groupSize.x, groupSize.y), "Room Triggers");

         //group1
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + buttonVSpacing, groupSize.x - 20, 20), "Group 1"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup1);
         }
         //group2
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 2 * buttonVSpacing, groupSize.x - 20, 20), "Group 2"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup2);
         }
         //group3
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 3*buttonVSpacing, groupSize.x - 20, 20), "Group 3"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup3);
         }
         //group4
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 4 * buttonVSpacing, groupSize.x - 20, 20), "Group 4"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup4);
         }

      // Rift Triggers
         startPos.x += 130;
         GUI.Box(new Rect(startPos.x, startPos.y, groupSize.x, groupSize.y), "Rift Triggers");

         //group1
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + buttonVSpacing, groupSize.x - 20, 20), "Group 1"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup1);
         }
         //group2
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 2 * buttonVSpacing, groupSize.x - 20, 20), "Group 2"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup2);
         }
         //group3
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 3 * buttonVSpacing, groupSize.x - 20, 20), "Group 3"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup3);
         }
         //group4
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 4 * buttonVSpacing, groupSize.x - 20, 20), "Group 4"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup4);
         }

      //kill all button
         startPos.y += 4 * buttonVSpacing  + 40;
         startPos.x -= 55;
         if (GUI.Button(new Rect(startPos.x, startPos.y, groupSize.x - 20, 20), "Reset All"))
         {
            ResetAllStems();
         }

      // text fields for stem tuning params
         startPos.x = Screen.width * .5f - 150;
         startPos.y = 10;
         groupSize = new Vector2(200, buttonVSpacing * 3 + 30);
         GUI.Box(new Rect(startPos.x, startPos.y, groupSize.x, groupSize.y), "Stem Behavior");
         //Attack Time
         startPos.y += buttonVSpacing;
         GUI.Label(new Rect(startPos.x + 10, startPos.y, groupSize.x - 50, 20), "Attack Time: ");
         string attackStr = StemAttackTime.ToString();
         string newAttackStr = GUI.TextField(new Rect(startPos.x + 10 + groupSize.x - 70, startPos.y, 50, 20), attackStr);
         if (!newAttackStr.Equals(attackStr))
         {
            float newAttack = 0.0f;
            if (float.TryParse(newAttackStr, out newAttack))
            {
               StemAttackTime = newAttack;
               ResetAllStemTimestamps();
            }
         }
         //Sustain Time
         startPos.y += buttonVSpacing;
         GUI.Label(new Rect(startPos.x + 10, startPos.y, groupSize.x - 50, 20), "Sustain Time: ");
         string sustainStr = StemSustainTime.ToString();
         string newSustainStr = GUI.TextField(new Rect(startPos.x + 10 + groupSize.x - 70, startPos.y, 50, 20), sustainStr);
         if (!newSustainStr.Equals(sustainStr))
         {
            float newSustain = 0.0f;
            if (float.TryParse(newSustainStr, out newSustain))
            {
               StemSustainTime = newSustain;
               ResetAllStemTimestamps();
            }
         }
         //Release
         startPos.y += buttonVSpacing;
         GUI.Label(new Rect(startPos.x + 10, startPos.y, groupSize.x - 50, 20), "Release Time: ");
         string releaseStr = StemReleaseTime.ToString();
         string newReleaseStr = GUI.TextField(new Rect(startPos.x + 10 + groupSize.x - 70, startPos.y, 50, 20), releaseStr);
         if (!newReleaseStr.Equals(releaseStr))
         {
            float newRelease = 0.0f;
            if (float.TryParse(newReleaseStr, out newRelease))
            {
               StemReleaseTime = newRelease;
               ResetAllStemTimestamps();
            }
         }

      // Test level sliders
         startPos.x = Screen.width - 300;
         startPos.y = 10;

         GUI.Box(new Rect(startPos.x - 30, startPos.y, 310, 350), "Stem Levels");

         startPos.y += buttonVSpacing;

         MusicTester.EnableTestMode = GUI.Toggle(new Rect(startPos.x, startPos.y, 200, 30), MusicTester.EnableTestMode, "Override");

         startPos.y += buttonVSpacing;

         Color origGUIColor = GUI.color;
         Color curGUIColor = origGUIColor;
         if (!MusicTester.EnableTestMode)
            curGUIColor.a = .5f;
         GUI.color = curGUIColor;

         //backing loop
         Rect backingRect = new Rect(startPos.x, startPos.y, 170, 25);
         GUI.Label(backingRect, "Backing: ");
         backingRect.x += 100;
         float backingVol = MusicPlayer.GetBackingLoopVolume();
         float newBackingVol = GUI.HorizontalSlider(backingRect, backingVol, 0.0f, 1.0f);
         if (MusicTester.EnableTestMode) //only sync slider value back if in test mode
            MusicPlayer.SetBackingLoopVolume(newBackingVol);

         startPos.y += buttonVSpacing;

         //peak loop
         Rect peakRect = new Rect(startPos.x, startPos.y, 170, 25);
         GUI.Label(peakRect, "Peak State: ");
         peakRect.x += 100;
         float peakVol = MusicPlayer.GetPeakLoopVolume();
         float newPeakVol = GUI.HorizontalSlider(peakRect, peakVol, 0.0f, 1.0f);
         if (MusicTester.EnableTestMode) //only sync slider value back if in test mode
            MusicPlayer.SetPeakLoopVolume(newPeakVol);

         //loops for each group
         foreach (EightNightsMgr.GroupID g in Enum.GetValues(typeof(EightNightsMgr.GroupID)))
         {
            startPos.y += buttonVSpacing;

            Rect sliderRect = new Rect(startPos.x, startPos.y, 170, 25);
            GUI.Label(sliderRect, g.ToString() + ": ");

            sliderRect.x += 100;

            AudioLayer l = MusicPlayer.GetLayerForGroup(g);
            float curV = MusicPlayer.GetVolumeForGroup(g);
            if (l != null)
            {
               float sliderVol = GUI.HorizontalSlider(sliderRect, curV, 0.0f, 1.0f);
               if(MusicTester.EnableTestMode) //only sync slider value back if in test mode
                  MusicPlayer.SetVolumeForGroup(g, sliderVol);
            }
         }

         GUI.color = origGUIColor;

      // Show cur MBT in bottom left corner
         startPos.x = 10;
         startPos.y = Screen.height - 30;
         string MBTStr = "Beat Time: " + (BeatClock.Instance.curMeasure + 1) + ":" + (BeatClock.Instance.curBeat +1) + ":" + BeatClock.Instance.curTick;
         GUI.Label(new Rect(startPos.x, startPos.y, 170, 25), MBTStr); 
      // Show elapsed time
         startPos.y -= 20;
         int minutes = (int)(BeatClock.Instance.elapsedSecs / 60.0f);
         int secs = (int) (BeatClock.Instance.elapsedSecs % 60.0f);
         string secsStr = (secs < 10) ? "0" + secs : secs.ToString();
         GUI.Label(new Rect(startPos.x, startPos.y, 170, 25), "Elapsed:    " + minutes + ":" + secsStr); 
      
      //Restart Song Button
      //TODO: this code doesn't work yet, so disabling for now...
       /*  startPos.x += 135;
         startPos.y += 10;
         if(GUI.Button(new Rect(startPos.x, startPos.y, 100, 25), "Restart Song"))
         {
            MusicPlayer.Restart();
         }*/
   }

   GroupStateData GetStateForGroup(EightNightsMgr.GroupID group)
   {
      foreach (GroupStateData d in _groupState)
      {
         if (d.Group == group)
            return d;
      }
      return null;
   }

   public void TriggerGroup(EightNightsMgr.GroupID group)
   {
      //TODO: temp behavior
      //we should eventually trigger button sound and wait for downbeat to start fading in track...
      if (ButtonSoundManager != null)
         ButtonSoundManager.TriggerSoundForGroup(group);

      GroupStateData stateData = GetStateForGroup(group);
      if (stateData != null)
      {
         stateData.CaptureTimestamp(); //reset decay timers
         stateData.LoopState = StemLoopState.Attacking;
         /*if (stateData.LoopState == StemLoopState.Off)
         {
            stateData.LoopState = StemLoopState.Attacking;
         }
         //THIS IS TEMP UNTIL BUTTON SOUNDS ARE IN!
         else if (stateData.LoopState == StemLoopState.Releasing)
         {
            stateData.LoopState = StemLoopState.Attacking;
         }*/
      }

      //keep peak alive, add a couple seconds to peak mode
      if (_peakGroupState.LoopState == StemLoopState.Sustaining)
      {
         float secsToAdd = 2.0f;
         float elapsedTime = Time.time - _peakGroupState.Timestamp();
         float timeLeft = Mathf.Clamp(StemSustainTime - elapsedTime, 0.0f, StemSustainTime);
         if (timeLeft + secsToAdd > StemSustainTime)
            secsToAdd = StemSustainTime - timeLeft;
         _peakGroupState.SetTimestamp(_peakGroupState.Timestamp() + secsToAdd);
      }
   }

   bool AllGroupStemsSustaining()
   {
      bool nowInPeak = true;
      foreach (GroupStateData d in _groupState)
      {
         if (d.LoopState != StemLoopState.Sustaining)
         {
            nowInPeak = false;
            break;
         }
      }
      return nowInPeak;
   }

	
	void Update () 
   {
      //test mode for overridding stem levels
      if (MusicTester.EnableTestMode && !ShowTestUI)
      {
         MusicPlayer.SetBackingLoopVolume(MusicTester.BackingLoopVolume);

         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup1, MusicTester.Rift1Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup2, MusicTester.Rift2Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup3, MusicTester.Rift3Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup4, MusicTester.Rift4Volume);

         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RoomGroup1, MusicTester.Room1Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RoomGroup2, MusicTester.Room2Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RoomGroup3, MusicTester.Room3Volume);
         MusicPlayer.SetVolumeForGroup(EightNightsMgr.GroupID.RoomGroup4, MusicTester.Room4Volume);
      }

      //update state of all the audio levels
      if (!MusicTester.EnableTestMode)
      {
         bool wasPeakMode = IsPeakMode();
         if (!wasPeakMode) //should we enter peak state?
         {
            //see if all the stems are on
            bool nowInPeak = AllGroupStemsSustaining();

            if (nowInPeak)
            {
               //reset timers so stems stay alive!
               foreach (GroupStateData d in _groupState)
               {
                  d.CaptureTimestamp();
               }

               _peakGroupState.LoopState = StemLoopState.Attacking;
               _peakGroupState.CaptureTimestamp();
            }
            else
            {
               _peakGroupState.LoopState = StemLoopState.Off;
            }

            MusicPlayer.SetPeakLoopVolume(0.0f);
         }
         else //in peak state!
         {
            if (_peakGroupState.LoopState == StemLoopState.Attacking)
            {
               float u = Mathf.Clamp01((Time.time - _peakGroupState.Timestamp()) / StemAttackTime);
               MusicPlayer.SetPeakLoopVolume(u);

               if (Mathf.Approximately(u, 1.0f))
               {
                  _peakGroupState.CaptureTimestamp();
                  _peakGroupState.LoopState = StemLoopState.Sustaining;
               }
            }
            else if (_peakGroupState.LoopState == StemLoopState.Sustaining)
            {
               MusicPlayer.SetPeakLoopVolume(1.0f);

               float u = Mathf.Clamp01((Time.time - _peakGroupState.Timestamp()) / StemSustainTime);

               if (Mathf.Approximately(u, 1.0f))
               {
                  _peakGroupState.CaptureTimestamp();
                  _peakGroupState.LoopState = StemLoopState.Releasing;
               }
            }
            else if (_peakGroupState.LoopState == StemLoopState.Releasing)
            {
               float u = Mathf.Clamp01((Time.time - _peakGroupState.Timestamp()) / StemReleaseTime);
               MusicPlayer.SetPeakLoopVolume(1.0f-u);

               if (Mathf.Approximately(u, 1.0f))
               {
                  _peakGroupState.CaptureTimestamp();
                  _peakGroupState.LoopState = StemLoopState.Off;
               }
            }
         }

         foreach (GroupStateData d in _groupState)
         {
            if (d.LoopState == StemLoopState.Off)
            {
               MusicPlayer.SetVolumeForGroup(d.Group, 0.0f);
            }
            else if (d.LoopState == StemLoopState.Attacking)
            {
               float u = Mathf.Clamp01( (Time.time - d.Timestamp()) / StemAttackTime );
               MusicPlayer.SetVolumeForGroup(d.Group, u);

               if (Mathf.Approximately(u, 1.0f))
               {
                  d.CaptureTimestamp();
                  d.LoopState = StemLoopState.Sustaining;
               }
            }
            else if (d.LoopState == StemLoopState.Sustaining)
            {
               MusicPlayer.SetVolumeForGroup(d.Group, 1.0f);

               float u = Mathf.Clamp01((Time.time - d.Timestamp()) / StemSustainTime);

               if (Mathf.Approximately(u, 1.0f))
               {
                  d.CaptureTimestamp();
                  d.LoopState = StemLoopState.Releasing;
               }
            }
            else if (d.LoopState == StemLoopState.Releasing)
            {
               float u = Mathf.Clamp01((Time.time - d.Timestamp()) / StemReleaseTime);
               MusicPlayer.SetVolumeForGroup(d.Group, 1.0f - u);

               if (Mathf.Approximately(u, 1.0f))
               {
                  d.CaptureTimestamp();
                  d.LoopState = StemLoopState.Off;
               }
            }
         }
      }
	}
}
