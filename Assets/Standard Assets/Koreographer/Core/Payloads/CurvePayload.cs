//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class KoreographyTrack
{
	#region Serialization Handling
	
	[HideInInspector][SerializeField]
	List<CurvePayload>	_CurvePayloads;
	[HideInInspector][SerializeField]
	List<int>			_CurvePayloadIdxs;

	#endregion
}

public static class CurvePayloadEventExtensions
{
	#region KoreographyEvent Extension Methods

	public static bool HasCurvePayload(this KoreographyEvent koreoEvent)
	{
		return (koreoEvent.Payload as CurvePayload) != null;
	}

	public static AnimationCurve GetCurveValue(this KoreographyEvent koreoEvent)
	{
		AnimationCurve retVal = null;

		CurvePayload pl = koreoEvent.Payload as CurvePayload;
		if (pl != null)
		{
			retVal = pl.CurveData;
		}

		return retVal;
	}

	public static float GetValueOfCurveAtTime(this KoreographyEvent koreoEvent, int sampleTime)
	{
		float retVal = 0f;

		CurvePayload pl = koreoEvent.Payload as CurvePayload;
		if (pl != null)
		{
			retVal = pl.GetValueAtDelta(koreoEvent.GetEventDeltaAtSampleTime(sampleTime));
		}

		return retVal;
	}

	#endregion
}

[System.Serializable]
public class CurvePayload : KoreographyPayload
{
	#region Editor Fields

#if UNITY_EDITOR

	static Color CurveBGColor = new Color(100f/255f, 100f/255f, 100f/255f);
	static Color SelectedCurveBGColor = new Color(0f, 100f/255f, 0f);

#endif

	#endregion
	#region Fields

	[SerializeField]
	AnimationCurve mCurveData;

	#endregion
	#region Properties

	public AnimationCurve CurveData
	{
		get
		{
			return mCurveData;
		}
		set
		{
			mCurveData = value;
		}
	}

	#endregion
	#region Constructor

	public CurvePayload()
	{
		mCurveData = GetNewCurve();
	}

	#endregion
	#region Standard Methods

	// This is used by the Koreography Editor to create the Payload type entry
	//  in the UI dropdown.
	public static string GetFriendlyName()
	{
		return "Curve";
	}

	AnimationCurve GetNewCurve()
	{
		AnimationCurve newCurve = new AnimationCurve();
		newCurve.AddKey(0f, 0f);
		newCurve.AddKey(1f, 1f);
		return newCurve;
	}

	public float GetValueAtDelta(float delta)
	{
		return mCurveData.Evaluate(delta * mCurveData.keys[mCurveData.length - 1].time);
	}

	#endregion
	#region KoreographyPayload Interface

#if UNITY_EDITOR

	public bool DoGUI(Rect displayRect, KoreographyTrack track, bool isSelected)
	{
//		bool bDidEdit = false;
		Color originalBG = GUI.backgroundColor;
		
		// 10,000 is the MAXIMUM width that a CurveField works at.  Try larger and it will crash Unity.
		if (displayRect.width <= 10000f)
		{
			GUI.backgroundColor = isSelected ? Color.green : originalBG;

			GUI.changed = false;
			AnimationCurve newCurve = EditorGUI.CurveField(displayRect, CurveData);
			if (GUI.changed)
			{
				// Attempting to Undo a curve (at least up to Unity 4.5.x) results in
				//  nearly undefined operations including, but not limited to:
				//  - Addition/Deletion of keys to/from curve not recorded in Undo stack.
				//  - Potentially [based on key addition/deletion interactions] stomp
				//		all over the Undo stack (at least that's what it looks like).
//				if (CurveData.length == newCurve.length)
//				{
//					Undo.RecordObject(track, "Modify Curve Payload");
//					bDidEdit = true;
//				}

				// Normally this is handled externally.  However, we always return false
				//  so we should set the dirty bit here.
				EditorUtility.SetDirty(track);

				CurveData = newCurve;
				GUI.changed = false;
			}
		}
		else
		{
			GUI.backgroundColor = isSelected ? SelectedCurveBGColor : CurveBGColor;
			
			GUI.Box(displayRect, string.Empty);
		}
		
		GUI.backgroundColor = originalBG;
//		return bDidEdit;
		return false;
	}

	public KoreographyPayload GetCopy()
	{
		CurvePayload newPL = new CurvePayload();
		newPL.CurveData = new AnimationCurve(CurveData.keys);

		return newPL;
	}

#endif

	#endregion
}
