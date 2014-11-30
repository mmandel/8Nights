//
//  A gaze-activateable button in the installation space
//  This script just manages the UI feedback through animator hooks.
//
//  See RiftButtonMgr for gameplay code
//

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class RiftButton : MonoBehaviour 
{
   [Space(10)]

   public EightNightsMgr.GroupID GroupToActivate;

   [Space(10)]

   [Header("Progress Feedback")]
   [AnimatorLayer]
   public int ProgressScrubLayer = 0;
   [AnimatorState("ProgressScrubLayer")]
   public string ProgressScrubState = "";

   [Header("Selected Feedback")]
   [AnimatorLayer]
   public int SelectedLayer = 0;
   [AnimatorState("SelectedLayer")]
   public string SelectedState = "";
   public AnimationCurve SelectedEase = null;
   public float SelectedTransitionIn = 1.0f;
   public float SelectedTransitionOut = 1.0f;

   [Header("Pressed Feedback")]
   [AnimatorLayer]
   public int PressedLayer = 0;
   [AnimatorState("PressedLayer")]
   public string PressedState = "";
   public float PressedAnimTime = 1.0f;

   [Header("Testing")]
   [Range(0.0f, 1.0f)]
   public float SelectionProgress = 0.0f;
   public bool Selected = false;
   [ScriptButton("Press!", "TriggerPress")]
   public bool DummyPressed = false;

   private Animator _animator = null;
   private float _curSelectedU = 0.0f;
   private float _pressTimestamp = -1.0f;

   public RiftButton()
   {
      SelectedEase = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
   }


	void Start () 
   {
      _animator = gameObject.GetComponent<Animator>();
	}

   public void TriggerPress(string propPath = "")
   {
      _pressTimestamp = Time.time;

      if(EightNightsAudioMgr.Instance != null)
         EightNightsAudioMgr.Instance.TriggerGroup(GroupToActivate);
   }
	

	void Update () 
   {
      if (_animator == null)
         return;

      _animator.speed = 0.0f;

      if (ProgressScrubState.Length > 0)
         _animator.Play(ProgressScrubState, ProgressScrubLayer, Mathf.Clamp(SelectionProgress, 0.0f, .999f));

      if (SelectedState.Length > 0)
      {
         if (Selected && (_curSelectedU < 1.0f))
            _curSelectedU += (1.0f / SelectedTransitionIn) * Time.deltaTime;
         else if (!Selected && (_curSelectedU > 0.0f))
            _curSelectedU -= (1.0f / SelectedTransitionOut) * Time.deltaTime;

         _curSelectedU = Mathf.Clamp01(_curSelectedU);

         float animU = SelectedEase.Evaluate(_curSelectedU);
         _animator.Play(SelectedState, SelectedLayer, Mathf.Clamp(_curSelectedU, 0.0f, .999f));
      }

      if (PressedState.Length > 0)
      {
         if (_pressTimestamp < 0)
         {
            _animator.Play(PressedState, PressedLayer, 0.0f);
         }
         else
         {
            float u = Mathf.Clamp01((Time.time - _pressTimestamp) / PressedAnimTime);
            if (Mathf.Approximately(u, 1.0f))
               _pressTimestamp = -1.0f;
            _animator.Play(PressedState, PressedLayer, Mathf.Clamp(u, 0.0f, .999f));
         }
      }
	}
}
