//
// Loads MIDI from a particular track, and then sends out events as notes are passed by
//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NAudio.Midi;
using System;
using System.Reflection;

public class MIDIReceiver : MonoBehaviour
{


   //events
   public event MIDIHandler OnNoteOn; 
   public class MIDIReceiverEventArgs : EventArgs
   {
      public MIDIReceiverEventArgs(MIDIReceiver r, int midiNote, float beat, float durationBeats, float velocity) { Receiver = r; MidiNote = midiNote; NoteBeat = beat; DurationBeats = durationBeats; Velocity = velocity; }
      public int MidiNote;
      public float Velocity = 1.0f;
      public float NoteBeat = 0.0f;
      public float DurationBeats = 0.0f;
      public MIDIReceiver Receiver = null;
   }
   public delegate void MIDIHandler(object sender, MIDIReceiverEventArgs e);

   [Tooltip("Should be the path to the MIDI files with the .bytes extension, relative to Assets/Resources/ (leave off the file extension).")]
   public string MIDIResourcePath = "MIDI/Balafon";
   [Tooltip("The MIDI track # to read the data out of, starting at 0")]
   public int MIDITrackIdx = 0;
   public float MIDITimeMult = 1.0f;

   public float BeatOffset = 0.0f;

   public class NoteInfo
   {
      public int NoteNumber;
      public float NoteOnBeat;
      public float DurationBeats;
      public float Velocity = 1.0f;
   }

   class PrerollSubscriber
   {
      public PrerollSubscriber(Action<MIDIReceiverEventArgs> callback, float prerollSecs)
      {
         _callback = callback;
         _prerollSecs = prerollSecs;
      }

      public Action<MIDIReceiverEventArgs> Callback() { return _callback; }
      public float Preroll() { return _prerollSecs; }
      public void SetPreroll(float s) { _prerollSecs = s; }

      public void DoCallback(MIDIReceiverEventArgs e) { _callback.Invoke(e); }

      Action<MIDIReceiverEventArgs> _callback = null;
      float _prerollSecs = 0.0f;
   }

   private List<NoteInfo> _noteOns = null;
   private List<PrerollSubscriber> _preRollSubscribers = new List<PrerollSubscriber>();
   private float _prevBeat = 0.0f;

   public void AddOrUpdatePrerollSubscriber(Action<MIDIReceiverEventArgs> callback, float preroll)
   {
      //find existing subscriber
      PrerollSubscriber sub = null;
      foreach (PrerollSubscriber s in _preRollSubscribers)
      {
         if (s.Callback() == callback)
         {
            sub = s;
            //update preroll!
            sub.SetPreroll(preroll);
            break;
         }
      }

      //no existing entry?  create one!
      if (sub == null)
      {
         sub = new PrerollSubscriber(callback, preroll);
         _preRollSubscribers.Add(sub);
      }
   }

   public void RemovePrerollSubscriber(Action<MIDIReceiverEventArgs> callback)
   {
      PrerollSubscriber sub = null;
      foreach (PrerollSubscriber s in _preRollSubscribers)
      {
         if (s.Callback() == callback)
         {
            sub = s;
            break;
         }
      }

      if (sub != null)
         _preRollSubscribers.Remove(sub);
   }

   public void ReImportMIDI()
   {
      Debug.Log("MIDIReceiver: About to load " + MIDIResourcePath);
      MidiFile mid = new MidiFile(MIDIResourcePath, false);
      if (mid.Events == null)
      {
         Debug.Log("MIDIReceiver: Resource load failed- " + MIDIResourcePath);
         return;
      }

      _noteOns = new List<NoteInfo>();
      foreach (MidiEvent ev in mid.Events[MIDITrackIdx])
      {
         NoteOnEvent noteEvent = ev as NoteOnEvent;
         
         if (noteEvent != null)
         {
            try
            {
               NoteInfo newNote = new NoteInfo();
               newNote.NoteNumber = noteEvent.NoteNumber;
               newNote.NoteOnBeat = MIDITimeMult*((float)noteEvent.AbsoluteTime / (float)mid.DeltaTicksPerQuarterNote);
               newNote.DurationBeats = MIDITimeMult*((float)noteEvent.NoteLength / (float)mid.DeltaTicksPerQuarterNote);
               newNote.Velocity = noteEvent.Velocity / 127.0f;
               _noteOns.Add(newNote);

               //Debug.Log("  imported midi Note " + noteEvent.NoteNumber + " at beat " + newNote.NoteOnBeat + " duration " + newNote.DurationBeats);
            }
            catch (System.SystemException e) { Debug.Log("Error during midi import: " + e.Message); }
         }
      }
   }

   void Awake()
   {
      ReImportMIDI();
   }

   void Update()
   {
      if (_noteOns == null)
         return;

      float curBeat = BeatClock.Instance.elapsedBeats + BeatOffset;
      if (curBeat < 0.0f)
         curBeat = 0.0f;
      foreach (NoteInfo info in _noteOns)
      {
         if ((info.NoteOnBeat > _prevBeat) && (info.NoteOnBeat <= curBeat))
         {
            //Debug.Log("NOTE ON: " + info.NoteNumber);
            if (OnNoteOn != null)
               OnNoteOn(this, new MIDIReceiverEventArgs(this, info.NoteNumber, info.NoteOnBeat, info.DurationBeats, info.Velocity));
         }

         //do preroll subscribers
         foreach (PrerollSubscriber s in _preRollSubscribers)
         {
            float prerolledBeat = info.NoteOnBeat - s.Preroll();
            if ((prerolledBeat > _prevBeat) && (prerolledBeat <= curBeat))
            {
               s.DoCallback(new MIDIReceiverEventArgs(this, info.NoteNumber, info.NoteOnBeat, info.DurationBeats, info.Velocity));
            }
         }
      }

      _prevBeat = curBeat;
   }
}
