//
// This is for animations that are scrubbed as tracks are brought int
//

using UnityEngine;
using System.Collections;

public class GroupTransformationFX : MonoBehaviour 
{

   public EightNightsMgr.GroupID Group = EightNightsMgr.GroupID.RiftGroup1;
   [HideInInspector]
   public GameObject ObjWithAnimator = null;
   [Space(10)]
   [AnimatorLayer]
   public int AnimatorLayer;
   [AnimatorState("AnimatorLayer")]
   public string StateToScrub;

   private Animator _animator = null;
   private float _lastU = 0.0f;

	void Start () 
   {
      _animator = (ObjWithAnimator != null) ? ObjWithAnimator.GetComponent<Animator>() : this.gameObject.GetComponent<Animator>();
	}
	
	void Update () 
   {
      if ((StateToScrub.Length == 0) || (_animator == null))
         return;

      _animator.speed = 0.0f;

      bool isCrescendoing = ButtonSoundMgr.Instance.IsGroupCrescendoing(Group);
      float crescendoProgress = ButtonSoundMgr.Instance.GetCrescendoProgressForGroup(Group);
      float trackVolume = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(Group);
      EightNightsAudioMgr.GroupStateData groupAudioState = EightNightsAudioMgr.Instance.GetStateForGroup(Group);

      float u = 0.0f;
      if (isCrescendoing)
         u = crescendoProgress;
      else
      {
         /*if (trackVolume > 0.0f)
            u = 1.0f;
         else
            u = 0.0f;*/
         if (groupAudioState.LoopState == EightNightsAudioMgr.StemLoopState.Releasing)
            u = trackVolume;
         else if (groupAudioState.LoopState == EightNightsAudioMgr.StemLoopState.Off)
            u = 0.0f;
         else
            u = 1.0f;
      }

      _animator.Play(StateToScrub, AnimatorLayer, u);

      _lastU = u;
	}
}
