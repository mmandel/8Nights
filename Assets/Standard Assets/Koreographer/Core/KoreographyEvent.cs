//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Linq;

public interface KoreographyPayload
{
#if UNITY_EDITOR
	// For drawing the GUI in the Editor Window (possibly scene overlay?).  Undo support available.
	//  Returns TRUE if the Payload was edited in the GUI, false otherwise.
	bool DoGUI(Rect displayRect, KoreographyTrack track, bool isSelected);

	// Returns a copy of the current object, including the pertinent parts of the payload.
	KoreographyPayload GetCopy();
#endif
}

/**
 * The base event definition.  Each event instance can carry a single
 * Payload.  Events can span a range of samples or can be tied to a
 * single one.
 * 
 * Sample values (Start/End) are in "Sample Time" range, *NOT* absolute
 * sample position.  Be sure that querries/comparisons occur in TIME and not
 * DATA space.
 */
[System.Serializable]
public class KoreographyEvent
{
	#region Fields

	[SerializeField]
	int mStartSample = 0;
	
	[SerializeField]
	int mEndSample = 0;

	// The data is serialized by the KoreographyTrack in the
	//  ISerializationCallbackReceiver method implementations.
	KoreographyPayload mPayload = null;

	#endregion
	#region Properties

	public int StartSample
	{
		get
		{
			return mStartSample;
		}
		set
		{
			// Start Sample should never fall below 0.
			mStartSample = Mathf.Max(0, value);

			// Move these together.
			if (mStartSample > mEndSample)
			{
				mEndSample = mStartSample;
			}
		}
	}
	
	public int EndSample
	{
		get
		{
			return mEndSample;
		}
		set
		{
			mEndSample = Mathf.Max(0, value);

			if (mEndSample < mStartSample)
			{
				mStartSample = mEndSample;
			}

		}
	}

	public KoreographyPayload Payload
	{
		get
		{
			return mPayload;
		}
		set
		{
			mPayload = value;
		}
	}

	#endregion
	#region Static Methods
	
	public static int CompareByStartSample(KoreographyEvent first, KoreographyEvent second)
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

	public static System.Type[] GetPayloadTypes()
	{
		// Get references to all Payload types.
		System.Type iface = typeof(KoreographyPayload);

		// Adapted from http://stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface/12602220
		System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();	// Get all Assemblies.
		return assemblies.SelectMany(ass => ass.GetTypes())											// Get all Types.
						 .Where(ty => ty.IsClass &&													// Filter out Classes.
				       			ty.GetInterfaces().Contains(iface))									// Find only those that implement KoreographyPayload.
						 .ToArray();

		// TODO: Check for any that Type.IsSubclassOf(MonoBehaviour) and issue a warning.
		//  They [probably?] won't work as Payload types (no way to instantiate them without)
		//  cloning.
	}
	
	#endregion
	#region Methods

	//  events have a range/span of 0.
	public bool IsOneOff()
	{
		return StartSample == EndSample;
	}

	// Returns a value in the range of [0.0, 1.0].  If the passed in sampleTime is not within
	//  this event's range, it returns 0/1 depending on if it comes before/after.
	public float GetEventDeltaAtSampleTime(int sampleTime)
	{
		float retVal = -1f;

		// TODO: Add an OutOfRange value?  Error?
		if (sampleTime < StartSample)
		{
			retVal = 0f;
		}
		else if (sampleTime > EndSample ||	// Check that we're beyond the end.
		         IsOneOff())			// Logic order is important here(?), enabling this check!
		{
			retVal = 1f;
		}
		else
		{
			retVal = (float)(sampleTime - StartSample) / (float)(EndSample - StartSample);
		}

		return retVal;
	}

	#endregion
	#region Editor Methods

#if UNITY_EDITOR

	public KoreographyEvent GetCopy()
	{
		KoreographyEvent newEvt = new KoreographyEvent();
		newEvt.StartSample = StartSample;
		newEvt.EndSample = EndSample;
		newEvt.Payload = (Payload != null) ? Payload.GetCopy() : null;
		return newEvt;
	}

	public void MoveTo(int newSampleLoc)
	{
		newSampleLoc = Mathf.Max(0, newSampleLoc);
		int span = EndSample - StartSample;

		StartSample = newSampleLoc;
		EndSample = StartSample + span;
	}

#endif

	#endregion
}