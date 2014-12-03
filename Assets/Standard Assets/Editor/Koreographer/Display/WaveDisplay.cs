//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public enum WaveDisplayType
{
	Line,
	MinMax,
	RMS,
	Both
}

public class WaveDisplay
{
	public static int pixelDistanceToPlayheadMarker = 200;

	const int MAX_CHANNELS_TO_DRAW = 2; // WARNING! Do not edit this without reworking Draw()!

	List<ChannelDisplay> channelDisplays = new List<ChannelDisplay>();
	TrackDisplay trackDisplay = new TrackDisplay();

	Rect waveContentRect;

	Color mainBGColor = new Color(0.19215f, 0.19215f, 0.19215f);
	Color[] sectionBGColors = new Color[]{new Color(0.19215f, 0f, 0f), new Color(0f, 0.19215f, 0f), new Color(0f, 0f, 0.19215f)};

	public void SetAudioData(AudioClip clip)
	{
		// Clear out previous channel displays.
		channelDisplays.Clear();

		float[] rawData = new float[clip.samples * clip.channels];
		clip.GetData(rawData, 0);
		
		int numChannelsToDraw = Mathf.Min(clip.channels, MAX_CHANNELS_TO_DRAW);
		for (int i = 0; i < numChannelsToDraw; ++i)
		{
			float[] channelData = new float[clip.samples];
			
			for (int rawSampleIdx = i, channelSampleIdx = 0; rawSampleIdx < rawData.Length; rawSampleIdx += clip.channels, ++channelSampleIdx)
			{
				channelData[channelSampleIdx] = rawData[rawSampleIdx];
			}
			
			channelDisplays.Add(new ChannelDisplay(channelData));
		}
	}

	public bool HasAudioData()
	{
		return (channelDisplays.Count > 0);
	}

	public void SetEventTrack(KoreographyTrack newEventTrack)
	{
		trackDisplay.EventTrack = newEventTrack;
	}

	public KoreographyTrack GetEventTrack()
	{
		return trackDisplay.EventTrack;
	}

	public void Draw(Rect displayRect, WaveDisplayState displayState, Koreography koreo, bool bShowPlayhead, List<KoreographyEvent> selectedEvents)
	{
		// Draw background.
		Color originalBG = GUI.backgroundColor;
		GUI.backgroundColor = mainBGColor;

		GUI.Box(displayRect, "");
		
		GUI.backgroundColor = originalBG;

		GUI.BeginGroup(displayRect);

		// Calculate drawing metrics for channels.
		float left = GUI.skin.box.padding.left + 1f;
		float top = GUI.skin.box.padding.top;
		float width = GetChannelPixelWidthForWindow((int)displayRect.width);
		float height = (displayRect.height - GUI.skin.box.padding.vertical) / MAX_CHANNELS_TO_DRAW;
		
		Rect contentRect = new Rect(left + displayState.drawStartOffsetInPixels, top, width - displayState.drawStartOffsetInPixels, displayRect.height - GUI.skin.box.padding.vertical);
		// Adjust for start offset!
		Rect channelRect = new Rect(left + displayState.drawStartOffsetInPixels, top, width - displayState.drawStartOffsetInPixels, height);

		// Draw the beat markers before the actual audio content.
		//  NOTE: This contains GUI code.  EveytType.Repaint optimizations handled internally.
		DrawBeatLines(contentRect, displayState, koreo);

		// Only process this drawing if we're repainting (DOES NOT USE GUI SYSTEM).
		if (Event.current.type.Equals(EventType.Repaint))
		{
			// Draw Channels (waveforms)
			for (int i = 0; i < channelDisplays.Count; ++i)
			{
				channelRect.y += i * height;

				// Draw ZERO Line
				Handles.color = new Color(0f, 0f, 0f, KoreographerColors.HandleFullAlpha);
				Handles.DrawLine(new Vector2(channelRect.x, channelRect.center.y), new Vector2(channelRect.x + channelRect.width, channelRect.center.y));

				// Draw Channel Content
				channelDisplays[i].Draw(channelRect, displayState);
			}
		}

		// Draw Tracks (events)
		if (trackDisplay.EventTrack != null)
		{
			Rect trackRect = new Rect(channelRect.x,
		    	                      contentRect.center.y - 12f,
		        	                  channelRect.width,
		            	              24f);

			trackDisplay.Draw(trackRect, displayState, selectedEvents);
		}

		// Only process this drawing if we're repainting (DOES NOT USE GUI SYSTEM).
		if (Event.current.type.Equals(EventType.Repaint) && bShowPlayhead)
		{
			// Draw overlays
			if (displayState.playheadSamplePosition >= displayState.firstSamplePackToDraw &&
			    displayState.playheadSamplePosition <= displayState.firstSamplePackToDraw + (width * displayState.samplesPerPixel))
			{
				// Make the playhead position flexible to allow for scrolling (while maintaining playhead position).
				int position = displayState.drawStartOffsetInPixels + ((displayState.playheadSamplePosition - displayState.firstSamplePackToDraw) / displayState.samplesPerPixel);
				DrawPlayheadLine((int)left + position, (int)top, (int)(top + (2f * height)));
			}
		}

		if (Event.current.type == EventType.Repaint)
		{
			// Store rect.  This must be done during Repaint as the values are not properly handled on Layout.
			waveContentRect = displayRect;
		}

		GUI.EndGroup();
	}

