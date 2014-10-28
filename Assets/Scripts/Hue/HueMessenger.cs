using UnityEngine;
using System.Collections;

public class HueMessenger : MonoBehaviour {

   public string BridgeIP = "10.0.1.11";
   public string User = "newdeveloper";

   public Light[] Lights = new Light[1];

   [System.Serializable]
   public class Light
   {
      public int id = 3;
      public bool on = true;
      public Color color = Color.red;
      [Range(0.0f, 1.0f)]
      public float fade = 1.0f;
   }

   bool _isRequesting = false;

	// Use this for initialization
	void Start () 
   {
      //StartCoroutine(TestHue());
	}

   void Update()
   {
      if (!_isRequesting)
         StartCoroutine(UpdateLights());
   }

   public IEnumerator UpdateLights()
   {
      _isRequesting = true;

      foreach (Light l in Lights)
      {
         string apiCall = "/api/" + User + "/lights/" + l.id +  "/state";
         HSBColor hsbColor = new HSBColor(l.color);
         string body = "{\"on\": " + ((l.on && (l.fade > 0.0f)) ? "true" : "false") +
                       "\"hue\": " + (int)(hsbColor.h * 65535.0f) +
                       "\"sat\": " + (int)(hsbColor.s * 255.0f) +
                       "\"bri\": " + (int)(hsbColor.b * l.fade * 255.0f) +
                       "}";
         string url = "http://" + BridgeIP + apiCall;
         //Debug.Log("URL: " + url + " body: " + body);
         HTTP.Request request = new HTTP.Request("put", "http://" + BridgeIP + apiCall, JSON.JsonDecode(body) as Hashtable);
         request.Send();

         while (!request.isDone)
         {
            yield return null;
         }

         if (!request.response.Text.Contains("success"))
            Debug.Log("Error updating light: " + request.response.Text);
      }

      _isRequesting = false;
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
