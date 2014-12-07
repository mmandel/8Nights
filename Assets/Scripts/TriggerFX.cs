//
//  Use this to trigger FX in the virtual world, synced to various events in the installation (like button presses or specific lights triggering)
//

using UnityEngine;
using System.Collections;

public class TriggerFX : MonoBehaviour 
{
   [Header("Trigger Condition")]
   public TriggerEvents TriggerEvent = TriggerEvents.NoteOnLight;
   public EightNightsMgr.GroupID Group;
   public EightNightsMgr.LightID Light;

   /*[Header("Emit Particles")]
   public bool EnableParticleEmit = false;
   public ParticleSystem[] ParticleSys = new ParticleSystem[0];
   public int NumParticlesToEmit = 100;*/

   [Header("Play Particle Effect")]
   public bool EnablePlayParticleEffect = false;
   public ParticleSystem[] PlayParticleSys = new ParticleSystem[0]; 

   [Header("Play Animation")]
   public bool EnablePlayAnimation = false;
   [AnimatorLayer]
   public int AnimatorLayer;
   [AnimatorState("AnimatorLayer")]
   public string AnimatorState;
   public float TimeToAnimate = 1.0f;


   /*[Header("Animator Trigger")]
   public bool EnableAnimatorTrigger = false;
   public string AnimatorTriggerName = "";*/

   public enum TriggerEvents
   {
      ButtonPress,
      NoteOnLight
   }

   private Animator _animator;
   private float _animationStartTime = -1.0f;

	void Start () 
   {
      _animator = this.gameObject.GetComponent<Animator>();

      //subscribe to the relevant events
      if (EightNightsMgr.Instance != null)
      {
         EightNightsMgr.Instance.OnButtonTriggered += OnButtonTriggered;
         EightNightsMgr.Instance.OnLightEffectTriggered += OnLightEffectTriggered;
      }
	}

   void OnDestroy()
   {
      if (EightNightsMgr.Instance != null)
      {
         EightNightsMgr.Instance.OnButtonTriggered -= OnButtonTriggered;
         EightNightsMgr.Instance.OnLightEffectTriggered -= OnLightEffectTriggered;
      }
   }

   void OnLightEffectTriggered(object sender, EightNightsMgr.LightTriggeredEventArgs e)
   {
      if (TriggerEvent != TriggerEvents.NoteOnLight)
         return;

      if ((e.Light == Light) && (e.Group == Group))
         Trigger();
   }

   void OnButtonTriggered(object sender, EightNightsMgr.ButtonTriggeredEventArgs e)
   {
      if (TriggerEvent != TriggerEvents.ButtonPress)
         return;

      if (e.Group == Group)
         Trigger();
   }

   void Trigger()
   {
      /*if (EnableParticleEmit)
      {

         foreach (ParticleSystem s in ParticleSys)
         {
            if (s != null)
               s.Emit(NumParticlesToEmit);
         }
      }*/

      if (EnablePlayParticleEffect)
      {
         foreach (ParticleSystem s in PlayParticleSys)
         {
            if (s != null)
               s.Play();
         }
      }

      if (EnablePlayAnimation)
      {
         _animationStartTime = Time.time;
      }

      /*if (EnableAnimatorTrigger)
      {
         if (_animator != null)
            _animator.SetTrigger(AnimatorTriggerName);
      }*/
   }
	
	void Update ()
   {
      if (_animationStartTime > 0.0f)
      {
         float u = Mathf.Clamp01((Time.time - _animationStartTime) / TimeToAnimate);
         if (_animator != null)
         {
            _animator.speed = 0.0f;
            _animator.Play(AnimatorState, AnimatorLayer, u);
         }

         if (Mathf.Approximately(u, 1.0f)) //done?
            _animationStartTime = -1.0f;
      }
	}
}
