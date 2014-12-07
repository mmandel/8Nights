//
// Drives animations for room transformation
//

using UnityEngine;
using System.Collections;

public class RoomTransformation : MonoBehaviour 
{
   [HideInInspector]
   public GameObject ObjWithAnimator = null;
   [Space(10)]

   [AnimatorLayer]
   public int AnimatorLayer;
   [AnimatorState("AnimatorLayer")]
   public string StateToScrub;

   public float RoomOutTime = 2.0f;
   public float RoomInTime = 1.0f;

   private float _roomU = 1.0f;

   private Animator _animator = null;

   void Start()
   {
      _animator = (ObjWithAnimator != null) ? ObjWithAnimator.GetComponent<Animator>() : this.gameObject.GetComponent<Animator>();
   }


	void Update () 
   {
      if ((StateToScrub.Length == 0) || (_animator == null))
         return;

      _animator.speed = 0.0f;

      float trackVolume1 = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup1);
      float trackVolume2 = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup2);
      float trackVolume3 = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup3);
      float trackVolume4 = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(EightNightsMgr.GroupID.RiftGroup4);

      //room goes away if one of the tracks is playing
      bool allTracksOff = (trackVolume1 == 0.0f) && (trackVolume2 == 0.0f) &&  (trackVolume3 == 0.0f) &&  (trackVolume4 == 0.0f);
      bool isCrescendoing = ButtonSoundMgr.Instance.IsGroupCrescendoing(EightNightsMgr.GroupID.RiftGroup1) || 
                            ButtonSoundMgr.Instance.IsGroupCrescendoing(EightNightsMgr.GroupID.RiftGroup2) || 
                            ButtonSoundMgr.Instance.IsGroupCrescendoing(EightNightsMgr.GroupID.RiftGroup3) || 
                            ButtonSoundMgr.Instance.IsGroupCrescendoing(EightNightsMgr.GroupID.RiftGroup4);

      if (isCrescendoing || !allTracksOff) //no room
      {
         _roomU += (1.0f / RoomOutTime) * Time.deltaTime;
         _roomU = Mathf.Clamp01(_roomU);
         _animator.Play(StateToScrub, AnimatorLayer, _roomU);	
      }
      else //bring room back
      {
         _roomU -= (1.0f / RoomInTime) * Time.deltaTime;
         _roomU = Mathf.Clamp01(_roomU);
         _animator.Play(StateToScrub, AnimatorLayer, _roomU);	
      }
	}
}
