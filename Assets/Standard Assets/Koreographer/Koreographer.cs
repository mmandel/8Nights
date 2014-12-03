//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

public interface KoreographerInterface
{
	int GetSampleTimeForClip(AudioClip clip);
	bool GetIsPlaying(AudioClip clip);
	float GetPitch();
	AudioClip GetCurrentClip();
}

class EventObj
{
	event KoreographyEventCallback basicEvent;
	event KoreographyEventCallbackWithTime timedEvent;

	public void Register(KoreographyEventCallback callback)
	{
		basicEvent += callback;
	}

	public void Register(KoreographyEventCallbackWithTime callback)
	{
		timedEvent += callback;
	}

	public void Unregister(KoreographyEventCallback callback)
	{
		basicEvent -= callback;
	}

	public void Unregister(KoreographyEventCallbackWithTime callback)
	{
		timedEvent -= callback;
	}

	public void UnregisterByObject(System.Object obj)
	{
		System.Delegate[] delegates;

		if (basicEvent != null)
		{
			delegates = basicEvent.GetInvocationList();

			for (int i = 0; i < delegates.Length; ++i)
			{
				if (delegates[i].Target == obj)
				{
					basicEvent -= (KoreographyEventCallback)delegates[i];
					break;
				}
			}
		}

		if (timedEvent != null)
		{
			delegates = timedEvent.GetInvocationList();

			for (int i = 0; i < delegates.Length; ++i)
			{
				if (delegates[i].Target == obj)
				{
					timedEvent -= (KoreographyEventCallbackWithTime)delegates[i];
					break;
				}
			}
		}
	}

	public void ClearRegistrations()
	{
		basicEvent = null;
		timedEvent = null;
	}

	public bool IsClear()
	{
		return (basicEvent == null) && (timedEvent == null);
	}

	public void Trigger(KoreographyEvent evt, int sampleTime, int sampleDelta)
	{
		if (basicEvent != null)
		{
			basicEvent(evt);
		}

		if (timedEvent != null)
		{
			timedEvent(evt, sampleTime, sampleDelta);
		}
	}
}

/// <summary>
/// The Koreographer is the object responsible for triggering events.  It takes the current music
///  point, querries the Koreography for events at those times, and then sends cues to "actors" that
///  are listening for directions.
/// This setup *does not* stop an "actor" from looking directly at a specific Koreography Track.
///  This is fully supported.  It just means that that actor will only get cues for the Tracks for
///  which it has registered.
/// The music is "reported" to the Koreographer.  In this sense, the Koreographer "Hears" the
///  music.
/// 
/// The Koreographer manages a list of Koreography.  These are considered "loaded" and any
///  time the associated music is played, their events are triggered.
/// 
/// When Koreography is loaded, the Koreographer finds-or-adds a string->EventObj mapping to a managed
///  list.  This allows the Koreographer to get notified when Koreography Events occur directly from the
///  triggered tracks.  The events are then automatically forwarded to those who registered for
///  such events from the Koreographer.
/// </summary>
[AddComponentMenu("Koreographer/Koreographer")]
public class Koreographer : MonoBehaviour
{
	static Koreographer _instance = null;
	public static Koreographer Instance { get { return _instance; } }

	public KoreographerInterface musicPlaybackController = null;	// Set this in the inspector!

	[SerializeField]
	List<Koreography> loadedKoreography = new List<Koreography>();

	// Like an operator's switch board.
	List<KeyValuePair<string,EventObj>> eventBoard = new List<KeyValuePair<string, EventObj>>();

	void Awake()
	{
		_instance = this;
	}

	#region Choreography Processing

	public void ProcessChoreography(AudioClip clip, int startTime, int endTime)
	{
		foreach(Koreography koreo in loadedKoreography)
		{
			if (koreo.SourceClip == clip)
			{
				koreo.UpdateTrackTime(startTime, endTime);
			}
		}
	}

	#endregion
	#region Koreography Management

	public void LoadKoreography(Koreography koreo)
	{
		if (koreo != null && !loadedKoreography.Contains(koreo))
		{
			foreach (KeyValuePair<string,EventObj> mapping in eventBoard)
			{
				// Tie the Koreography to pre-existing event requests.
				koreo.RegisterForEventsWithTime(mapping.Key, mapping.Value.Trigger);
			}

			loadedKoreography.Add(koreo);
		}
	}

	public void UnloadKoreography(Koreography koreo)
	{
		if (koreo != null && loadedKoreography.Contains(koreo))
		{
			foreach (KeyValuePair<string,EventObj> mapping in eventBoard)
			{
				if(koreo.DoesTrackWithEventIDExist(mapping.Key))
				{
					// Untie the Koreography from existing event requests.
					koreo.UnregisterForEventsWithTime(mapping.Key, mapping.Value.Trigger);
				}
			}

			loadedKoreography.Remove(koreo);
		}
	}

