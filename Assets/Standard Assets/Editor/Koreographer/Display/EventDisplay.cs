//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;
using UnityEngine;

public enum EventEditMode
{
	None,
	ResizeLeft,
	ResizeRight,
	Move,
}

public class EventDisplay
{
	static int MinPixelWidth = 3;

	public static void ValidateDisplayRect(ref Rect displayRect)
	{
		// Fix up minimum width situation.
		if (displayRect.width < MinPixelWidth)
		{
			displayRect.x -= MinPixelWidth / 2;
			displayRect.width = MinPixelWidth;
		}
	}

	public static void Draw(Rect displayRect, KoreographyTrack track, KoreographyEvent drawEvent, bool isSelected = false)
	{
		if (drawEvent.IsOneOff())
		{
			DrawOneOff(displayRect, drawEvent, isSelected);
		}
		else
		{
			if (drawEvent.Payload != null)
			{
				if (drawEvent.Payload.DoGUI(displayRect, track, isSelected))
				{
					GUI.changed = false;
					EditorUtility.SetDirty(track);
				}
			}
			else
			{
				DrawNoPayload(displayRect, drawEvent, isSelected);
			}
		}
	}

	public static void DrawOneOff(Rect displayRect, KoreographyEvent drawEvent, bool isSelected = false)
	{
		Color originalBG = GUI.backgroundColor;
		GUI.backgroundColor = isSelected ? Color.green : Color.red;

		GUI.Box(displayRect, "");

		GUI.backgroundColor = originalBG;
	}

	public static void DrawNoPayload(Rect displayRect, KoreographyEvent drawEvent, bool isSelected = false)
	{
		Color originalBG = GUI.backgroundColor;
		GUI.backgroundColor = isSelected ? Color.green : Color.red;

		GUIStyle labelSkin = GUI.skin.GetStyle("Label");
		TextAnchor originalAlign = labelSkin.alignment;
		labelSkin.alignment = TextAnchor.MiddleCenter;
		
		GUI.Box(displayRect, "No Payload");

		labelSkin.alignment = originalAlign;
		
		GUI.backgroundColor = originalBG;
	}
}
