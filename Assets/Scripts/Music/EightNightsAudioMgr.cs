using UnityEngine;
using System.Collections;
using System;

public class EightNightsAudioMgr : MonoBehaviour 
{
   public EightNightsMusicPlayer MusicPlayer;

   public MusicTestData MusicTester = new MusicTestData();

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

   public static EightNightsAudioMgr Instance { get; private set; }

   void Awake()
   {
      Instance = this;
   }

	void Start () 
   {
      MusicPlayer.SetBackingLoopVolume(1.0f);
      MusicTester.EnableTestMode = false;
	}
	
	void Update () 
   {
      if (MusicTester.EnableTestMode)
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
	}
}
