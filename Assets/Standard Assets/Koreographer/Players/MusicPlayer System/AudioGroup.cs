//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Audio Group
/// This class is a collection.  It bundles up several audio layers, or stems,
/// into one logical "piece" of audio.  The properties revealed by the class
/// are all based on the base layer; the AudioLayer at position 0 in the
/// Audio Layers list.
/// </summary>

[System.Serializable]
public class AudioGroup
{
	[SerializeField]
	List<AudioLayer> audioLayers = new List<AudioLayer>();

	// TODO: Single Group TimeLine - This to allow timed, layer segments, rather than "play until the end."
	//  Though this may not be necessary: "silence" can be added by simply skipping the "data fill" in the
	//  AddData function at the Layer level.

	public int TotalSampleTime
	{
		get
		{
			int val = 0;
			if (audioLayers != null && audioLayers.Count > 0)
			{
				val = audioLayers[0].TotalSampleTime;
			}
			return val;
		}
	}
	
	public int Channels
	{
		get
		{
			int val = 0;
			if (audioLayers != null && audioLayers.Count > 0)
			{
				val = audioLayers[0].Channels;
			}
			return val;
		}
	}

	public int Frequency
	{
		get
		{
			int val = 0;
			if (audioLayers != null && audioLayers.Count > 0)
			{
				val = audioLayers[0].Frequency;
			}
			return val;
		}
	}

	public int NumLayers
	{
		get
		{
			return (audioLayers == null) ? 0 : audioLayers.Count;
		}
	}

   public AudioLayer GetLayer(int layerIdx)
   {
      if ((layerIdx >= 0) && (layerIdx < NumLayers))
         return audioLayers[layerIdx];
      return null;
   }

	public void InitLayerData()
	{
		foreach (AudioLayer layer in audioLayers)
		{
			layer.InitData();
		}
	}

	public void ClearLayerData()
	{
		foreach (AudioLayer layer in audioLayers)
		{
			layer.ClearData();
		}
	}

	public void RegisterKoreography()
	{
		foreach (AudioLayer layer in audioLayers)
		{
			Koreographer.Instance.LoadKoreography(layer.Koreo);
		}
	}

	public void UnregisterKoreography()
	{
		foreach (AudioLayer layer in audioLayers)
		{
			Koreographer.Instance.UnloadKoreography(layer.Koreo);
		}
	}

	public bool IsKoreographyRegistered()
	{
		bool bIsLoaded = true;

		foreach (AudioLayer layer in audioLayers)
		{
			if (!Koreographer.Instance.IsKoreographyLoaded(layer.Koreo))
			{
				bIsLoaded = false;
				break;
			}
		}

		return bIsLoaded;
	}

	public bool IsReady()
	{
		bool bReady = true;
		foreach (AudioLayer layer in audioLayers)
		{
			if (!layer.IsReady())
			{
				bReady = false;
				break;
			}
		}
		return bReady;
	}

	public bool IsEmpty()
	{
		return (audioLayers.Count == 0);
	}

	public bool ContainsClip(AudioClip clip)
	{
		bool bContains = false;
		if (clip != null)
		{
			foreach (AudioLayer layer in audioLayers)
			{
				if (layer.Clip == clip)
				{
					bContains = true;
					break;
				}
			}
		}
		return bContains;
	}

	public AudioClip GetBaseClip()
	{
		AudioClip baseClip = null;

		if (audioLayers != null && audioLayers.Count > 0)
		{
			baseClip = audioLayers[0].Clip;
		}

		return baseClip;
	}

	public AudioClip GetClipAtLayer(int layerIdx)
	{
		AudioClip clip = null;

		if (audioLayers != null && layerIdx < audioLayers.Count)
		{
			clip = audioLayers[layerIdx].Clip;
		}

		return clip;
	}

	/// <summary>
	/// Called every frame by the AudioPlayer.  This will actually perform
	/// the Koreography check across all the AudioLayers.
	/// </summary>
	/// <param name="lastSampleTime">Sample time of last update.</param>
	/// <param name="curSampleTime">Sample time of this update.</param>
	public void DoKoreographyUpdate(int lastSampleTime, int curSampleTime)
	{
		foreach (AudioLayer layer in audioLayers)
		{
			Koreography koreo = layer.Koreo;
			if (koreo != null)
			{
				koreo.UpdateTrackTime(lastSampleTime, curSampleTime);
			}
		}
	}

	/// <summary>
	/// Gets the audio data from the various layers.
	/// </summary>
	public void GetAudioData(int sampleTime, float[] data, int dataOffset, int amountToRead)
	{
		// We report the length of data of the group based on the base layer.  This should be okay.
		audioLayers[0].ReadLayerAudioData(sampleTime, data, dataOffset, amountToRead);

		for (int i = 1; i < audioLayers.Count; ++i)
		{
			int layerOffset = sampleTime * audioLayers[i].Channels;	// How far in data (samples * channels) we start reading.

			// Sub-layers can potentially be shorter than the base layer.  Protect against this.
			if (audioLayers[i].TotalDataLength > layerOffset + amountToRead)
			{
				audioLayers[i].AddLayerAudioData(sampleTime, data, dataOffset, amountToRead);
			}
			else
			{
				// Calculate the amount we have left to feed into the data array.
				//  The "+1" is to compensate for 0-based indexing value vs 1-based magnitude.
				int amountLeft = audioLayers[i].TotalDataLength - (layerOffset + 1);

				// Verify if we're being asked for the final batch or for a position beyond our end.
				if (amountLeft > 0)
				{
					// Fill up to the end of the track.
					audioLayers[i].AddLayerAudioData(sampleTime, data, dataOffset, amountLeft);
				}
			}
		}
	}
}
