//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/**
 * An object that stores metadata necessary to properly define the tempo
 *  for a part of a song.
 */
[System.Serializable]
public class TempoSectionDef
{
	#region Fields

	[SerializeField]
	string sectionName = "New Section";		// Generally intended for Editor use only!

	[SerializeField]
	int startSample = 0;

	[SerializeField]
	float samplesPerBeat = 22050f;			// Defaults to 120bpm for songs at 44100 samples/second.

	[SerializeField]
	int beatsPerMeasure = 4;

	#endregion
	#region Properties

	public string SectionName
	{
		get
		{
			return sectionName;
		}
		set
		{
			sectionName = value;
		}
	}

	/**
	 * Guaranteed to be >= 0.
	 */
	public int StartSample
	{
		get
		{
			return startSample;
		}
		set
		{
			// Disallow non-negative startSamples.
			if (value < 0)
			{
				startSample = 0;
			}
			else
			{
				startSample = value;
			}
		}
	}

	/**
	 * Guaranteed to be greater than zero.
	 */
	public float SamplesPerBeat
	{
		get
		{
			return samplesPerBeat;
		}
		set
		{
			if (value <= 0)
			{
				samplesPerBeat = 1;
			}
			else
			{
				samplesPerBeat = value;
			}
		}
	}

	/**
	 * Guaranteed to be greater than zero.
	 */
	public int BeatsPerMeasure
	{
		get
		{
			return beatsPerMeasure;
		}
		set
		{
			if (value <= 0)
			{
				beatsPerMeasure = 1;
			}
			else
			{
				beatsPerMeasure = value;
			}
		}
	}

	#endregion
	#region Static Methods
	
	public static int CompareByStartSample(TempoSectionDef first, TempoSectionDef second)
	{
		if (first.StartSample < second.StartSample)
		{
			return -1;
		}
		else if (first.StartSample == second.StartSample)
		{
			return 0;
		}
		else
		{
			return 1;
		}
	}
	
	#endregion
	#region General Methods

	public float GetSamplesPerBeatSection(int numSubBeats = 0)
	{
		return SamplesPerBeat / (float)(numSubBeats + 1);
	}

	// Allow the caller to specify subbeats if they like (eigth notes, sixteenth notes, ...)
	public float GetBeatTimeFromSampleTime(int sampleTime, int numSubBeats = 0)
	{
		float beatTime = 0f;
		
		if (sampleTime >= startSample)
		{
			beatTime = (float)(sampleTime - startSample) / GetSamplesPerBeatSection(numSubBeats);
		}
		
		return beatTime;
	}

	public float GetMeasureTimeFromSampleTime(int sampleTime)
	{
		return GetBeatTimeFromSampleTime(sampleTime) / (float)BeatsPerMeasure;
	}

	#endregion
}

/**
 * A group of n-KoreographyTrack objects associated with a single
 * AudioClip object.  Each Track is uniquely tied to a single
 * Event ID.
 */
public class Koreography : ScriptableObject
{
	#region Fields

	[SerializeField]
	AudioClip mSourceClip = null;

	[SerializeField]
	List<TempoSectionDef> mTempoSections = new List<TempoSectionDef>(new TempoSectionDef[1]{new TempoSectionDef()});
	
	[SerializeField]
	List<KoreographyTrack> mTracks = new List<KoreographyTrack>();

	int lastUpdateStart = 0;
	int lastUpdateEnd = 0;

	#endregion
	#region Properties

	public AudioClip SourceClip
	{
		get
		{
			return mSourceClip;
		}
		set
		{
			foreach (KoreographyTrack track in mTracks)
			{
				track.SourceClip = value;
			}

			mSourceClip = value;
		}
	}

	// Returns a COPY of the original list.
	//  This grants access to tracks but does NOT
	//  allow for editing the track list.
	public List<KoreographyTrack> Tracks
	{
		get
		{
			return new List<KoreographyTrack>(mTracks);
		}
	}

	#endregion
	#region Maintenance Methods

	/**
	 * @return TRUE if there was a change to the list during this operation, FALSE otherwise.
	 */
	public bool CheckTrackListIntegrity()
	{
		int startLength = mTracks.Count;

		// Remove NULL entries.
		mTracks.RemoveAll(track => track == null);

		return (startLength != mTracks.Count);
	}

	/**
	 * @return TRUE if there was a change to the list during this operation, FALSE otherwise.
	 */
	public bool CheckTempoSectionListIntegrity()
	{
		int startLength = mTempoSections.Count;

		// Remove NULL entries.
		mTempoSections.RemoveAll(section => section == null);

		EnsureTempoSectionOrder();

		// TODO: determine if this should be done elsewhere.  This seems pretty safe, but...
		bool bDidAdjustFirst = false;
		if (mTempoSections[0].StartSample > 0)
		{
			mTempoSections[0].StartSample = 0;
			bDidAdjustFirst = true;
		}

		return bDidAdjustFirst || (startLength != mTracks.Count);
	}

	#endregion
	#region Tempo Section Management Methods

	public int GetIndexOfTempoSection(TempoSectionDef sectionDef)
	{
		return mTempoSections.IndexOf(sectionDef);
	}