	public int GetMaximumSamplesPerPixel(int totalSampleTime, int windowWidth)
	{
		return Mathf.CeilToInt((float)totalSampleTime / (float)GetChannelPixelWidthForWindow(windowWidth));
	}

	void DrawPlayheadLine(int x, int startY, int endY)
	{
		float grayValue = 180f / 255f;
		Handles.color = new Color(grayValue, grayValue, grayValue, KoreographerColors.HandleFullAlpha);
		Handles.DrawLine(new Vector2(x, startY), new Vector2(x, endY));
	}
	
	void DrawBeatLines(Rect contentRect, WaveDisplayState displayState, Koreography koreo)
	{
		int startSample = displayState.firstSamplePackToDraw;
		int endSample = startSample + displayState.samplesPerPixel * (int)contentRect.width;

		int startSectionIdx = koreo.GetTempoSectionIndexForSample(startSample);
		int endSectionIdx = koreo.GetTempoSectionIndexForSample(endSample);

		TempoSectionDef drawSection = koreo.GetTempoSectionAtIndex(startSectionIdx);
		if (startSectionIdx < endSectionIdx)
		{
			// Multiple sections to draw!
			for (int i = startSectionIdx + 1; i <= endSectionIdx; ++i)
			{
				TempoSectionDef nextSection = koreo.GetTempoSectionAtIndex(i);

				DrawBeatLinesForSection(contentRect, displayState, drawSection, startSample, nextSection.StartSample, sectionBGColors[(i - 1) % sectionBGColors.Length]);

				// Set up for the next section!
				startSample = nextSection.StartSample;
				drawSection = nextSection;
			}
		}

		// Draw the lines for the final (or only) section.
		DrawBeatLinesForSection(contentRect, displayState, drawSection, startSample, endSample, sectionBGColors[endSectionIdx % sectionBGColors.Length]);
	}

