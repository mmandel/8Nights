//
//  This instances a prefab that has a looping AudioSource attached.  It can fade out one instance while another is spawned.
//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Holoville.HOTween;

public class SpawnAudioLoop : MonoBehaviour {

   [Header("Prefab w/ AudioSource")]
   public GameObject ClipPrefab = null;

   [Header("Play")]
   public float PlayVolume = 1.0f;

   [Header("FadeOut")]
   public float NormalFadeTime = 1.0f;
   public float CancelFadeTime = 1.0f;
   public Holoville.HOTween.EaseType FadeOutEase = Holoville.HOTween.EaseType.EaseInOutSine;

   [Header("Performance")]
   [Range(1, 15)]
   public int MaxInstances = 5;

   [Header("Debug")]
   public bool EnableTestingCheats = false;

   private GameObject _curLoop = null;
   private List<GameObject> _fadingLoops = new List<GameObject>();
   private Tweener _curClipFadeIn;

   public void Play(float overrideVolume)
   {
      if (ClipPrefab == null)
         return;

      if (_curLoop != null)
         FadeOut(true);

      _curLoop = Instantiate(ClipPrefab) as GameObject;
      if (_curLoop != null)
      {
         _curLoop.transform.parent = this.transform;
         _curLoop.audio.volume = overrideVolume;
         _curLoop.audio.Play();
      }
   }

   public bool IsPlaying()
   {
      return (_curLoop != null);
   }

   public void Play()
   {
      Play(PlayVolume);
   }

   //fade clip up to its PlayVolume over the given time
   public void FadeIn(float time, Holoville.HOTween.EaseType ease = EaseType.Linear)
   {
      if (_curLoop != null)
      {
         _curClipFadeIn = HOTween.To(_curLoop.audio, time, new TweenParms().Prop("volume", PlayVolume).Ease(ease));
      }
   }

   public void FadeOut(bool isCancel = false)
   {
      if (_curLoop != null)
      {
         if(_curClipFadeIn != null)
         {
            _curClipFadeIn.Kill();
            _curClipFadeIn = null;
         }

         if (_fadingLoops.Count == (MaxInstances-1))
         {
            DestroyObject(_fadingLoops[0]);
            _fadingLoops.RemoveAt(0);
         }

         //start fading it out, and track it till its done
         HOTween.To(_curLoop.audio, isCancel ? CancelFadeTime : NormalFadeTime, new TweenParms().Prop("volume", 0.0f).OnComplete(this.gameObject, "OnFadeOutComplete", _curLoop).Ease(FadeOutEase));
         _fadingLoops.Add(_curLoop);

         _curLoop = null;
      }
   }

   //done fading out a loop, just kill the gameobject now
   void OnFadeOutComplete(GameObject fadedObj)
   {
      bool removed = _fadingLoops.Remove(fadedObj);
      if (removed)
         DestroyObject(fadedObj);
      else
         Debug.LogError("Finished fading " + fadedObj.name + " but wasn't tracking it!");
   }

   void Update()
   {
      if (EnableTestingCheats)
      {
         if (Input.GetKeyDown(KeyCode.P))
            Play();
         if (Input.GetKeyDown(KeyCode.L))
            FadeOut();
      }
   }
}