	public TempoSectionDef InsertTempoSectionAtIndex(int idxToInsert)
	{
		TempoSectionDef newSectionDef = null;

		if (idxToInsert >= 0 && idxToInsert <= mTempoSections.Count)
		{
			newSectionDef = new TempoSectionDef();
			if (idxToInsert == mTempoSections.Count)
			{
				mTempoSections.Add(newSectionDef);
			}
			else
			{
				mTempoSections.Insert(idxToInsert, newSectionDef);
			}
		}

		return newSectionDef;
	}

	public bool RemoveTempoSectionAtIndex(int idxToRemove)
	{
		bool bDidRemove = false;

		// Only allow removal if we have more than one section.
		if (mTempoSections.Count > 1 &&
		    idxToRemove < mTempoSections.Count)
		{
			mTempoSections.RemoveAt(idxToRemove);
			bDidRemove = true;

			// Ensure that our first section is okay!
			if (idxToRemove == 0)
			{
				// Force the first sample to the beginning of the song.
				mTempoSections[0].StartSample = 0;
			}
		}

		return bDidRemove;
	}

	public bool RemoveTempoSection(TempoSectionDef sectionDef)
	{
		return RemoveTempoSectionAtIndex(GetIndexOfTempoSection(sectionDef));
	}

	public string[] GetTempoSectionNames()
	{
		return mTempoSections.Select(section => section.SectionName).ToArray();
	}

	public TempoSectionDef GetTempoSectionAtIndex(int sectionIdx)
	{
		return (sectionIdx >= 0 && sectionIdx < mTempoSections.Count) ? mTempoSections[sectionIdx] : null;
	}

	public int GetNumTempoSections()
	{
		return mTempoSections.Count;
	}

	public void EnsureTempoSectionOrder()
	{
		mTempoSections.Sort(TempoSectionDef.CompareByStartSample);
	}

	#endregion
	#region Track Management Methods

	public bool CanAddTrack(KoreographyTrack track)
	{
		return (track.SourceClip == null || track.SourceClip == track.SourceClip) && !DoesTrackWithEventIDExist(track.EventID);
	}

	public bool AddTrack(KoreographyTrack track)
	{
		bool bDidAdd = false;
		if (CanAddTrack(track))
		{
			track.SourceClip = SourceClip;
			mTracks.Add(track);

			bDidAdd = true;
		}
		return bDidAdd;
	}

	public void RemoveTrack(KoreographyTrack track)
	{
		mTracks.Remove(track);
	}

	public string[] GetEventIDs()
	{
		return (mTracks.Count > 0) ? mTracks.Select(track => track.EventID).ToArray() : new string[]{""};
	}

	public KoreographyTrack GetTrackAtIndex(int trackIdx)
	{
		return (trackIdx >= 0 && trackIdx < mTracks.Count) ? mTracks[trackIdx] : null;
	}

	public int GetIndexOfTrack(KoreographyTrack track)
	{
		return mTracks.IndexOf(track);
	}

	public int GetNumTracks()
	{
		return mTracks.Count;
	}

	public bool DoesTrackWithEventIDExist(string eventID)
	{
		return System.Array.Exists(GetEventIDs(), name => name == eventID);
	}

	#endregion
	#region Music Timing

	// TODO: Add a function called GetCurrentBeatTime(int subBeats) that returns the beat time based on
	//  the internal lastUpdateEnd field?

	public float GetBeatTimeDelta(int subBeats = 0)
	{
		return GetBeatTimeFromSampleTime(lastUpdateEnd, subBeats) - GetBeatTimeFromSampleTime(lastUpdateStart, subBeats);
	}

	public float GetBeatTimeFromSampleTime(int sampleTime, int subBeats = 0)
	{
		float beatTime = 0f;

		int destTempoSectionIdx = GetTempoSectionIndexForSample(sampleTime);

		for (int i = 1; i <= destTempoSectionIdx; ++i)
		{
			//  +1 because the GetBeatTimeFromSampleTime is 0-indexed and we want to build on it.
			beatTime += Mathf.Floor(mTempoSections[i - 1].GetBeatTimeFromSampleTime(mTempoSections[i].StartSample - 1)) + 1f;
		}

		return beatTime + mTempoSections[destTempoSectionIdx].GetBeatTimeFromSampleTime(sampleTime, subBeats);
	}

	public float GetMeasureTimeFromSampleTime(int sampleTime)
	{
		float measureTime = 0;
		
		int destTempoSectionIdx = GetTempoSectionIndexForSample(sampleTime);

		for (int i = 1; i <= destTempoSectionIdx; ++i)
		{
			// +1 because GetMeasureTimeFromSampleTime is 0-indexed and we want to build on it.
			measureTime += Mathf.Floor(mTempoSections[i - 1].GetMeasureTimeFromSampleTime(mTempoSections[i].StartSample - 1)) + 1f;
		}

		return measureTime + mTempoSections[destTempoSectionIdx].GetMeasureTimeFromSampleTime(sampleTime);
	}
	
