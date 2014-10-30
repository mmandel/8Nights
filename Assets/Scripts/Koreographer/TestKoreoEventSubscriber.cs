using UnityEngine;
using System.Collections;

public class TestKoreoEventSubscriber : MonoBehaviour {

   public string EventID = "TestEventID";

   public HueEffect HueEffectToFire;

	// Use this for initialization
	void Start () {
      Koreographer.Instance.RegisterForEvents(EventID, OnKoreoEvent);
	}

   void OnKoreoEvent(KoreographyEvent e)
   {
      Debug.Log("Event!");
      if (HueEffectToFire != null)
         HueEffectToFire.TriggerEffect();
   }
}
