//
// Loads MIDI from a particular track, and then sends out events as notes are passed by
//

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NAudio.Midi;
using System;

public class MIDIReceiver : MonoBehaviour
{


   //events
   public event MIDIHandler OnNoteOn; 
   public class MIDIReceiverEventArgs : EventArgs
   {
      public MIDIReceiverEventArgs(int midiNote, float beat, float durationBeats) { MidiNote = midiNote; NoteBeat = beat; DurationBeats = durationBeats; }
      public int MidiNote;
      public float NoteBeat = 0.0f;
      public float DurationBeats = 0.0f;
   }
   public delegate void MIDIHandler(object sender, MIDIReceiverEventArgs e);

   [Tooltip("Should be the path to the MIDI files with the .bytes extension, relative to Assets/Resources/ (leave off the file extension).")]
   public string MIDIResourcePath = "MIDI/Balafon";
   [Tooltip("The MIDI track # to read the data out of, starting at 0")]
   public int MIDITrackIdx = 0;

   public class NoteInfo
   {
      public int NoteNumber;
      public float NoteOnBeat;
      public float DurationBeats;
   }

   private List<NoteInfo> _noteOns = null;
   private float _prevBeat = 0.0f;

   public void ReImport()
   {
      MidiFile mid = new MidiFile(MIDIResourcePath);
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
               newNote.NoteOnBeat = ((float)noteEvent.AbsoluteTime / (float)mid.DeltaTicksPerQuarterNote);
               newNote.DurationBeats = ((float)noteEvent.NoteLength / (float)mid.DeltaTicksPerQuarterNote);
               _noteOns.Add(newNote);

               Debug.Log("  imported midi Note " + noteEvent.NoteNumber + " at beat " + newNote.NoteOnBeat + " duration " + newNote.DurationBeats);
            }
            catch (System.SystemException e) { Debug.Log("Error during midi import: " + e.Message); }
         }
      }
   }

   void Awake()
   {
      ReImport();
   }

   void Update()
   {
      if (_noteOns == null)
         return;

      float curBeat = BeatClock.Instance.elapsedBeats;
      foreach (NoteInfo info in _noteOns)
      {
         if ((info.NoteOnBeat > _prevBeat) && (info.NoteOnBeat <= curBeat))
         {
            //Debug.Log("NOTE ON: " + info.NoteNumber);
            if (OnNoteOn != null)
               OnNoteOn(this, new MIDIReceiverEventArgs(info.NoteNumber, info.NoteOnBeat, info.DurationBeats));
         }
      }

      _prevBeat = curBeat;
   }
}