	public bool IsKoreographyLoaded(Koreography koreo)
	{
		return loadedKoreography.Contains(koreo);
	}

	#endregion
	#region Event Callback Registration
	
	public void RegisterForEvents(string eventID, KoreographyEventCallback callback)
	{
		if (string.IsNullOrEmpty(eventID))
		{
			Debug.LogError("Cannot register for events with an empty event identifier!");
		}
		else
		{
			KeyValuePair<string,EventObj> mapping = eventBoard.Find(x=>x.Key == eventID);

			// KeyValuePair generics treat the key as a property, which can return null.
			if (string.IsNullOrEmpty(mapping.Key))
			{
				mapping = new KeyValuePair<string, EventObj>(eventID, new EventObj());
				eventBoard.Add(mapping);
				
				// New Mapping (we haven't encountered this event ID before).  Register with previously
				//  loaded Koreography!
				// Adds the Koreographer->Koreography link.
				ConnectEventToLoadedKoreography(mapping);
			}
			
			// Add the Obj->Koreographer link.
			mapping.Value.Register(callback);
		}
	}
	
	public void RegisterForEventsWithTime(string eventID, KoreographyEventCallbackWithTime callback)
	{
		if (string.IsNullOrEmpty(eventID))
		{
			Debug.LogError("Cannot register for events with an empty event identifier!");
		}
		else
		{
			KeyValuePair<string,EventObj> mapping = eventBoard.Find(x=>x.Key == eventID);

			// KeyValuePair generics treat the key as a property, which can return null.
			if (string.IsNullOrEmpty(mapping.Key))
			{
				mapping = new KeyValuePair<string, EventObj>(eventID, new EventObj());
				eventBoard.Add(mapping);
				
				// New Mapping (we haven't encountered this event ID before).  Register with previously
				//  loaded Koreography!
				// Adds the Koreographer->Koreography link.
				ConnectEventToLoadedKoreography(mapping);
			}
			
			// Add the Obj->Koreographer link.
			mapping.Value.Register(callback);
		}
	}
	
	public void UnregisterForEvents(string eventID, KoreographyEventCallback callback)
	{
		if (string.IsNullOrEmpty(eventID))
		{
			Debug.LogError("Cannot unregister for events with an empty event identifier!");
		}
		else
		{
			KeyValuePair<string,EventObj> mapping = eventBoard.Find(x=>x.Key == eventID);

			// KeyValuePair generics treat the key as a property, which can return null.
			if (!string.IsNullOrEmpty(mapping.Key))
			{
				// Remove the Obj->Koreographer link.
				mapping.Value.Unregister(callback);
				
				if (mapping.Value.IsClear())
				{
					// If there isn't a reason for this to exist anymore, clean it up!
					
					// Remove the Koreographer->Koreography link.
					DisconnectEventFromLoadedKoreography(mapping);
					
					eventBoard.Remove(mapping);
				}
			}
		}
	}
	
	public void UnregisterForEvents(string eventID, KoreographyEventCallbackWithTime callback)
	{
		if (string.IsNullOrEmpty(eventID))
		{
			Debug.LogError("Cannot unregister for events with an empty event identifier!");
		}
		else
		{
			KeyValuePair<string,EventObj> mapping = eventBoard.Find(x=>x.Key == eventID);

			// KeyValuePair generics treat the key as a property, which can return null.
			if (!string.IsNullOrEmpty(mapping.Key))
			{
				// Remove the Obj->Koreographer link.
				mapping.Value.Unregister(callback);
				
				if (mapping.Value.IsClear())
				{
					// If there isn't a reason for this to exist anymore, clean it up!
					
					// Remove the Koreographer->Koreography link.
					DisconnectEventFromLoadedKoreography(mapping);

					eventBoard.Remove(mapping);
				}
			}
		}
	}

	public void UnregisterForAllEvents(System.Object obj)
	{
		// Go backwards as this loop may shrink the eventBoard list.
		for (int i = eventBoard.Count - 1; i >= 0; --i)
		{
			KeyValuePair<string,EventObj> mapping = eventBoard[i];

			mapping.Value.UnregisterByObject(obj);

			if (mapping.Value.IsClear())
			{
				// If there isn't a reason for this to exist anymore, clean it up!

				// Remove the Koreographer->Koreography link.
				DisconnectEventFromLoadedKoreography(mapping);

				eventBoard.Remove(mapping);
			}
		}
	}
	
