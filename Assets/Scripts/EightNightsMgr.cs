//
//  Central manager for the installation.  Handles configuration of all the lights and sends out events as states change
//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class EightNightsMgr : MonoBehaviour 
{

   //events
   public event LightHandler OnLightChanged; //send out whenever a physical light is updated
   public class LightEventArgs : EventArgs
   {
      public LightEventArgs(GroupID g, LightID l, LightTypes lt, LightData d) { Group = g; Light = l; LightType = lt;  Data = d; }
      public GroupID Group;
      public LightID Light;
      public LightTypes LightType;
      public LightData Data;
   }
   public delegate void LightHandler(object sender, LightEventArgs e);

   //identifiers for each group of lights
   public enum GroupID
   {
      RiftGroup1 = 0,
      RiftGroup2,
      RiftGroup3,
      RiftGroup4,
      RoomGroup1,
      RoomGroup2,
      RoomGroup3,
      RoomGroup4
   }

   //identifiers for each light within a group (up to 8)
   public enum LightID
   {
      Light1 = 0,
      Light2,
      Light3,
      Light4,
      Light5,
      Light6,
      Light7,
      Light8
   }

   public enum LightTypes
   {
      Hue, //WiFi controlled, RGB lights
      LightJams //DMX controlled, dimmable lights
   }

   [System.Serializable]
   public class LightGroupConfig
   {
      [HideInInspector]
      public string GroupName; //editor hack that is filled in automatically so array looks pretty in editor

      public bool Enabled = true;
      public GroupID Group = GroupID.RiftGroup1;
      public Color DefaultColor = Color.white;
      public LightConfig[] Lights = new LightConfig[1];

   }
   
   [System.Serializable]
   public class LightConfig
   {
      public bool Enabled = true;
      public LightTypes LightType = LightTypes.LightJams;
      [Tooltip("Either the LightJams channel or index of light configured in the HueMessenger")]
      public int Channel;

      public void SetGroupConfig(LightGroupConfig gConfig) { _groupConfig = gConfig; }
      private LightGroupConfig _groupConfig = null;

      //state to implement transition time for LightJams
      private float _fromIntensity = 0.0f;
      private float _toIntensity = 0.0f;
      private float _lastIntensity = 0.0f;
      private float _transitionTime = 0.0f;
      private float _timeStamp = -1.0f;
      private int _debugNum = 0;


      public void Set(Color color, float intensity, float transitionTime = 0.0f)
      {
         if (!Enabled || !_groupConfig.Enabled)
            return;

         if (LightType == LightTypes.Hue)
         {
            HueMessenger.Instance.SetState(HueMessenger.Instance.FindLightWithChannel(Channel), (intensity > 0), intensity, color, transitionTime);
         }
         else if (LightType == LightTypes.LightJams)
         {
            if (transitionTime > 0)
            {
               _transitionTime = transitionTime;
               _timeStamp = Time.time;
               _fromIntensity = _lastIntensity;
               _toIntensity = intensity;
            }
            else
            {
               LightJamsMgr.Instance.SendToLightJams(Channel, intensity);

               _timeStamp = -1.0f;
               _lastIntensity = intensity;
            }
         }
      }

      public void Update()
      {
         //handle transition blend
         if (_timeStamp > 0.0f)
         {
            float u = Mathf.Clamp01((Time.time - _timeStamp) / _transitionTime);
            _lastIntensity = Mathf.Lerp(_fromIntensity, _toIntensity, u);
            LightJamsMgr.Instance.SendToLightJams(Channel, _lastIntensity);

            if (Mathf.Approximately(u, 1.0f)) //done?
               _timeStamp = -1.0f;
         }
      }

      public void SetDebugNum(int d) { _debugNum = d; }
      public int DebugNum() { return _debugNum; }
   }

   public class LightData
   {
      public LightData() { }
      public LightData(Color c, float i) { LightColor = c; LightIntensity = i; }
      public Color LightColor = Color.white;
      public float LightIntensity = 1.0f;
   }

   class BlendHueData
   {
      public BlendHueData(GroupID gID, LightID lID, LightData fromData, LightData toData, float transitionTime)
      {
         _gID = gID;
         _lID = lID;
         _fromData = fromData;
         _toData = toData;
         _transitionTime = transitionTime;

         _timeStamp = Time.time;
      }

      public GroupID GetGroupID() { return _gID; }
      public LightID GetLightID() { return _lID; }

      public void Update()
      {
         if (_timeStamp > 0.0f)
         {
            float u = Mathf.Clamp01((Time.time - _timeStamp) / _transitionTime);
            LightData blendedData = new LightData(Color.Lerp(_fromData.LightColor, _toData.LightColor, u), Mathf.Lerp(_fromData.LightIntensity, _toData.LightIntensity, u));

            EightNightsMgr.Instance.SendHueEvent(_gID, _lID, blendedData);

            if (Mathf.Approximately(u, 1.0f))
               _timeStamp = -1.0f;
         }
      }

      public bool IsDone()
      {
         return _timeStamp < 0.0f;
      }

      GroupID _gID;
      LightID _lID; 
      LightData _fromData;
      LightData _toData;
      float _transitionTime;

      float _timeStamp;

   }

   public bool TestLights = false;
   public LightGroupConfig[] LightGroups = new LightGroupConfig[1];

   public static EightNightsMgr Instance { get; private set; }

   List<BlendHueData> _eventBlends = new List<BlendHueData>();

   void Awake()
   {
      Instance = this;
   }

	void Start () 
   {
      //subscribe to updates from HueMessenger, which get forwarded to in-game effects triggers
      HueMessenger.Instance.OnLightChanged += OnHueLightChanged;

      //subscribe to updates from LightJamsMgr, which get forwarded to in-game effects triggers
      LightJamsMgr.Instance.OnLightChanged += OnLightJamsLightChanged;

      //generate the configuration for the HueMessenger based on our configuration
      HueMessenger.Instance.Lights = new HueMessenger.Light[NumLightsOfType(LightTypes.Hue)];
      int hueLightIdx = 0;
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig lg = LightGroups[i];
         for (int j = 0; j < lg.Lights.Length; j++)
         {
            LightConfig lc = lg.Lights[j];

            lc.SetGroupConfig(lg);

            if (lc.LightType == LightTypes.Hue)
            {
               HueMessenger.Light newLight = new HueMessenger.Light();
               newLight.on = false;
               newLight.color = lg.DefaultColor;
               newLight.id = lc.Channel;
               newLight.fade = 1.0f;
               HueMessenger.Instance.Lights[hueLightIdx] = newLight;
               hueLightIdx++;
            }
         }
      }

      //start with lights all off
      SetAllLights(0.0f, Color.white, 0.0f);
	}

   //a way to set the value of any light in the system, regardless of type (Hue, LightJams, whatevs)
   public void SetLight(GroupID gID, LightID lID, float intensity, Color color = default(Color), float transitionTime = 0.0f)
   {
      LightConfig lc = FindLightConfig(gID, lID);
      if (lc != null)
         lc.Set(color, intensity, transitionTime);
   }

   public void SetAllLights(float intensity, Color color = default(Color), float transitionTime = 0.0f)
   {
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig lg = LightGroups[i];
         for (int j = 0; j < lg.Lights.Length; j++)
         {
            LightConfig lc = lg.Lights[j];
            lc.Set(color, intensity, transitionTime);
         }
      }
   }
   
   //get latency for the given light.  Meaning this is how long it takes to set a value, and have it appear on the lights.
   public float GetLatency(GroupID gID, LightID lID)
   {
         LightConfig lc = FindLightConfig(gID, lID);
         if (lc != null)
         {
            if (lc.LightType == LightTypes.Hue)
               return HueMessenger.Instance.GetCurLatency();
            else
               return LightJamsMgr.Instance.GetCurLatency();
         }
         return 0.0f;
   }

   //finds the GroupID and LightID of a light with the given channel #
   //returns false if it doesnt find a compatible light in any of the groups
   bool FindLight(int channel, LightTypes lightType, ref GroupID gID, ref LightID lID)
   {
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig g = LightGroups[i];

         for (int j = 0; j < g.Lights.Length; j++)
         {
            LightConfig l = g.Lights[j];

            if ((l.Channel == channel) && (l.LightType == lightType))
            {
               gID = g.Group;
               lID = (LightID)j;
               return true;
            }
         }
      }

      return false;
   }

   LightGroupConfig FindGroupConfig(GroupID gId)
   {
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig g = LightGroups[i];
         if (g.Group == gId)
            return g;
      }

      return null;
   }

   LightConfig FindLightConfig(GroupID gId, LightID lId)
   {
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig g = LightGroups[i];
         if (g.Group == gId)
         {
            for (int j = 0; j < g.Lights.Length; j++)
            {
               if (j == (int)lId)
                  return g.Lights[j];
            }
         }
      }

      return null;
   }

   int NumLightsOfType(LightTypes t)
   {
      int count = 0;
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig lg = LightGroups[i];
         for (int j = 0; j < lg.Lights.Length; j++)
         {
            LightConfig lc = lg.Lights[j];
            if (lc.LightType == t)
               count++;
         }
      }
      return count;
   }

   void OnHueLightChanged(object sender, HueMessenger.HueEventArgs e)
   {
      if (OnLightChanged != null)
      {
         GroupID gID = GroupID.RiftGroup1;
         LightID lID = LightID.Light1;
         if (FindLight(e.LightID, LightTypes.Hue, ref gID, ref lID))
         {
            LightData newData = new LightData(e.LightColor, e.LightFade);

            //simulate fades
            if (e.TransitionTime > 0.0)
            {
               LightData fromData = new LightData(e.PrevColor, e.PrevFade);
               BlendHueData newBlendData = new BlendHueData(gID, lID, fromData, newData, e.TransitionTime);

               foreach (BlendHueData h in _eventBlends)
               {
                  if ((h.GetGroupID() == gID) && (h.GetLightID() == lID))
                  {
                     _eventBlends.Remove(h);
                     break;
                  }
               }

               _eventBlends.Add(newBlendData);
            }
            else
            {
               SendHueEvent(gID, lID, newData);
            }
         }
      }
   }

   public void SendHueEvent(GroupID gID, LightID lID, LightData newData)
   {
      if(OnLightChanged != null)
         OnLightChanged(this, new LightEventArgs(gID, lID, LightTypes.Hue, newData));
   }
   

   void OnLightJamsLightChanged(object sender, LightJamsMgr.LJEventArgs e)
   {
      if (OnLightChanged != null)
      {
         GroupID gID = GroupID.RiftGroup1;
         LightID lID = LightID.Light1;
         if (FindLight(e.Channel, LightTypes.LightJams, ref gID, ref lID))
         {
            LightGroupConfig config = FindGroupConfig(gID);
            OnLightChanged(this, new LightEventArgs(gID, lID, LightTypes.LightJams, new LightData(config.DefaultColor, e.Intensity)));
         }
      }
   }

	void Update () 
   {
      //update light configs to handle things like transitioning
      for (int i = 0; i < LightGroups.Length; i++)
      {
         LightGroupConfig lg = LightGroups[i];
         for (int j = 0; j < lg.Lights.Length; j++)
         {
            LightConfig lc = lg.Lights[j];
            lc.Update();
         }
      }

      List<BlendHueData> deleteMe = new List<BlendHueData>();
      foreach (BlendHueData b in _eventBlends)
      {
         b.Update();
         if (b.IsDone())
            deleteMe.Add(b);
      }
      foreach (BlendHueData b in deleteMe)
         _eventBlends.Remove(b);

      //toggle test mode with 't' key
      if (Input.GetKeyDown(KeyCode.T))
      {
         TestLights = !TestLights;
         //start with lights all off
         SetAllLights(0.0f, Color.white, 0.0f);
      }

      //run test patterns through all the lights
      if (TestLights)
      {
         Color[] testColors = new Color[] {  Color.red, Color.red, Color.green, Color.green, Color.blue, Color.blue, Color.yellow, Color.yellow };
         float[] testIntensities = new float[] { 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f };

         for (int i = 0; i < LightGroups.Length; i++)
         {
            LightGroupConfig lg = LightGroups[i];
            for (int j = 0; j < lg.Lights.Length; j++)
            {
               LightConfig lc = lg.Lights[j];

               float latency = GetLatency(lg.Group, (LightID)j);

               int testNum = ((int)(Time.time - latency));

               if (testNum != lc.DebugNum())
               {
                  Color testColor = testColors[testNum % testColors.Length];
                  float testIntensity = testIntensities[testNum % testIntensities.Length];
                  lc.Set(testColor, testIntensity, 1.0f);

                  lc.SetDebugNum(testNum);
               }
            }
         }
      }
	}
}
