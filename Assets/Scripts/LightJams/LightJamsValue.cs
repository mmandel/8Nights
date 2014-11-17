//
//  Control a single value in LightJams over OSC
//

using UnityEngine;
using System.Collections;

public class LightJamsValue : MonoBehaviour {

   [Range(0.0f, 1.0f)]
   public float Value = 0.0f;
   [Range(0, 511)]
   public int Channel = 1;

   void OnDestroy()
   {
      if ((LightJamsMgr.Instance != null)) //don't leave lights on in-game
         LightJamsMgr.Instance.SendToLightJams(Channel, 0.0f);
   }

	// Update is called once per frame
	void Update () 
   {
      if (LightJamsMgr.Instance != null)
         LightJamsMgr.Instance.SendToLightJams(Channel, Value);
	}
}
