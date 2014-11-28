//
// A way to keyframe a simple effect that targets a particular light in the installation 
//

using UnityEngine;
using System.Collections;

public class LightEffect : MonoBehaviour
{

   [System.Serializable]
   public class EffectKeyframe
   {
      public float HoldTime = 0.0f; //hold this value for this amount of time

      public float BlendTime = 1.0f; //transition to next key over this amount of time

      public LightState[] LightKeys = new LightState[1];
   }

   [System.Serializable]
   public class LightState
   {
      public EightNightsMgr.LightID Light;
      
      public bool on = true;
      public Color color = Color.red;
      public float fade = 1.0f;
   }

   [ScriptButton("Trigger!", "TriggerEffect")]
   public bool DummyTrigger;

   public EightNightsMgr.GroupID LightGroup;
   public bool Loop = true;
   public bool BlendWhenLooping = true;
   public float SpeedScale = 1.0f;
   public bool AutoTrigger = false;
   public bool AutoDestroy = false; //do we destroy the game object we're on after playing once?
   public EffectKeyframe[] Keyframes = new EffectKeyframe[1];

   int _curKey = -1;
   float _timeStamp = -1.0f;
   float _curTransitionTime = 0.0f;
   KeyState _curState = KeyState.kDone;
   bool _doesControlHue = false;
   bool _doesControlLightJams = false;

   enum KeyState
   {
      kDone,
      kStart,
      kHolding,
      kBlending
   };

   // Use this for initialization
   void Start()
   {
      foreach (EffectKeyframe k in Keyframes)
      {
         foreach (LightState l in k.LightKeys)
         {
            if (EightNightsMgr.Instance.IsHueLight(LightGroup, l.Light))
               _doesControlHue = true;
            if (EightNightsMgr.Instance.IsLightJamsLight(LightGroup, l.Light))
               _doesControlLightJams = true;
         }
      }
   }

   void OnEnable()
   {
      //always restart effect when our script is enabled
      if(AutoTrigger)
         TriggerEffect();
   }

   public bool ControlsHueLight()
   {
      return _doesControlHue;
   }

   public bool ControlsLightJamsLight()
   {
      return _doesControlLightJams;
   }

   public void TriggerEffect(string propPath = "")
   {
      _curState = KeyState.kStart;
   }

   // Update is called once per frame
   void Update()
   {

      if ((Keyframes.Length == 0) || (HueMessenger.Instance == null))
         return;

      float elapsed = Time.time - _timeStamp;

      EffectKeyframe key = null;
      switch (_curState)
      {
         case KeyState.kStart:
            _timeStamp = -1;
            _curKey = -1;
            ApplyKey(0);
            break;
         case KeyState.kBlending:
            key = Keyframes[_curKey];
            if (elapsed >= _curTransitionTime)
            {
               if (Mathf.Approximately(key.HoldTime, 0.0f)) //no hold, so go to next key
               {
                  if ((_curKey == (Keyframes.Length - 1)) && !Loop) //last key, we're done!
                     _curState = KeyState.kDone;
                  else
                     ApplyKey((_curKey + 1) % Keyframes.Length);
               }
               else //start holding
               {
                  _timeStamp = _timeStamp + _curTransitionTime;
                  _curState = KeyState.kHolding;
               }
            }
            break;
         case KeyState.kHolding:
            key = Keyframes[_curKey];
            if (elapsed >= SpeedScale * key.HoldTime)
            {
               if ((_curKey == (Keyframes.Length - 1)) && !Loop) //last key, we're done!
                  _curState = KeyState.kDone;
               else
                  ApplyKey((_curKey + 1) % Keyframes.Length);
            }
            break;
         case KeyState.kDone:
            if (AutoDestroy)
               DestroyObject(this.gameObject);
            else if (Loop)
               _curState = KeyState.kStart;
            break;
      }
   }

   void ApplyKey(int idx)
   {
      EffectKeyframe prevKey = (_curKey >= 0) ? Keyframes[_curKey] : null;
      EffectKeyframe key = Keyframes[idx];

      //we fade effects out with the volume of their group
      float groupFader = (EightNightsAudioMgr.Instance != null) ? EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(LightGroup) : 1.0f;

      float transitionTime = (prevKey != null) ? SpeedScale * prevKey.BlendTime : 0.0f;

      //looping around, pop to first frame if configured for that
      if ((prevKey != null) && (idx == 0) && !BlendWhenLooping)
         transitionTime = 0.0f;

      for (int i = 0; i < key.LightKeys.Length; i++)
      {
         LightState curState = key.LightKeys[i];
         EightNightsMgr.Instance.SetLight(LightGroup, curState.Light, groupFader*curState.fade, curState.color, transitionTime);
      }

      if (Mathf.Approximately(key.BlendTime, 0.0f))
         _curState = KeyState.kHolding;
      else
         _curState = KeyState.kBlending;

      _curKey = idx;
      _curTransitionTime = transitionTime;
      _timeStamp = Time.time;
   }
}
