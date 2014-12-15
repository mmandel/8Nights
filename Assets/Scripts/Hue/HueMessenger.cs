//
// Sends updates to Phillips Hue lights over HTTP
//

using UnityEngine;
using System.Collections;
using System;

public class HueMessenger : MonoBehaviour 
{

   public string BridgeIP = "10.0.1.11";
   public string User = "newdeveloper";
   public bool UseFixedLatency = true;
   public float FixedLatency = .18f;

   [Space(10)]

   public bool FireAndForget = true;

   [Space(10)]

   public Light[] Lights = new Light[1];

   public static HueMessenger Instance { get; private set; }

   [System.Serializable]
   public class Light
   {
      public int id = 3;
      public bool on = true;
      public Color color = Color.red;
      [Range(0.0f, 1.0f)]
      public float fade = 1.0f;
      public float transitionTime = .25f;

      public bool ShouldPush() { return (on != _lastOn) || (_lastFade != fade) || (_lastColor != color); }

      public void Pushed()
      {
         _lastColor = color;
         _lastOn = on;
         _lastFade = fade;
         _lastUpdateTime = Time.time;
      }

      public Color LastColor() { return _lastColor; }
      public float LastFade() { return _lastOn ? _lastFade : 0.0f; }

      public float TimeSinceLastUpdate() { return Time.time - _lastUpdateTime; }

      private Color _lastColor = Color.black;
      private float _lastFade = 0.0f;
      private bool _lastOn = false;
      private float _lastUpdateTime = 0.0f;
   }

   //events
   public event HueHandler OnLightChanged;     //send out whenever a physical light is expected to change in real life
   public event HueHandler OnLightCommandSent; //send out whenever HTTP message is sent to a light (should be send ahead of time due to latency)
   public class HueEventArgs : EventArgs
   {
      public HueEventArgs(int id, Color c, float f, float tt, Color pC, float pF) { LightID = id; LightColor = c; LightFade = f; TransitionTime = tt; PrevColor = pC; PrevFade = pF; }
      public int LightID = 0;
      public Color LightColor = Color.white;
      public float LightFade = 1.0f;
      public float TransitionTime = 0.0f;

      public float PrevFade = 1.0f;
      public Color PrevColor = Color.white;
   }
   public delegate void HueHandler(object sender, HueEventArgs e);

   bool _isRequesting = false;
   float _masterFader = 1.0f;

   public void SetMasterFade(float f) { _masterFader = f; }
   public float MasterFader() { return _masterFader; }

   void Awake()
   {
      Instance = this;
   }

	// Use this for initialization
	void Start () 
   {
      //StartCoroutine(TestHue());
	}

   public float GetCurLatency()
   {
      return UseFixedLatency ? FixedLatency : 0.0f; //TODO: compute this dynamically!
   }

   public int FindLightWithChannel(int channel)
   {
      for (int i = 0; i < Lights.Length; i++)
      {
         if (Lights[i].id == channel)
            return i;
      }
      return -1;
   }


   public void SetState(int lightIdx, bool on, float fade, Color c, float transitionTime)
   {
      if ((lightIdx >= 0) && (lightIdx < Lights.Length))
      {
         Lights[lightIdx].on = on;
         Lights[lightIdx].fade = fade;
         Lights[lightIdx].color = c;
         Lights[lightIdx].transitionTime = transitionTime;
      }
   }

   IEnumerator SendDelayedUpdateEvent(float delayTime, HueEventArgs eventData)
   {
      yield return new WaitForSeconds(delayTime);

      if (OnLightChanged != null)
         OnLightChanged(this, eventData);
   }

   void Update()
   {
      if (FireAndForget)
      {
         foreach (Light l in Lights)
         {
            UpdateLight(l);
         }
      }
      else
      {
         if (!_isRequesting)
            StartCoroutine(UpdateLights());
      }
   }

   /*void LateUpdate()
   {
      for (int i = 0; i < Lights.Length; i++)
      {
         HueMessenger.Light l = Lights[i];
         l.
      }
   }*/