	public int GetSampleTimeFromMeasureTime(int measure)
	{
		int i = 0;
		while (i < mTempoSections.Count - 1)
		{
			// Find the maximum measure of the current section (measure that contains the last sample before the next section).
			int curSectionNumMeasures = Mathf.FloorToInt(mTempoSections[i].GetMeasureTimeFromSampleTime(mTempoSections[i + 1].StartSample - 1));

			if (measure >= curSectionNumMeasures)
			{
				// Subtract out and continue!
				measure -= curSectionNumMeasures;
				++i;
			}
			else
			{
				// We're out!
				break;
			}
		}

		TempoSectionDef targetSection = mTempoSections[i];

		// When converting measures to samples we must be careful to use the next sample that is fully inside the measure.
		//  Rounding down could actually return us the last sample from the previous measure.
		return targetSection.StartSample + Mathf.CeilToInt((float)measure * (float)targetSection.BeatsPerMeasure * targetSection.SamplesPerBeat);
	}

	public float GetBeatCountInMeasureFromSampleTime(int sampleTime)
	{
		TempoSectionDef destTempoSection = GetTempoSectionForSample(sampleTime);

		return destTempoSection.GetBeatTimeFromSampleTime(sampleTime) % (float)destTempoSection.BeatsPerMeasure;
	}

	public float GetSamplesPerBeat(int checkSample, int subBeats = 0)
	{
		TempoSectionDef sectionForSample = GetTempoSectionForSample(checkSample);
		return sectionForSample.GetSamplesPerBeatSection(subBeats);
	}

	public int GetSampleOfNearestBeat(int checkSample, int subBeats = 0)
	{
		// Find TempoSection this sample is within.
		TempoSectionDef sampleSection = GetTempoSectionForSample(checkSample);

		float samplesPerBeat = sampleSection.GetSamplesPerBeatSection(subBeats);

		int distFromStart = checkSample - sampleSection.StartSample;

		return sampleSection.StartSample + (samplesPerBeat > 0 ? (int)(Mathf.Round((float)distFromStart / samplesPerBeat) * samplesPerBeat) : 0);
	}

	public TempoSectionDef GetTempoSectionForSample(int checkSample)
	{
		int sectionIdx = GetTempoSectionIndexForSample(checkSample);

		return (sectionIdx >= 0) ? mTempoSections[sectionIdx] : null;
	}

	public int GetTempoSectionIndexForSample(int checkSample)
	{
		int sectionIdx = -1;
		
		if (checkSample >= 0)
		{
			if (mTempoSections.Count == 1)
			{
				sectionIdx = 0;
			}
			else
			{
				int i = 0;
				while (i < mTempoSections.Count &&
				       checkSample >= mTempoSections[i].StartSample)
				{
					++i;
				}
				
				// We're in the previous entry.
				sectionIdx = i - 1;
			}
		}
		
		return sectionIdx;
	}

	#endregion
	#region Event Registration/Triggering
	
	public void UpdateTrackTime(int startTime, int endTime)
	{
		lastUpdateStart = startTime;
		lastUpdateEnd = endTime;

		foreach(KoreographyTrack track in mTracks)
		{
			track.CheckForEvents(startTime, endTime);
		}
	}
	
	public void RegisterForEvents(string eventDef, KoreographyEventCallback callback)
	{
		KoreographyTrack koreoTrack = mTracks.Find(x=>x.EventID == eventDef);

		if(koreoTrack != null)
		{
			koreoTrack.RegisterForEvents(callback);
		}
		else
		{
			Debug.LogWarning("WARNING: no Koreography Track with event definition '" + eventDef + "' to register with.");
		}
	}

	public void RegisterForEventsWithTime(string eventDef, KoreographyEventCallbackWithTime callback)
	{
		KoreographyTrack koreoTrack = mTracks.Find(x=>x.EventID == eventDef);

		if(koreoTrack != null)
		{
			koreoTrack.RegisterForEventsWithTime(callback);
		}
		else
		{
			Debug.LogWarning("WARNING: no Koreography Track with event definition '" + eventDef + "' to register with.");
		}
	}

	public void UnregisterForEvents(string eventDef, KoreographyEventCallback callback)
	{
		KoreographyTrack koreoTrack = mTracks.Find(x=>x.EventID == eventDef);

		if (koreoTrack != null)
		{
			koreoTrack.UnregisterForEvents(callback);
		}
		else
		{
			Debug.LogWarning("WARNING: no Koreography Track with event definition '" + eventDef + "' to unregister from.");
		}
	}

	public void UnregisterForEventsWithTime(string eventDef, KoreographyEventCallbackWithTime callback)
	{
		KoreographyTrack koreoTrack = mTracks.Find(x=>x.EventID == eventDef);

		if (koreoTrack != null)
		{
			koreoTrack.UnregisterForEventsWithTime(callback);
		}
		else
		{
			Debug.LogWarning("WARNING: no Koreography Track with event definition '" + eventDef + "' to unregister from.");
		}
	}

	public void ClearEventRegister()
	{
		foreach(KoreographyTrack track in mTracks)
			track.ClearEventRegister();
	}

	#endregion
}
