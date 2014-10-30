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
	List<FloatPayload>	_FloatPayloads;
	[HideInInspector][SerializeField]
	List<int>			_FloatPayloadIdxs;
	
	#endregion
}

[System.Serializable]
public class FloatPayload : KoreographyPayload
{
	#region Fields
	
	[SerializeField]
	float mFloatVal;
	
	#endregion
	#region Properties
	
	public float FloatVal
	{
		get
		{
			return mFloatVal;
		}
		set
		{
			mFloatVal = value;
		}
	}
	
	#endregion
	#region Standard Methods

	// This is used by the Koreography Editor to create the Payload type entry
	//  in the UI dropdown.
	public static string GetFriendlyName()
	{
		return "Float";
	}

	#endregion
	#region KoreographyPayload Interface

#if UNITY_EDITOR

	public bool DoGUI(Rect displayRect, KoreographyTrack track, bool isSelected)
	{
		bool bDidEdit = false;
		Color originalBG = GUI.backgroundColor;
		GUI.backgroundColor = isSelected ? Color.green : originalBG;

		GUI.changed = false;
		float newVal = EditorGUI.FloatField(displayRect, FloatVal);
		if (GUI.changed)
		{
			Undo.RecordObject(track, "Modify Float Payload");
			FloatVal = newVal;
			GUI.changed = false;
			bDidEdit = true;
		}
		
		GUI.backgroundColor = originalBG;
		return bDidEdit;
	}

	public KoreographyPayload GetCopy()
	{
		FloatPayload newPL = new FloatPayload();
		newPL.FloatVal = FloatVal;

		return newPL;
	}

#endif
	
	#endregion
}
