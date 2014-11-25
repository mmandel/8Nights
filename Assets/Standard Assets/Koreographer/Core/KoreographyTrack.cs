//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/**
 * TODO: Fix this - shouldn't need the AudioClip reference.
 * A single list of events associated with an AudioClip object.
 * This class guarantees that all events in the track are
 * written in order.
 */

public delegate void KoreographyEventCallback(KoreographyEvent koreoEvent);
public delegate void KoreographyEventCallbackWithTime(KoreographyEvent koreoEvent, int sampleTime, int sampleDelta);

[System.Serializable]
public partial class KoreographyTrack : ScriptableObject
{
	#region Fields

	[SerializeField]
	AudioClip mSourceClip = null;

	[SerializeField]
	string mEventID = string.Empty;

	[SerializeField]
	List<KoreographyEvent> mEventList = new List<KoreographyEvent>();

	public event KoreographyEventCallback koreographyEventCallbacks;
	public event KoreographyEventCallbackWithTime koreographyEventCallbacksWithTime;

	#endregion
	#region Properties

	public string EventID
	{
		get
		{
			return mEventID;
		}
		set
		{
			mEventID = value;
		}
	}

	public AudioClip SourceClip
	{
		get
		{
			return mSourceClip;
		}
		set
		{
			mSourceClip = value;
		}
	}

	#endregion
	#region Maintenance Methods

	/**
	 * @return TRUE if there was a change to the list during this operation, FALSE otherwise.
	 */
	public bool CheckEventListIntegrity()
	{
		int startLength = mEventList.Count;
		
		// Remove NULL entries.
		mEventList.RemoveAll(e => e == null);
		
		return (startLength != mEventList.Count);
	}

	#endregion
	#region General Methods

	public int GetIDForEvent(KoreographyEvent e)
	{
		// Returns -1 if not in the list!
		return mEventList.IndexOf(e);
	}

	public void EnsureEventOrder()
	{
		mEventList.Sort(KoreographyEvent.CompareByStartSample);
	}

	public KoreographyEvent GetEventAtStartSample(int sample)
	{
		return mEventList.Find(e => e.StartSample == sample);
	}

	/**
	 * Adds an event to the track.  The event is inserted in order by
	 * sample time.  The event is NOT added if it is a OneOff event
	 * and another OneOff event exists at the same time.
	 * 
	 * TODO: Remove the "no duplicate OneOffs" requirement(?).
	 */
	// Editor only?
	public bool AddEvent(KoreographyEvent addEvent)
	{
		// Check/verify that the event fits within the song?

		bool bAdd = true;

		if (addEvent.IsOneOff())
		{
			KoreographyEvent sameStartEvent = GetEventAtStartSample(addEvent.StartSample);
			if (sameStartEvent != null && sameStartEvent.IsOneOff())
			{
				// Disallow the add!
				bAdd = false;
			}
		}

		if (bAdd)
		{
			mEventList.Add(addEvent);
			EnsureEventOrder();
		}

		return bAdd;
	}

	public bool RemoveEvent(KoreographyEvent removeEvent)
	{
		return mEventList.Remove(removeEvent);
	}

	/**
	 * Returns a list of events in the provided range.  This includes events that intersect
	 * the range.
	 * 
	 * This check is inclusive of Start/End ranges!
	 */
	public List<KoreographyEvent> GetEventsInRange(int startSample, int endSample)
	{
		List<KoreographyEvent> eventsInRange = new List<KoreographyEvent>();
		
		foreach (KoreographyEvent e in mEventList)
		{
			// Find out which events are in the provided range.
			if ((e.StartSample >= startSample && e.StartSample <= endSample)	// Start in range.
			    || (e.EndSample >= startSample && e.EndSample <= endSample)		// End in range.
			    || (e.StartSample <= startSample && e.EndSample >= endSample))	// Beyond both ends.
			{
				eventsInRange.Add(e);
			}
			// Stop the search once we're starting afresh beyond the end of the range.
			else if (e.StartSample > endSample)
			{
				break;
			}
		}

		return eventsInRange;
	}

	/**
	 * Returns a list with all events in the track.
	 */
	public List<KoreographyEvent> GetAllEvents()
	{
		return new List<KoreographyEvent>(mEventList);
	}

	#endregion
	#region Event Registration/Triggering
	
