//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;

public class ChannelDisplay
{
	float channelAmplitudePercent = 0.9f;

	float[] sampleData = new float[0];

	class WaveformCacheEntry
	{
		public bool rmsValid = false;
		public bool minMaxValid = false;

		public Vector2 rmsValues;
		public Vector2 minMaxValues;
	}

	int cachedSamplesPerPixel = 0;
	WaveformCacheEntry[] cachedData = new WaveformCacheEntry[0];

	public ChannelDisplay(float[] inSamples)
	{
		sampleData = inSamples;

		// Set up the cache.  Reserve the maximum required size [ceil(sampleCount/2)].
		cachedData = new WaveformCacheEntry[Mathf.CeilToInt((float)sampleData.Length / 2f)];
	}

	public void Draw(Rect displayRect, WaveDisplayState displayState)
	{
		if (displayState.displayType != WaveDisplayType.Line &&
		    displayState.samplesPerPixel != cachedSamplesPerPixel)
		{
			// New list instead?  Clear takes O(n) time, whereas creating a new list may be faster.
			//  Would probably thrash memory more, though.
			System.Array.Clear(cachedData, 0, cachedData.Length);
			cachedSamplesPerPixel = displayState.samplesPerPixel;
		}

		switch (displayState.displayType)
		{
		case WaveDisplayType.Line:
			DrawWaves(displayRect, displayState);
			break;
		case WaveDisplayType.MinMax:
			DrawMinMax(displayRect, displayState);
			break;
		case WaveDisplayType.RMS:
			DrawRMS(displayRect, displayState);
			break;
		case WaveDisplayType.Both:
			DrawMinMax(displayRect, displayState);
			DrawRMS(displayRect, displayState);
			break;
		}
	}

	void DrawWaves(Rect waveArea, WaveDisplayState displayState)
	{
		UnityEditor.Handles.color = new Color(1f, 149f / 255f, 0f, KoreographerColors.HandleFullAlpha);

		int startSample = displayState.firstSamplePackToDraw;
		int amplitude = (int)(channelAmplitudePercent * (waveArea.height / 2f));
		
		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		float lastY = waveArea.center.y + (sampleData[startSample] * amplitude);

		for (int i = 1; i < waveArea.width && i + startSample < sampleData.Length; ++i)
		{
			endPoint.x = waveArea.x + i;
			startPoint.x = endPoint.x - 1;	// Back us up by one!
				
			// Get y's for left channel.
			startPoint.y = lastY;
			endPoint.y = waveArea.center.y + (sampleData[startSample + i] * amplitude);
			UnityEditor.Handles.DrawLine(startPoint, endPoint);
			
			// Store previous y for next time!
			lastY = endPoint.y;
		}
	}

	void DrawMinMax(Rect waveArea, WaveDisplayState displayState)
	{
		UnityEditor.Handles.color = new Color(1f, 104f / 255f, 0f, KoreographerColors.HandleFullAlpha);

		int amplitude = (int)(channelAmplitudePercent * (waveArea.height / 2f));
		
		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		float minSample, maxSample, curSample;
		int sampleIdxForPixel;

		int cacheIdxOffset = displayState.firstSamplePackToDraw / displayState.samplesPerPixel;
		
		for (int i = 0; i < waveArea.width && displayState.firstSamplePackToDraw + (i * displayState.samplesPerPixel) < sampleData.Length; ++i)
		{
			WaveformCacheEntry entry;

			if (cachedData[cacheIdxOffset + i] == null)
			{
				entry = new WaveformCacheEntry();
				cachedData[cacheIdxOffset + i] = entry;
			}
			else
			{
				entry = cachedData[cacheIdxOffset + i];
			}

			if (!entry.minMaxValid)
			{
				minSample = 1f;
				maxSample = -1f;

				sampleIdxForPixel = i * displayState.samplesPerPixel;
				
				for (int j = 0; j < displayState.samplesPerPixel && displayState.firstSamplePackToDraw + sampleIdxForPixel + j < sampleData.Length; j++)
				{
					curSample = sampleData[displayState.firstSamplePackToDraw + sampleIdxForPixel + j];
					minSample = Mathf.Min(minSample, curSample);
					maxSample = Mathf.Max(maxSample, curSample);
				}
				
				// Subtract because positive is down!
				entry.minMaxValues.x = waveArea.center.y - (maxSample * amplitude);
				entry.minMaxValues.y = waveArea.center.y - (minSample * amplitude);

				// Update the cache entry!
				entry.minMaxValid = true;
			}

			// Draw a vertical line.
			startPoint.x = waveArea.x + i;
			endPoint.x = startPoint.x;

			startPoint.y = entry.minMaxValues.x;
			endPoint.y = entry.minMaxValues.y;

			UnityEditor.Handles.DrawLine(startPoint, endPoint);
		}
	}

	void DrawRMS(Rect waveArea, WaveDisplayState displayState)
	{
		UnityEditor.Handles.color = new Color(1f, 0.58431f, 0f, KoreographerColors.HandleFullAlpha);

		int amplitude = (int)(channelAmplitudePercent * (waveArea.height / 2f));

		Vector2 startPoint = Vector2.zero;
		Vector2 endPoint = Vector2.zero;

		float curSample, peak;
		int sampleIdxForPixel;
		
		int cacheIdxOffset = displayState.firstSamplePackToDraw / displayState.samplesPerPixel;
		
		// Calculate the waveform via RMS!
		for (int i = 0; i < waveArea.width && displayState.firstSamplePackToDraw + (i * displayState.samplesPerPixel) < sampleData.Length; ++i)
		{
			WaveformCacheEntry entry;
			
			if (cachedData[cacheIdxOffset + i] == null)
			{
				entry = new WaveformCacheEntry();
				cachedData[cacheIdxOffset + i] = entry;
			}
			else
			{
				entry = cachedData[cacheIdxOffset + i];
			}
			
			if (!entry.rmsValid)
			{
				peak = 0;

				sampleIdxForPixel = i * displayState.samplesPerPixel;
				
				for (int j = 0; j < displayState.samplesPerPixel && displayState.firstSamplePackToDraw + sampleIdxForPixel + j < sampleData.Length; j++)
				{
					curSample = sampleData[displayState.firstSamplePackToDraw + sampleIdxForPixel + j];
					
					peak += (curSample * curSample);
				}
				
				peak = Mathf.Sqrt(peak / (float)displayState.samplesPerPixel);
				
				entry.rmsValues.x = waveArea.center.y - (peak * amplitude);
				entry.rmsValues.y = waveArea.center.y + (peak * amplitude);

				// Update the cache entry!
				entry.rmsValid = true;
			}

			// Draw vertical lines.
			startPoint.x = waveArea.x + i;
			endPoint.x = startPoint.x;

			startPoint.y = entry.rmsValues.x;
			endPoint.y = entry.rmsValues.y;

			UnityEditor.Handles.DrawLine(startPoint, endPoint);
		}
	}
}
