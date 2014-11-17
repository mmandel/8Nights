//
//  Sends messages to the LightJams app using OSC network packets, to update DMX controlled physical lights
//

using UnityEngine;
using System.Collections;
using System;

public class LightJamsMgr : MonoBehaviour 
{
   string kOSCPrefix = "/lj/osc/";

   //events
   public event LJHandler OnLightChanged;     //send out whenever a physical light is expected to change in real life
   public class LJEventArgs : EventArgs
   {
      public LJEventArgs(int chan, float i) { Channel = chan; Intensity = i; }
      public int Channel = 0;
      public float Intensity = 1.0f;
   }
   public delegate void LJHandler(object sender, LJEventArgs e);

   	//Singleton
   public static LightJamsMgr Instance { get;  private set; }

	// Use this for initialization
	void Awake () {
	   Instance = this;

      //make sure the OSCMessenger exists
      if (gameObject.GetComponent<OSCMessenger>() == null)
         gameObject.AddComponent<OSCMessenger>();
	}

   public void SendToLightJams(int channelNum, float val)
   {
      if (OSCMessenger.Instance != null)
      {
         OSCMessenger.Instance.SendMessage(kOSCPrefix + channelNum, val);

         if (OnLightChanged != null)
         {
            OnLightChanged(this, new LJEventArgs(channelNum, val));
         }
      }
   }
	
}