   public IEnumerator UpdateLights()
   {
      _isRequesting = true;

      foreach (Light l in Lights)
      {
         Color prevColor = l.LastColor();
         float prevFade = l.LastFade();

         if (!l.ShouldPush())
            continue;

         string apiCall = "/api/" + User + "/lights/" + l.id + "/state";
         float fade = Mathf.Clamp01(l.fade);
         HueHSBColor hsbColor = new HueHSBColor(l.color);
         int transitionTime = (int)(l.transitionTime * 10.0f); //this is specified in hundreds of millisecs (i.e 10 = 1000 ms = 1s)
         string body = "{\"on\": " + ((l.on && (fade > 0.0f)) ? "true" : "false") +
                       " \"hue\": " + (int)(hsbColor.h * 65535.0f) +
                       " \"sat\": " + (int)(hsbColor.s * 255.0f) +
                       " \"bri\": " + (int)(hsbColor.b * fade * 255.0f) +
                       " \"transitiontime\": " + transitionTime +
                       //" \"effect\":\"colorloop\"" +
                       "}";
         string url = "http://" + BridgeIP + apiCall;
         //Debug.Log("URL: " + url + " body: " + body + " at Time: " + Time.time + " deltaSinceLastUpdate: " + l.TimeSinceLastUpdate() + "\n");
         HTTP.Request request = new HTTP.Request("put", "http://" + BridgeIP + apiCall, JSON.JsonDecode(body) as Hashtable);
         request.Send();

         l.Pushed();

         SendEvents(l, prevColor, prevFade);

         if (!FireAndForget)
         {
            float startTime = Time.time;
            while (!request.isDone)
            {
               yield return null;
            }
            Debug.Log("Received response in " + (Time.time - startTime) +  " secs!");

            if (!request.response.Text.Contains("success"))
               Debug.Log("Error updating light: " + request.response.Text);
         }
      }

      _isRequesting = false;
   }

   HTTP.Request UpdateLight(Light l)
   {
      Color prevColor = l.LastColor();
      float prevFade = l.LastFade();
      if (!l.ShouldPush())
         return null;

      string apiCall = "/api/" + User + "/lights/" + l.id + "/state";
      float fade = Mathf.Clamp01(l.fade);
      HueHSBColor hsbColor = new HueHSBColor(l.color);
      int transitionTime = (int)(l.transitionTime * 10.0f); //this is specified in hundreds of millisecs (i.e 10 = 1000 ms = 1s)
      bool on = (l.on && (fade > 0.0f));
      string body = "{\"on\": " + (on ? "true" : "false") +
                    " \"hue\": " + (int)(hsbColor.h * 65535.0f) +
                    " \"sat\": " + (int)(hsbColor.s * 255.0f) +
                    " \"bri\": " + (int)(hsbColor.b * fade * 255.0f) +
                    " \"transitiontime\": " + transitionTime +
                     //" \"effect\":\"colorloop\"" +
                    "}";
     /* on = true;
      string body = "{\"on\": " + (on ? "true" : "false") +
                    " \"effect\":\"colorloop\"" +
                    "}";*/
      string url = "http://" + BridgeIP + apiCall;
      //Debug.Log("URL: " + url + " body: " + body + " at Time: " + Time.time + " deltaSinceLastUpdate: " + l.TimeSinceLastUpdate() + "\n");      
      HTTP.Request request = new HTTP.Request("put", "http://" + BridgeIP + apiCall, JSON.JsonDecode(body) as Hashtable);
      request.Send();

      l.Pushed();

      //notify anyone who cares
      SendEvents(l, prevColor, prevFade);

      return request;
   }

   void SendEvents(Light l, Color prevColor, float prevFade)
   {
      //notify anyone who cares
      if ((OnLightCommandSent != null) || (OnLightChanged != null))
      {
         float eventFade = l.fade;
         if (!l.on)
            eventFade = 0.0f;
         HueEventArgs eventData = new HueEventArgs(l.id, l.color, eventFade, l.transitionTime, prevColor, prevFade);

         if (OnLightCommandSent != null)
            OnLightCommandSent(this, eventData);

         //run co-routine to send out event for game after latency elapses (so visuals in-game don't trigger early)
         if (OnLightChanged != null)
            StartCoroutine(SendDelayedUpdateEvent(GetCurLatency(), eventData));
      }
   }

   public IEnumerator TestHue()
   {
      string apiCall = "/api/newdeveloper/lights/3/";      
      HTTP.Request someRequest = new HTTP.Request("get", "http://" + BridgeIP + apiCall);
      someRequest.Send();

      while (!someRequest.isDone)
      {
         yield return null;
      }

      Debug.Log("Response: " + someRequest.response.Text);
      //Hashtable result = JSON.JsonDecode(someRequest.response.Text) as Hashtable;
      //Debug.Log("JSON: state- " + result["state"]);
      // parse some JSON, for example:
      //JSONObject thing = new JSONObject(request.response.Text);

      apiCall = "/api/newdeveloper/lights/3/state";
      string body = "{\"on\": true}";
      HTTP.Request anotherRequest = new HTTP.Request("put", "http://" + BridgeIP + apiCall, JSON.JsonDecode(body) as Hashtable);
      anotherRequest.Send();

      while (!anotherRequest.isDone)
      {
         yield return null;
      }

      Debug.Log("Response: " + anotherRequest.response.Text);
   }
}
