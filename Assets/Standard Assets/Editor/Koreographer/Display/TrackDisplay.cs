//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TrackDisplay
{
	KoreographyTrack eventTrack = null;

	Dictionary<KoreographyEvent, Rect[]> eventDisplays = new Dictionary<KoreographyEvent, Rect[]>();

	Rect trackContentRect;

	public KoreographyTrack EventTrack
	{
		get
		{
			return eventTrack;
		}
		set
		{
			eventTrack = value;
		}
	}

	public void Draw(Rect displayRect, WaveDisplayState displayState, List<KoreographyEvent> selectedEvents)
	{
		if (eventTrack != null)
		{
			int rangeStart = displayState.firstSamplePackToDraw;
			int rangeEnd = rangeStart + ((int)displayRect.width * displayState.samplesPerPixel);

			List<KoreographyEvent> drawEvents = eventTrack.GetEventsInRange(rangeStart, rangeEnd);

			// In case we want to change things later.
			Rect eventRect = new Rect(displayRect);
			int xStart, xEnd;	// Range [0, displayRect.width].  These are offsets from displayRect.x!

			if (Event.current.type == EventType.Repaint)
			{
				eventDisplays.Clear();
			}

			foreach (KoreographyEvent e in drawEvents)
			{
				// Sample-space to pixel-space.
				xStart = (e.StartSample - rangeStart) / displayState.samplesPerPixel;

				xEnd = (e.EndSample - rangeStart) / displayState.samplesPerPixel;

				eventRect.xMin = displayRect.x + xStart;
				eventRect.xMax = displayRect.x + xEnd;

				EventDisplay.ValidateDisplayRect(ref eventRect);
				EventDisplay.Draw(eventRect, eventTrack, e, selectedEvents.Contains(e));

				if (Event.current.type == EventType.Repaint)
				{
					// Add a little bit to either side.
					eventRect.width += 3f;
					eventRect.x -= 1.5f;

					// Cursor Left:
					Rect leftRect = new Rect(eventRect);
					Rect centRect = new Rect(eventRect);
					Rect rightRect = new Rect(eventRect);

					float resizeRectWidth = 3f;

					// Resize Edges.
					if (eventRect.width <= 6f)
					{
						resizeRectWidth = eventRect.width / 3f;
					}

					leftRect.xMax = leftRect.xMin + resizeRectWidth;
					rightRect.xMin = rightRect.xMax - resizeRectWidth;

					// Etc.
					centRect.xMin = leftRect.xMax;
					centRect.xMax = rightRect.xMin;

					EditorGUIUtility.AddCursorRect(leftRect, MouseCursor.ResizeHorizontal);
					EditorGUIUtility.AddCursorRect(rightRect, MouseCursor.ResizeHorizontal);
					EditorGUIUtility.AddCursorRect(centRect, MouseCursor.MoveArrow);

					// Store the rects!
					Rect[] rectSet = new Rect[4]{eventRect, leftRect, rightRect, centRect};
					eventDisplays.Add(e, rectSet);
				}
			}

			if (Event.current.type == EventType.Repaint)
			{
				// Store our rect.
				trackContentRect = displayRect;
			}
		}
	}
	
	public bool ContainsPoint(Vector2 loc)
	{
		return trackContentRect.Contains(loc);
	}

	public KoreographyEvent GetEventAtLoc(Vector2 loc)
	{
		KoreographyEvent retEvent = null;
		if (trackContentRect.Contains(loc))
		{
			foreach (KeyValuePair<KoreographyEvent, Rect[]> kvp in eventDisplays)
			{
				if (kvp.Value[(int)EventEditMode.None].Contains(loc))
				{
					retEvent = kvp.Key;
					break;
				}
			}
		}
		return retEvent;
	}

	public List<KoreographyEvent> GetEventsTouchedByArea(Rect testArea)
	{
		List<KoreographyEvent> touchedEvents = new List<KoreographyEvent>();

		foreach (KeyValuePair<KoreographyEvent, Rect[]> kvp in eventDisplays)
		{
			Rect testRect = kvp.Value[(int)EventEditMode.None];
			// Rect overlap algorithm from:
			//  http://stackoverflow.com/a/306332
			if (testArea.xMin <= testRect.xMax && testArea.xMax >= testRect.xMin &&
			    testArea.yMin <= testRect.yMax && testArea.yMax >= testRect.yMin)
			{
				touchedEvents.Add(kvp.Key);
			}
		}

		return touchedEvents;
	}

	public EventEditMode GetEventEditModeAtLoc(Vector2 loc)
	{
		EventEditMode retMode = EventEditMode.None;

		KoreographyEvent evt = GetEventAtLoc(loc);
		if (evt != null)
		{
			Rect[] rectSet = eventDisplays[evt];

			if (rectSet[(int)EventEditMode.ResizeLeft].Contains(loc))
			{
				retMode = EventEditMode.ResizeLeft;
			}
			else if (rectSet[(int)EventEditMode.ResizeRight].Contains(loc))
			{
				retMode = EventEditMode.ResizeRight;
			}
			else if (rectSet[(int)EventEditMode.Move].Contains(loc))
			{
				retMode = EventEditMode.Move;
			}
		}

		return retMode;
	}
}