	public void ClearEventRegister()
	{
		foreach (KeyValuePair<string,EventObj> mapping in eventBoard)
		{
			// Remove Obj->Koreographer links.
			mapping.Value.ClearRegistrations();

			// Remove the Koreographer->Koreography link.
			DisconnectEventFromLoadedKoreography(mapping);
		}
		
		// Releases all mappings.
		eventBoard.Clear();
	}

	void ConnectEventToLoadedKoreography(KeyValuePair<string,EventObj> mapping)
	{
		// Adds the Koreographer->Koreography link.
		foreach (Koreography koreo in loadedKoreography)
		{
			if (koreo.DoesTrackWithEventIDExist(mapping.Key))
			{
				koreo.RegisterForEventsWithTime(mapping.Key, mapping.Value.Trigger);
			}
		}
	}

	void DisconnectEventFromLoadedKoreography(KeyValuePair<string,EventObj> mapping)
	{
		// Remove Koreographer->Koreography links.
		foreach (Koreography koreo in loadedKoreography)
		{
			if (koreo.DoesTrackWithEventIDExist(mapping.Key))
			{
				koreo.UnregisterForEventsWithTime(mapping.Key, mapping.Value.Trigger);
			}
		}
	}
	
	#endregion
	#region Beat Timing

	protected float GetBeatTimeDeltaInternal(AudioClip clip = null, int subBeats = 0)
	{
		float deltaTime = 0f;

		if (clip == null && musicPlaybackController != null)
		{
			clip = musicPlaybackController.GetCurrentClip();
		}

		Koreography koreo = loadedKoreography.Find(x=>x.SourceClip == clip);

		if (koreo != null)
		{
			deltaTime = koreo.GetBeatTimeDelta(subBeats);
		}

		return deltaTime;
	}
	
	protected float GetCurrentBeatTime(AudioClip clip = null, int subBeats = 0)
	{
		float beatTime = -1f;
		
		if (clip == null && musicPlaybackController != null)
		{
			clip = musicPlaybackController.GetCurrentClip();
		}
		
		Koreography koreo = loadedKoreography.Find(x=>x.SourceClip == clip);
		
		if (koreo != null)
		{
			beatTime = koreo.GetBeatTimeFromSampleTime(musicPlaybackController.GetSampleTimeForClip(clip), subBeats);
		}
		
		return beatTime;
	}
	
	#endregion
	#region Static Accessors

	public static int GetSampleRate()
	{
		Koreographer grapher = Koreographer.Instance;

		int sampleRate = 44100;	// Standard default.

		if (grapher != null && grapher.musicPlaybackController != null)
		{
			AudioClip clip = grapher.musicPlaybackController.GetCurrentClip();

			if (clip != null)
			{
				sampleRate = clip.frequency;
			}
		}

		return sampleRate;
	}

	public static int GetTotalSampleTime()
	{
		Koreographer grapher = Koreographer.Instance;

		int totalSampleTime = 0;

		if (grapher != null && grapher.musicPlaybackController != null)
		{
			AudioClip clip = grapher.musicPlaybackController.GetCurrentClip();

			if (clip != null)
			{
				totalSampleTime = clip.samples;
			}
		}

		return totalSampleTime;
	}

	public static int GetSampleTime(AudioClip clip = null)
	{
		Koreographer grapher = Koreographer.Instance;

		int sampleTime = 0;

		if (grapher != null)
		{
			if (clip == null)
			{
				clip = grapher.musicPlaybackController.GetCurrentClip();
			}

			sampleTime = grapher.musicPlaybackController.GetSampleTimeForClip(clip);
		}

		return sampleTime;
	}

	public static float GetBeatTimeDelta(AudioClip clip = null, int subBeats = 0)
	{
		Koreographer grapher = Koreographer.Instance;

		float deltaTime = 0f;

		if (grapher != null)
		{
			deltaTime = grapher.GetBeatTimeDeltaInternal(clip, subBeats);
		}

		return deltaTime;
	}

	public static float GetBeatTime(AudioClip clip = null, int subBeats = 0)
	{
		Koreographer grapher = Koreographer.Instance;

		float beatTime = -1f;

		if (grapher != null)
		{
			beatTime = grapher.GetCurrentBeatTime(clip, subBeats);
		}

		return beatTime;
	}

	public static float GetBeatTimeNormalized(AudioClip clip = null, int subBeats = 0)
	{
		return Koreographer.GetBeatTime(clip, subBeats) % 1f;
//		float beatTime = Koreographer.GetBeatTime(clip, subBeats);
//		return beatTime - Mathf.Floor(beatTime);
	}

	#endregion
}
