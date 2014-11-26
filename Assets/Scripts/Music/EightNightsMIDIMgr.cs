﻿//
// Deals with associating MIDI events with each light group
//

using UnityEngine;
using System.Collections;
using System;

public class EightNightsMIDIMgr : MonoBehaviour
{
   public MIDIConfig[] MIDIConfigs;

   //events
   public event EightNightsMIDIHandler OnNoteOn;
   public class EightNightsMIDIEventArgs : EventArgs
   {
      public EightNightsMIDIEventArgs(int midiNote, float beat, float durationBeats, EightNightsMgr.GroupID g) { Group = g; MidiNote = midiNote; NoteBeat = beat; DurationBeats = durationBeats; }
      EightNightsMgr.GroupID Group;
      public int MidiNote;
      public float NoteBeat = 0.0f;
      public float DurationBeats = 0.0f;
   }
   public delegate void EightNightsMIDIHandler(object sender, EightNightsMIDIEventArgs e);

   [System.Serializable]
   public class MIDIConfig
   {
      public EightNightsMgr.GroupID Group;
      public MIDIReceiver MIDIReceiver;
      public MIDIReceiver.MIDIReceiverEventArgs LastNoteOnEvent = null;
   }


   public MIDIConfig FindMIDIConfig(EightNightsMgr.GroupID group)
   {
      foreach (MIDIConfig c in MIDIConfigs)
      {
         if (c.Group == group)
            return c;
      }
      return null;
   }

   public static EightNightsMIDIMgr Instance { get; private set; }

   void Awake()
   {
      Instance = this;
   }

   void Start()
   {
      //register for MIDI events
      foreach (MIDIConfig c in MIDIConfigs)
      {
         c.MIDIReceiver.OnNoteOn += OnMIDIReceiverNoteOn;
      }
   }

   void OnMIDIReceiverNoteOn(object sender, MIDIReceiver.MIDIReceiverEventArgs e)
   {
      MIDIReceiver receiver = sender as MIDIReceiver;
      if (receiver != null)
      {
         foreach (MIDIConfig c in MIDIConfigs)
         {
            if (c.MIDIReceiver == receiver)
            {
               c.LastNoteOnEvent = e;
               //Debug.Log("NOTE ON " + e.MidiNote + " for Group: " + c.Group.ToString() + " Beat: " + e.NoteBeat + " curBeat: " + BeatClock.Instance.elapsedBeats + " curSecs: " + BeatClock.Instance.elapsedSecs);
               if (OnNoteOn != null)
               {
                  OnNoteOn(this, new EightNightsMIDIEventArgs(e.MidiNote, e.NoteBeat, e.DurationBeats, c.Group));
               }
            }
         }
      }
   }

   void OnGUI()
   {
      if (!EightNightsAudioMgr.Instance.ShowTestUI)
         return;

      float HSpacing = 135;
      Vector2 badgeSize = new Vector2(120, 120);

      Vector2 startPos = new Vector2((.5f * Screen.width) - (MIDIConfigs.Length*badgeSize.x*.5f) - 50, .5f * Screen.height);

      foreach (MIDIConfig c in MIDIConfigs)
      {
         Color origGUIColor = GUI.color;
         float badgeAlpha = 1.0f;
         if(c.LastNoteOnEvent != null)
         {
            float noteProgressU = Mathf.InverseLerp(c.LastNoteOnEvent.NoteBeat, c.LastNoteOnEvent.NoteBeat + 1.0f, BeatClock.Instance.elapsedBeats);
            badgeAlpha = Mathf.Lerp(.6f, 1.0f, 1.0f - noteProgressU);
         }
         float volumeMult = EightNightsAudioMgr.Instance.MusicPlayer.GetVolumeForGroup(c.Group);
         Color curGUIColor = origGUIColor;
         curGUIColor.a = volumeMult*badgeAlpha;
         GUI.color = curGUIColor;

         Rect badgeRect = new Rect(startPos.x, startPos.y, badgeSize.x, badgeSize.y);
         GUI.Box(badgeRect,"");

         GUI.color = origGUIColor;

         curGUIColor.a = volumeMult;
         GUI.color = curGUIColor;

         Rect titleRect = badgeRect;
         titleRect.x += 15;
         GUI.Label(titleRect, c.Group.ToString() + " MIDI");
         GUI.Label(new Rect(badgeRect.center.x - 10, badgeRect.center.y - 10, 30, 30), (c.LastNoteOnEvent != null) ? c.LastNoteOnEvent.MidiNote.ToString() : "");

         GUI.color = origGUIColor;

         startPos.x += HSpacing;

      }
   }
}
