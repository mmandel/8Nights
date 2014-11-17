using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public struct OSCCallbackData
{
   public List<object> Data;
   public string Address;
};

public delegate void OSCCallback(OSCCallbackData data);

public class OSCMessenger : MonoBehaviour
{
   //singleton
    public static OSCMessenger Instance { get; private set; }

    public OSCMessenger()
    {
        Instance = this;
    }


   public static bool NoMaxMode = false;

   private long mLastPacketTimestamp = 0;
   private static bool mOscInitialized = false;
   private float noMaxEventTimer = 0;
   private float noMaxEventDelay = 0.1f;

   private class OSCReceiver
   {
      public string mResponseAddress;
      public OSCCallback mCallback;
      public int mValue;
   };
   private List<OSCReceiver> mScheduledEvents = new List<OSCReceiver>();
   private List<OSCReceiver> mPersistent = new List<OSCReceiver>();
   private List<OSCReceiver> mListeners = new List<OSCReceiver>();

   void Awake()
   {
      Instance = this;
      if (!mOscInitialized)
      {
         OSCHandler.Instance.Init();
         mOscInitialized = true;

         try
         {
            OSCHandler.Instance.SendMessageToClient("MaxMSP Out", "/ping", 1);
         }
         catch (System.Exception e)
         {
            e.GetHashCode();  //just do something with it to suppress "unused" warning about e
         }
      }

      Application.runInBackground = true;
   }
	
	void Update()
   {
      if (NoMaxMode)
      {
         HandleNoMax();
      }
      
      ReadOSC();
	}

   public void Quit()
   {

   }

   void HandleNoMax()
   {
      foreach (OSCReceiver scheduled in mScheduledEvents)
      {
         if (scheduled.mValue != 0)
         {
            OSCCallbackData empty = new OSCCallbackData();
            scheduled.mCallback(empty);
         }
      }
      mScheduledEvents.Clear();

      noMaxEventTimer += Time.deltaTime;
      
      if (noMaxEventTimer > noMaxEventDelay)
      {
         foreach (OSCReceiver persistent in mPersistent)
         {
            if (persistent.mValue != 0)
            {
               OSCCallbackData empty = new OSCCallbackData();
               persistent.mCallback(empty);
            }
         }

         noMaxEventTimer = 0f;
      }
   }
   
   void ReadOSC()
   {
      OSCHandler.Instance.UpdateLogs();
      
      Dictionary<string, ServerLog> servers = new Dictionary<string, ServerLog>();
      servers = OSCHandler.Instance.Servers;
      foreach( KeyValuePair<string, ServerLog> item in servers )
      {
         // If we have received at least one packet,
         // show the last received from the log in the Debug console
         if(item.Value.log.Count > 0) 
         {
            foreach (UnityOSC.OSCPacket packet in item.Value.packets)
            {
               if (packet.TimeStamp <= mLastPacketTimestamp)
                  continue;

               Debug.Log("Receive OSC: " + packet.Address);
  
               if (packet.Address == "/pong")
               {
                  NoMaxMode = false;
               }

               List<OSCReceiver> toRemove = new List<OSCReceiver>();;
               foreach (OSCReceiver currEvent in mScheduledEvents)
               {
                  if (currEvent.mResponseAddress == packet.Address)
                  {
                     toRemove.Add(currEvent);
                     OSCCallbackData data = new OSCCallbackData();
                     data.Data = packet.Data;
                     data.Address = packet.Address;
                     currEvent.mCallback(data);
                     break;
                  }
               }

               foreach (OSCReceiver remove in toRemove)
                  mScheduledEvents.Remove(remove);

               foreach (OSCReceiver persistent in mPersistent)
               {
                  if (persistent.mResponseAddress == packet.Address)
                  {
                     OSCCallbackData data = new OSCCallbackData();
                     data.Data = packet.Data;
                     data.Address = packet.Address;
                     persistent.mCallback(data);
                  }
               }

               foreach (OSCReceiver listener in mListeners)
               {
                  if (listener.mResponseAddress == packet.Address)
                  {
                     OSCCallbackData data = new OSCCallbackData();
                     data.Data = packet.Data;
                     data.Address = packet.Address;
                     listener.mCallback(data);
                  }
               }
            }
            
            mLastPacketTimestamp = item.Value.packets[item.Value.packets.Count-1].TimeStamp;
         }
      }
   }

   public void SendMessage(string address, int value)
   {
      //if (!NoMaxMode)
         OSCHandler.Instance.SendMessageToClient("MaxMSP Out", address, value);
   }

   public void SendMessage(string address, float value)
   {
      //if (!NoMaxMode)
         OSCHandler.Instance.SendMessageToClient("MaxMSP Out", address, value);
   }

   public void ScheduleEvent(string address, int value, string responseAddress, OSCCallback callback)
   {
      if (!NoMaxMode)
         OSCHandler.Instance.SendMessageToClient("MaxMSP Out", address, value);

      OSCReceiver newEvent = new OSCReceiver();
      newEvent.mResponseAddress = responseAddress;
      newEvent.mCallback = callback;
      newEvent.mValue = value;
      mScheduledEvents.Add(newEvent);
   }

   public void SetPersistent(string address, int value, string responseAddress, OSCCallback callback)
   {
      if (!NoMaxMode)
         OSCHandler.Instance.SendMessageToClient("MaxMSP Out", address, value);
      
      OSCReceiver persistent = new OSCReceiver();
      persistent.mResponseAddress = responseAddress;
      persistent.mCallback = callback;
      persistent.mValue = value;

      OSCReceiver found = mPersistent.Find(x => x.mResponseAddress == responseAddress);

      if (found == null)
         mPersistent.Add(persistent);
      else
         found.mValue = value;
   }

   public void AddListener(string responseAddress, OSCCallback callback)
   {  
      OSCReceiver listener = new OSCReceiver();
      listener.mResponseAddress = responseAddress;
      listener.mCallback = callback;
      
      mListeners.Add(listener);
   }

   public void RemoveListener(string responseAddress, OSCCallback callback)
   {
      mListeners.RemoveAll(x => (x.mResponseAddress == responseAddress && x.mCallback == callback));
   }
}
