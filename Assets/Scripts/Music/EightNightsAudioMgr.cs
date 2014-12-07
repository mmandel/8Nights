using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
   public int BaseBPM = 84;
   public bool StartDoubleTempo = false;
   public float StemAttackTime = 1.0f;
   public float StemSustainTime = 10.0f;
   public float StemReleaseTime = 3.0f;
   public bool  EnableSoloDucking = true;
   public float SoloTime = 5.0f;


   public static EightNightsAudioMgr Instance { get; private set; }

   private GroupStateData[] _groupState = null;
   private GroupStateData _peakGroupState = null;
   private SoloGroup _riftSoloing = null;
   private SoloGroup _roomSoloing = null;
   private bool _isDoubleTempo = false;

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
      public bool UseTriggerCheat = false;
      public KeyCode TriggerCheat = KeyCode.Alpha1;
      public float MasterFader = 1.0f;


      public void CaptureTimestamp() { _timeStamp = Time.time; }
      public void SetTimestamp(float t) { _timeStamp = t; }
      public float Timestamp() { return _timeStamp; }
      private float _timeStamp = -1.0f;
   }

   public class SoloGroup
   {
      public List<EightNightsMgr.GroupID> GroupsInGroup = new List<EightNightsMgr.GroupID>();

      public void MakeSoloist(EightNightsMgr.GroupID newSoloist)
      {
         foreach(EightNightsMgr.GroupID g in GroupsInGroup)
         {
            if (g == newSoloist)
            {
               _lastSoloist = g;
               _lastSoloistStart = Time.time;
               break;
            }
         }
      }

      public void Update()
      {
         float soloTime = EightNightsAudioMgr.Instance.SoloTime;
         bool soloingActive = (_lastSoloistStart > 0.0f) && ((Time.time - _lastSoloistStart) <= soloTime) && EightNightsAudioMgr.Instance.EnableSoloDucking;

         float duckSpeed = .50f;
         float soloSpeed = .50f;

         foreach (EightNightsMgr.GroupID g in GroupsInGroup)
         {
            GroupStateData sd = EightNightsAudioMgr.Instance.GetStateForGroup(g);
            if (sd == null)
               continue;

            bool isCrescendoing = ButtonSoundMgr.Instance.IsGroupCrescendoing(sd.Group);

            if (((g == _lastSoloist) && !isCrescendoing) || !soloingActive) //drive master fader towards 1
            {
               sd.MasterFader += soloSpeed * Time.deltaTime;
               sd.MasterFader = Mathf.Clamp01(sd.MasterFader);
            }
            else //drive master fader towards .5
            {
               sd.MasterFader -= duckSpeed * Time.deltaTime;
               sd.MasterFader = Mathf.Clamp(sd.MasterFader, .5f, 1.0f);
            }
         }
      }

      private EightNightsMgr.GroupID _lastSoloist;
      private float _lastSoloistStart = -1.0f;
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
         newData.UseTriggerCheat = true;
         _groupState[i] = newData;
         i++;
      }

      //setup cheats
      GetStateForGroup(EightNightsMgr.GroupID.RiftGroup1).TriggerCheat = KeyCode.Alpha1;
      GetStateForGroup(EightNightsMgr.GroupID.RiftGroup2).TriggerCheat = KeyCode.Alpha2;
      GetStateForGroup(EightNightsMgr.GroupID.RiftGroup3).TriggerCheat = KeyCode.Alpha3;
      GetStateForGroup(EightNightsMgr.GroupID.RiftGroup4).TriggerCheat = KeyCode.Alpha4;
      GetStateForGroup(EightNightsMgr.GroupID.RoomGroup1).TriggerCheat = KeyCode.Q;
      GetStateForGroup(EightNightsMgr.GroupID.RoomGroup2).TriggerCheat = KeyCode.W;
      GetStateForGroup(EightNightsMgr.GroupID.RoomGroup3).TriggerCheat = KeyCode.E;
      GetStateForGroup(EightNightsMgr.GroupID.RoomGroup4).TriggerCheat = KeyCode.R;

      //setup soloing groups
      _riftSoloing = new SoloGroup();
      _riftSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RiftGroup1);
      _riftSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RiftGroup2);
      _riftSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RiftGroup3);
      _riftSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RiftGroup4);
      _roomSoloing = new SoloGroup();
      _roomSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RoomGroup1);
      _roomSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RoomGroup2);
      _roomSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RoomGroup3);
      _roomSoloing.GroupsInGroup.Add(EightNightsMgr.GroupID.RoomGroup4);
   }

	void Start () 
   {
      SetIsDoubleTempo(StartDoubleTempo);

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

   public void ForcePeak()
   {
      foreach (GroupStateData g in _groupState)
      {
         if(g.LoopState != StemLoopState.Sustaining)
            g.LoopState = StemLoopState.Attacking;
         g.CaptureTimestamp();
      }
   }

   void SetIsDoubleTempo(bool b)
   {
      _isDoubleTempo = b;
      BeatClock.Instance.bpm = (_isDoubleTempo) ? 2 * BaseBPM : BaseBPM;

      foreach (EightNightsMIDIMgr.MIDIConfig c in EightNightsMIDIMgr.Instance.MIDIConfigs)
      {
         c.MIDIReceiver.MIDITimeMult = _isDoubleTempo ? 2.0f : 1.0f;
         c.MIDIReceiver.ReImportMIDI();
      }
   }


   void ResetAllStems(bool instant = false, bool RiftOnly = false)
   {
      foreach (GroupStateData g in _groupState)
      {
         if (RiftOnly && !g.Group.ToString().Contains("Rift"))
            continue;

         if (instant)
         {
            g.LoopState = StemLoopState.Off;
         }
         else
         {
            if ((g.LoopState != StemLoopState.Off) && (g.LoopState != StemLoopState.Releasing))
            {
               g.LoopState = StemLoopState.Releasing;
               g.CaptureTimestamp();
            }
         }
      }

      if (instant)
      {
         _peakGroupState.LoopState = StemLoopState.Off;
      }
      else
      {
         if ((_peakGroupState.LoopState != StemLoopState.Off) && (_peakGroupState.LoopState != StemLoopState.Releasing))
         {
            _peakGroupState.LoopState = StemLoopState.Releasing;
            _peakGroupState.CaptureTimestamp();
         }
      }
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

      // Rift Triggers
         Vector2 groupSize = new Vector2(100, buttonVSpacing*4 + 30);
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
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 3*buttonVSpacing, groupSize.x - 20, 20), "Group 3"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup3);
         }
         //group4
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 4 * buttonVSpacing, groupSize.x - 20, 20), "Group 4"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RiftGroup4);
         }

      // Room Triggers
         startPos.x += 130;
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
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 3 * buttonVSpacing, groupSize.x - 20, 20), "Group 3"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup3);
         }
         //group4
         if (GUI.Button(new Rect(startPos.x + 10, startPos.y + 4 * buttonVSpacing, groupSize.x - 20, 20), "Group 4"))
         {
            TriggerGroup(EightNightsMgr.GroupID.RoomGroup4);
         }

      //kill all button
         startPos.y += 4 * buttonVSpacing  + 40;
         startPos.x -= 55;
         if (GUI.Button(new Rect(startPos.x, startPos.y, groupSize.x - 20, 20), "Reset All"))
         {
            ResetAllStems();
         }

      //force peak button
         startPos.y += buttonVSpacing;
         if (GUI.Button(new Rect(startPos.x, startPos.y, groupSize.x - 20, 20), "Force Peak"))
         {
            ForcePeak();
         }
      //dynamic solo ducking
         startPos.y += 1.5f*buttonVSpacing;
         EnableSoloDucking = GUI.Toggle(new Rect(startPos.x, startPos.y, groupSize.x + 50, 20),  EnableSoloDucking, "Solo Ducking");
      //double tempo
         startPos.y += buttonVSpacing;
         bool newDoubleTempo = GUI.Toggle(new Rect(startPos.x, startPos.y, groupSize.x + 50, 20), _isDoubleTempo, "Double Tempo");
         if (newDoubleTempo != _isDoubleTempo)
            SetIsDoubleTempo(newDoubleTempo);

      // text fields for stem tuning params
         startPos.x = Screen.width * .5f - 150;
         startPos.y = 10;
         groupSize = new Vector2(200, buttonVSpacing * 4 + 30);
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
         //tweak sync (HACK!)
         startPos.y += buttonVSpacing;
         String signStr = (BeatClock.Instance.LatencyMs >= 0) ? "+" : "-";
         GUI.Label(new Rect(startPos.x + 10, startPos.y, groupSize.x - 50, 20), "Sync: " + signStr +  (BeatClock.Instance.LatencyMs / 1000));
         if (GUI.Button(new Rect(startPos.x + 10 + groupSize.x - 85, startPos.y, 30, 20), "<"))
         {
            BeatClock.Instance.LatencyMs -= 1000;
         }

         if (GUI.Button(new Rect(startPos.x + 10 + groupSize.x - 45, startPos.y, 30, 20), ">"))
         {
            BeatClock.Instance.LatencyMs += 1000;
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
      /*   startPos.x += 135;
         startPos.y += 10;
         if(GUI.Button(new Rect(startPos.x, startPos.y, 100, 25), "Restart Song"))
         {
            MusicPlayer.Restart();
         }
      */

      //Test lights in bottom right
      if(EightNightsMgr.Instance != null)
      {
         startPos.x = Screen.width - 150;
         startPos.y = Screen.height - 30;
         EightNightsMgr.Instance.TestLights = GUI.Toggle(new Rect(startPos.x, startPos.y, groupSize.x + 50, 20), EightNightsMgr.Instance.TestLights, "Test Lights");
      }
   }

   public GroupStateData GetStateForGroup(EightNightsMgr.GroupID group)
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

         //This now happens in Update in response to crescendo progress
         /*if (stateData.LoopState != StemLoopState.Sustaining)
         {
            stateData.LoopState = StemLoopState.Attacking;
         }*/
      }

      if (EnableSoloDucking)
      {
         _roomSoloing.MakeSoloist(group);
         _riftSoloing.MakeSoloist(group);
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

      if (EightNightsMgr.Instance == null)
      {
         if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
      }

      //toggle debug UI with a key
      if (Input.GetKeyDown(KeyCode.D))
      {
         ShowTestUI = !ShowTestUI;
      }

      if (Input.GetKeyDown(KeyCode.P))
         ResetAllStems(false, true);

      if (Input.GetKeyDown(KeyCode.KeypadPlus))
         BeatClock.Instance.LatencyMs += 1000;
      if (Input.GetKeyDown(KeyCode.KeypadMinus))
         BeatClock.Instance.LatencyMs -= 1000;      

      //keyboard cheats
      foreach (GroupStateData d in _groupState)
      {
         if (d.UseTriggerCheat && Input.GetKeyDown(d.TriggerCheat))
            TriggerGroup(d.Group);
      }

      //Update Soloing Faders
      _riftSoloing.Update();
      _roomSoloing.Update();

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
            //handle transition to attacking:  fade in track as button crescendoing completes
            if ((d.LoopState != StemLoopState.Sustaining) && (d.LoopState != StemLoopState.Attacking))
            {
               bool isCrescendoing = ButtonSoundMgr.Instance.IsGroupCrescendoing(d.Group);
               float crescendoTimeRemaining = ButtonSoundMgr.Instance.GetCrescendoTimeRemainaingForGroup(d.Group);
               //if(isCrescendoing)
               //   Debug.Log("Crescendo Time Remaining for " + d.Group.ToString() + ": " + crescendoTimeRemaining);
               if (isCrescendoing && (crescendoTimeRemaining < StemAttackTime))
               {
                  //Debug.Log("  ATTACK GROUP " + d.Group.ToString());
                  d.LoopState = StemLoopState.Attacking;
                  d.CaptureTimestamp();
               }
            }

            if (d.LoopState == StemLoopState.Off)
            {
               MusicPlayer.SetVolumeForGroup(d.Group, 0.0f);
            }
            else if (d.LoopState == StemLoopState.Attacking)
            {
               float u = Mathf.Clamp01((Time.time - d.Timestamp()) / StemAttackTime);
               MusicPlayer.SetVolumeForGroup(d.Group, d.MasterFader * u);

               if (Mathf.Approximately(u, 1.0f))
               {
                  d.CaptureTimestamp();
                  d.LoopState = StemLoopState.Sustaining;
               }
            }
            else if (d.LoopState == StemLoopState.Sustaining)
            {
               MusicPlayer.SetVolumeForGroup(d.Group, d.MasterFader*1.0f);

               float u =  Mathf.Clamp01((Time.time - d.Timestamp()) / StemSustainTime);

               if (Mathf.Approximately(u, 1.0f))
               {
                  d.CaptureTimestamp();
                  d.LoopState = StemLoopState.Releasing;
               }
            }
            else if (d.LoopState == StemLoopState.Releasing)
            {
               float u = Mathf.Clamp01((Time.time - d.Timestamp()) / StemReleaseTime);
               MusicPlayer.SetVolumeForGroup(d.Group, d.MasterFader*(1.0f - u));

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
