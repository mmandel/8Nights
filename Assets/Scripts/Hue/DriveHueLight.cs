//
// Drive a light in the HueMessenger - basically an interface for animations to drive lights
//

using UnityEngine;
using System.Collections;

public class DriveHueLight : MonoBehaviour {

   public int LightIdx = 0;
   public bool on = true;
   public Color color = Color.red;
   public float fade = 1.0f;
   public float transitionTime = .25f;
	
	// Update is called once per frame
	void Update () 
   {
      if (HueMessenger.Instance != null)
         HueMessenger.Instance.SetState(LightIdx, on, fade, color, transitionTime);
	}
}