	public void CheckForEvents(int startTime, int endTime)
	{
		// Check inclusive for Start/End times!!  This simplifies the logic a bit.
		List<KoreographyEvent> eventsToUpdate = mEventList.Where(x=>
		                                                    (x.StartSample >= startTime && x.StartSample <= endTime) || 		// Straddle start sample?
		                                                    (x.EndSample <= endTime && x.EndSample >= startTime && !x.IsOneOff()) || // Straddle end sample?
		                                                    (x.StartSample <= startTime && x.EndSample >= endTime)).ToList();	// Straddle both ends?
		foreach(KoreographyEvent e in eventsToUpdate)
		{
			if(koreographyEventCallbacks != null)
			{
				koreographyEventCallbacks(e);
			}
			if(koreographyEventCallbacksWithTime != null)
			{
				int delta = endTime - startTime;
				koreographyEventCallbacksWithTime(e, endTime, delta);
			}
		}
	}
	
	public void RegisterForEvents(KoreographyEventCallback callback)
	{
		koreographyEventCallbacks += callback;
	}

	public void RegisterForEventsWithTime(KoreographyEventCallbackWithTime callback)
	{
		koreographyEventCallbacksWithTime += callback;
	}

	public void UnregisterForEvents(KoreographyEventCallback callback)
	{
		koreographyEventCallbacks -= callback;
	}

	public void UnregisterForEventsWithTime(KoreographyEventCallbackWithTime callback)
	{
		koreographyEventCallbacksWithTime -= callback;
	}

	public void ClearEventRegister()
	{
		koreographyEventCallbacks = null;
		koreographyEventCallbacksWithTime = null;
	}

	#endregion
}

/**
 * ===Koreography Payload Serialization Implementation===
 *  Each Payload class must implement the KoreographyPayload interface
 *   and provide a partial KoreographyTrack class implementation. Each
 *   such partial class must inlude two lists of the following formats:
 * 
 *     [SerializeField][HideInInspector]
 *     List<[PayloadClass]> _[PayloadClass]s;
 * 
 *     [SerializeField][HideInInspector]
 *     List<int>			_[PayloadClass]Idxs;
 * 
 *  The [SerializeField] attribute must exist, while the 
 *   [HideInInspector] field remains optional but is recommended.
 * 
 *  These lists are used by the Serialization system through
 *   reflection to actually store the KoreographyEvent Payloads across
 *   session boundaries.
 */
public partial class KoreographyTrack : ISerializationCallbackReceiver
{
	#region Serialization Handling
	
	[HideInInspector][SerializeField]
	List<string>	_SerializedPayloadTypes;
	
	#endregion
	#region Serialization Interface and Support Methods
	
//	void ResetPayloadLists()
//	{
//		System.Type[] payloadTypes = KoreographyEvent.GetPayloadTypes();
//		
//		foreach (System.Type plType in payloadTypes)
//		{
//			// Grab the fields from the type.
//			System.Reflection.FieldInfo plListInfo = GetType().GetField("_" + plType.ToString() + "s", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
//			System.Reflection.FieldInfo plListIdxsInfo = GetType().GetField("_" + plType.ToString() + "Idxs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
//			
//			// Set the fields to null.
//			plListInfo.SetValue(this, null);
//			plListIdxsInfo.SetValue(this, null);
//		}
//	}
	
