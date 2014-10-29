using UnityEngine;
using System.Collections;

public class HueMessenger : MonoBehaviour {

   public string BridgeIP = "10.0.1.11";
   public string User = "newdeveloper";

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

      public float TimeSinceLastUpdate() { return Time.time - _lastUpdateTime; }

      private Color _lastColor = Color.black;
      private float _lastFade = 0.0f;
      private bool _lastOn = false;
      private float _lastUpdateTime = 0.0f;
   }

   bool _isRequesting = false;

   void Awake()
   {
      Instance = this;
   }

	// Use this for initialization
	void Start () 
   {
      //StartCoroutine(TestHue());
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

   public IEnumerator UpdateLights()
   {
      _isRequesting = true;

      foreach (Light l in Lights)
      {
         HTTP.Request request = UpdateLight(l);

         if (!FireAndForget)
         {
            while (!request.isDone)
            {
               yield return null;
            }

            if (!request.response.Text.Contains("success"))
               Debug.Log("Error updating light: " + request.response.Text);
         }
      }

      _isRequesting = false;
   }

   HTTP.Request UpdateLight(Light l)
   {
      if (!l.ShouldPush())
         return null;

      string apiCall = "/api/" + User + "/lights/" + l.id + "/state";
      float fade = Mathf.Clamp01(l.fade);
      HSBColor hsbColor = new HSBColor(l.color);
      int transitionTime = (int)(l.transitionTime * 10.0f); //this is specified in hundreds of millisecs (i.e 10 = 1000 ms = 1s)
      string body = "{\"on\": " + ((l.on && (fade > 0.0f)) ? "true" : "false") +
                    " \"hue\": " + (int)(hsbColor.h * 65535.0f) +
                    " \"sat\": " + (int)(hsbColor.s * 255.0f) +
                    " \"bri\": " + (int)(hsbColor.b * fade * 255.0f) +
                    " \"transitiontime\": " + transitionTime +
                    "}";
      string url = "http://" + BridgeIP + apiCall;
      Debug.Log("URL: " + url + " body: " + body + " at Time: " + Time.time + " deltaSinceLastUpdate: " + l.TimeSinceLastUpdate() + "\n");      
      HTTP.Request request = new HTTP.Request("put", "http://" + BridgeIP + apiCall, JSON.JsonDecode(body) as Hashtable);
      request.Send();

      l.Pushed();      

      return request;
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
