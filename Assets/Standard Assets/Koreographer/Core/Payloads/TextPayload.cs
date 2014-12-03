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
	List<TextPayload>	_TextPayloads;
	[HideInInspector][SerializeField]
	List<int>			_TextPayloadIdxs;
	
	#endregion
}

public static class TextPayloadEventExtensions
{
	#region KoreographyEvent Extension Methods
	
	public static bool HasTextPayload(this KoreographyEvent koreoEvent)
	{
		return (koreoEvent.Payload as TextPayload) != null;
	}
	
	public static string GetTextValue(this KoreographyEvent koreoEvent)
	{
		string retVal = string.Empty;
		
		TextPayload pl = koreoEvent.Payload as TextPayload;
		if (pl != null)
		{
			retVal = pl.TextVal;
		}
		
		return retVal;
	}
	
	#endregion
}

[System.Serializable]
public class TextPayload : KoreographyPayload
{
	#region Fields
	
	[SerializeField]
	string mTextVal;
	
	#endregion
	#region Properties
	
	public string TextVal
	{
		get
		{
			return mTextVal;
		}
		set
		{
			mTextVal = value;
		}
	}
	
	#endregion
	#region Standard Methods

	// This is used by the Koreography Editor to create the Payload type entry
	//  in the UI dropdown.
	public static string GetFriendlyName()
	{
		return "Text";
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
		string newVal = EditorGUI.TextField(displayRect, TextVal);
		if (GUI.changed)
		{
			Undo.RecordObject(track, "Modify Text Payload");
			TextVal = newVal;
			GUI.changed = false;
			bDidEdit = true;
		}
		
		GUI.backgroundColor = originalBG;
		return bDidEdit;
	}

	public KoreographyPayload GetCopy()
	{
		TextPayload newPL = new TextPayload();
		newPL.TextVal = TextVal;

		return newPL;
	}

#endif
	
	#endregion
}