	public void OnBeforeSerialize()
	{
		// We must clear the payload lists.  This is to make sure that we don't have any data stored
		//  hidden away somewhere.  In theory this could happen when the user deletes the last few
		//  entries of a specific type from the payloads.  When "Save" was called, the lists might
		//  get generated and store the data.  Then, if there's no more references to that type in the
		//  KoreographyEvents list, we wouldn't check it here and we wouldn't know to check it in
		//  OnAfterDeserialize, either.  By resetting the payload lists here, we make sure we have a
		//  clean slate prior to reading out the actual payloads from the KoreographyEvents list.
		if (_SerializedPayloadTypes == null)
		{
			_SerializedPayloadTypes = new List<string>();
		}
		else
		{
			_SerializedPayloadTypes.Clear();
		}

		// Maps for storing Type->Field links (means we only have to reflect twice per Type).
		Dictionary<System.Type, System.Reflection.FieldInfo> plMaps = new Dictionary<System.Type, System.Reflection.FieldInfo>();
		Dictionary<System.Type, System.Reflection.FieldInfo> plIdxMaps = new Dictionary<System.Type, System.Reflection.FieldInfo>();

		// Grab the list of all Payload Types.
		System.Type[] payloadTypes = KoreographyEvent.GetPayloadTypes();

		// Loop over the payload types, storing references to each storage field in the
		//  KoreographyTrack.  Also set them to null so we're on a clean slate.
		foreach (System.Type plType in payloadTypes)
		{
			// Prepare the lists for serialization, erroring if we can't find them.
			//  1) Grab the fields from the type.
			//  2) Set the fields to null.
			//  3) Store the references so we can recall them later in the function.

			// Data storage.
			System.Reflection.FieldInfo plListInfo = GetType().GetField("_" + plType.ToString() + "s", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (plListInfo != null)
			{
				plListInfo.SetValue(this, null);
			}
			else
			{
				Debug.LogError("Serialization Error: No 'List<" + plType.ToString() + "> _" + plType.ToString() + "s' defined in KoreographyTrack class!");
			}
			plMaps.Add(plType, plListInfo);

			// Index storage.
			System.Reflection.FieldInfo plListIdxsInfo = GetType().GetField("_" + plType.ToString() + "Idxs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (plListIdxsInfo != null)
			{
				plListIdxsInfo.SetValue(this, null);
			}
			else
			{
				Debug.LogError("Serialization Error: No 'List<int> _" + plType.ToString() + "Idxs' defined in KoreographyTrack class!");
			}
			plIdxMaps.Add(plType, plListIdxsInfo);
		}

		// Loop over all of the events, storing their Payload contents if they exist
		//  in the provided lists.  These lists will have been enumerated in the logic
		//  above and stored in Dictionaries for quick lookup.
		for (int i = 0; i < mEventList.Count; ++i)
		{
			KoreographyEvent curEvt = mEventList[i];
			if (curEvt.Payload != null)
			{
				// Grab actual type.
				System.Type plType = curEvt.Payload.GetType();
				string plTypeStr = plType.ToString();
				
				// Grab Serialization List references.
				System.Reflection.FieldInfo plListInfo = plMaps[plType];
				System.Reflection.FieldInfo plListIdxsInfo = plIdxMaps[plType];

				// If these are null then the lists don't exist and we've already issued an Error.
				if (plListInfo == null || plListIdxsInfo == null)
				{
					continue;
				}

				System.Object plList = plListInfo.GetValue(this);
				List<int> plListIdxs = plListIdxsInfo.GetValue(this) as List<int>;

				// Create the interface for dealing with list manipulation (special because lists
				//  are generic (can't do List<plType>...).
				// See: http://msdn.microsoft.com/en-us/library/b8ytshk6(v=vs.110).aspx for creating generics.
				System.Type[] typeArgs = {plType};
				System.Type constructedListType = typeof(List<>).MakeGenericType(typeArgs);
				
				// When marked as SerializeField, Unity *appears* to auto create the lists.
				//  The inspector calls "OnBeforeSerialize" frequently and thus we need to
				//  ensure that we aren't simply adding repeatedly.  We work around this
				//  by clearing (newing) the lists each time we come across them here.

				bool bFirstAdd = !_SerializedPayloadTypes.Contains(plTypeStr);

				// Grabbing the Payload Storage list.
				if (plList == null || bFirstAdd)
				{
					plList = System.Activator.CreateInstance(constructedListType);

					try
					{
						plListInfo.SetValue(this, plList);
					}
					catch(System.Exception e)	// Most likely a System.ArgumentException.
					{
						// This could get spammy as the tested type will always be "bFirstAdd".  Should
						//  be okay because it will likely only happen when testing integration of new
						//  Payload type by devs.  This should become a non-issue by production phase.
						Debug.LogError("Payload storage list '" + plListInfo.Name + "' probably has an incorrect type declaration." +
						               "\n\tExpected: 'List<" + plTypeStr + ">'\n\tFound: '" + plListInfo.FieldType + "'" +
						               "\nCaught the following exception:\n" + e.ToString());
						continue;				// Move along to next event.
					}
				}

				// Setting the Payload Indexing list.
				if (plListIdxs == null || bFirstAdd)
				{
					plListIdxs = new List<int>();

					try
					{
						plListIdxsInfo.SetValue(this, plListIdxs);
					}
					catch(System.Exception e)	// Most likely a System.ArgumentException.
					{
						// This could get spammy as the tested type will always be "bFirstAdd".  Should
						//  be okay because it will likely only happen when testing integration of new
						//  Payload type by devs.  This should become a non-issue by production phase.
						Debug.LogError("Payload storage indexing list '" + plListIdxsInfo.Name + "' probably has an incorrect type declaration." +
						               "\n\tExpected: 'List<int>'\n\tFound: '" + plListIdxsInfo.FieldType + "'" +
						               "\nCaught the following exception:\n" + e.ToString());
						continue;				// Move along to next event.
					}
				}

				// Everything's good for storage.  Save the type if it isn't already there.
				if (bFirstAdd)
				{
					_SerializedPayloadTypes.Add(plTypeStr);
				}
				
				// Actually store the payload into a list that will save!
				System.Object[] addParams = {curEvt.Payload as System.Object};
				constructedListType.GetMethod("Add").Invoke(plList, addParams);
				plListIdxs.Add(i);
			}
		}
	}
	
	public void OnAfterDeserialize()
	{
		// Grab all loaded types so we can restore.
		System.Type[] allTypes = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(ass => ass.GetTypes()).ToArray();

		// Early out if nothing was serialized.
		if (_SerializedPayloadTypes == null)
		{
			return;
		}
		
		foreach (string typeStr in _SerializedPayloadTypes)
		{
			// Verify that we even have a non-null (empty) list.
			System.Reflection.FieldInfo plListInfo = GetType().GetField("_" + typeStr + "s", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
			if (plListInfo == null)
			{
				Debug.LogError("Payload storage list 'List<" + typeStr + "> _" + typeStr + "s" +
				               "' could not be retreived. Old data or did someone change the name?");
				continue;
			}

			System.Object plList = plListInfo.GetValue(this);
			
			if (plList != null)
			{
				// Grab the actual Type from the string representation.
				System.Type plType = allTypes.First(ty => ty.ToString() == typeStr);

				// Add an else statement to warn about losing data from deleted/old type?
				if (plType != null)
				{
					// Grab the second field type, assuming it is good.
					System.Reflection.FieldInfo plListIdxsInfo = GetType().GetField("_" + typeStr + "Idxs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
					if (plListIdxsInfo == null)
					{
						Debug.LogError("Payload storage indexing list 'List<int> _" + typeStr + "Idxs" +
						               "' could not be retreived. Old data or did someone change the name?");
						continue;
					}
					
					// Grab the constructed generic list type.
					System.Type[] typeArgs = {plType};
					System.Type constructedListType = typeof(List<>).MakeGenericType(typeArgs);
					
					// Get the two lists with Payload info stored in them.
					List<int> plListIdxs = plListIdxsInfo.GetValue(this) as List<int>;

					if (plListIdxs == null)
					{
						Debug.LogError("Payload storage indexing list '" + plListIdxsInfo.Name + "' probably has an incorrect type declaration." +
						               "\n\tExpected: 'List<int>'\n\tFound: '" + plListIdxsInfo.FieldType + "'");
						continue;
					}
					
					// Add the payloads back to the events.
					for (int i = 0; i < plListIdxs.Count; ++i)
					{
						System.Object[] eltAtParams = {i as System.Object};
						try
						{
							mEventList[plListIdxs[i]].Payload = constructedListType.GetProperty("Item").GetGetMethod().Invoke(plList, eltAtParams) as KoreographyPayload;
						}
						catch (System.Exception e)	// Most likely a System.Reflection.TargetException.
						{
							Debug.LogError("Payload storage list '" + plListInfo.Name + "' probably has an incorrect type declaration." +
							               "\n\tExpected: 'List<" + typeStr + ">'\n\tFound: '" + plListInfo.FieldType + "'" +
							               "\nCaught the following exception:\n" + e.ToString());
							break;					// Move along to next Type.
						}
					}
					
					// All done with these lists.  Clear them!
					plListInfo.SetValue(this, null);
					plListIdxsInfo.SetValue(this, null);
				}
			}
		}

		_SerializedPayloadTypes = null;
		// We SHOULDN'T need to reset the payload lists here because we cleared
		//  the used lists at the end of the inner foreach loop above.
	}
	
	#endregion
}