	void DrawBeatLinesForSection(Rect contentRect, WaveDisplayState displayState, TempoSectionDef tempoSection, int startSample, int endSample, Color sectionColor)
	{
		// Only draw the lines if our current zoom level is reasonable.  
		if (tempoSection.SamplesPerBeat >= displayState.samplesPerPixel * 2)		// Check that we will not just be drawing a line (or multiple lines!) for each pixel.  Require at least one gap.
		{
			// Draw our background box.
			{
				Color bgColor = GUI.backgroundColor;
				GUI.backgroundColor = sectionColor;
				Rect boxRect = new Rect(contentRect);
				boxRect.xMin += (float)(startSample - displayState.firstSamplePackToDraw) / displayState.samplesPerPixel;
				boxRect.xMax -= contentRect.width - (float)(endSample - displayState.firstSamplePackToDraw) / displayState.samplesPerPixel;
				GUI.Box(boxRect, "");
				GUI.backgroundColor = bgColor;
			}

			if (Event.current.type.Equals(EventType.Repaint))
			{
				// Set us up to draw the first beat.  Initially, assume we start somewhere within the view.
				float lineLoc = (float)tempoSection.StartSample - displayState.firstSamplePackToDraw;
				int beatNum = 0;

				// Get us onto the current beat boundary if our content begins beyond that first beat.
				if (startSample > tempoSection.StartSample)
				{
					lineLoc %= tempoSection.SamplesPerBeat;
					beatNum = ((int)((float)(startSample - tempoSection.StartSample) / tempoSection.SamplesPerBeat));
				}

				float grayValue = 170f / 255f;
				Color firstBeatColor = new Color(grayValue, grayValue, grayValue, KoreographerColors.HandleFullAlpha);
				
				grayValue = 96f / 255f;
				Color normalBeatColor = new Color(grayValue, grayValue, grayValue, KoreographerColors.HandleFullAlpha);

				// Draw all the beat lines!
				for (; lineLoc < (float)endSample - displayState.firstSamplePackToDraw; lineLoc += tempoSection.SamplesPerBeat)
				{
					int x = (int)(contentRect.x + (lineLoc / displayState.samplesPerPixel));
					Handles.color = (beatNum % tempoSection.BeatsPerMeasure == 0) ? firstBeatColor : normalBeatColor;
					Handles.DrawLine(new Vector2(x, contentRect.yMin), new Vector2(x, contentRect.yMax));

					// Increment the beat count!
					beatNum++;
				}
			}
		}
	}
	
	public bool ContainsPoint(Vector2 loc)
	{
		return waveContentRect.Contains(loc);
	}

	public bool IsClickableAtLoc(Vector2 loc)
	{
		return waveContentRect.Contains(loc);
	}

	public int GetChannelPixelWidthForWindow(int containerWidth)
	{
		return containerWidth - GUI.skin.box.padding.horizontal;
	}

	public int GetPixelOffsetInChannelAtLoc(Vector2 loc)
	{
		return (int)GetOffsetLocFromRaw(loc).x;
	}

	Vector2 GetOffsetLocFromRaw(Vector2 rawLoc)
	{
		// Need to adjust the location for the internal stuff from here.  It's all stored in an offset GUI.Group!
		return new Vector2(rawLoc.x - (waveContentRect.xMin),
		                   rawLoc.y - (waveContentRect.yMin));
	}

	public bool IsTrackAtPoint(Vector2 loc)
	{
		return trackDisplay.ContainsPoint(GetOffsetLocFromRaw(loc));
	}

	public KoreographyEvent GetEventAtLoc(Vector2 loc)
	{
		KoreographyEvent retEvent = null;
		if (waveContentRect.Contains(loc))
		{
			retEvent = trackDisplay.GetEventAtLoc(GetOffsetLocFromRaw(loc));
		}
		return retEvent;
	}

	public EventEditMode GetEventEditModeAtLoc(Vector2 loc)
	{
		EventEditMode retMode = EventEditMode.None;
		if (waveContentRect.Contains(loc))
	    {
			retMode = trackDisplay.GetEventEditModeAtLoc(GetOffsetLocFromRaw(loc));
		}
		return retMode;
	}

	public List<KoreographyEvent> GetEventsTouchedByArea(Rect testArea)
	{
		testArea.center = GetOffsetLocFromRaw(testArea.center);

		return trackDisplay.GetEventsTouchedByArea(testArea);
	}

	float GetDrawStart(WaveDisplayState displayState)
	{
		// TODO: Embed this info into the skin!  This was reconstructed from Draw().
		return GUI.skin.box.padding.left + GUI.skin.box.margin.left + displayState.drawStartOffsetInPixels;
	}

	public int GetSamplePositionOfPoint(Vector2 loc, WaveDisplayState displayState)
	{
		float drawStart = GetDrawStart(displayState);
		float distFromContentStart = loc.x - waveContentRect.x;

		int samplePos = displayState.firstSamplePackToDraw + displayState.samplesPerPixel * (int)(distFromContentStart - drawStart);

		// Disallow negative numbers!
		return Mathf.Max(samplePos, 0);
	}

	public float GetHorizontalLocOfSample(int samplePos, WaveDisplayState displayState)
	{
		float pixelsIn = (float)(samplePos - displayState.firstSamplePackToDraw) / (float)displayState.samplesPerPixel;
		return pixelsIn + GetDrawStart(displayState);
	}
}
