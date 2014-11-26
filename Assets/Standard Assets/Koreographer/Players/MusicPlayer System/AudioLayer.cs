//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Audio Layer
/// This class is a data supplier: it logically combines Koreography with
///  an AudioClip for playback purposes.
/// 
/// Note that EITHER the Koreography or the Clip is sufficient
///  to work.  This is designed to allow non-choreographed layers in the
///  audio system.  In the case that both exist, the AudioClip specified
///  by the user preferred.  WARNING: this will cause issues with the
///  specified Koreography if the clip is different.
/// </summary>
/// 
/// TODO: Simplify this.  No need to have an AudioClip alone.  Just create empty Koreography to wrap it?

[System.Serializable]
public class AudioLayer
{
	[SerializeField]
	Koreography koreo = null;

	float[] audioData = null;

	[SerializeField]
	AudioClip clip = null;

	int totalSampleTime = 0;
	int channelCount = 0;
	int frequency = 0;

	[Range(0f, 1f)]
	public float volume = 1f;

	public AudioClip Clip
	{
		get
		{
			return clip;
		}
	}

	public Koreography Koreo
	{
		get
		{
			return koreo;
		}
	}

	public int TotalSampleTime
	{
		get
		{
			return totalSampleTime;
		}
	}

	public int Channels
	{
		get
		{
			return channelCount;
		}
	}

	public int Frequency
	{
		get
		{
			return frequency;
		}
	}

	public float Volume
	{
		get
		{
			return volume;
		}
		set
		{
			volume = Mathf.Clamp01(value);
		}
	}
	
	public int TotalDataLength
	{
		get
		{
			return totalSampleTime * channelCount;
		}
	}

	public void InitData()
	{
		if (clip == null)
		{
			clip = koreo.SourceClip;
		}

		// Store all of this off.  This is because we can ONLY access
		//  properties of the AudioClip from the Main thread.
		totalSampleTime = clip.samples;
		channelCount = clip.channels;
		frequency = clip.frequency;

		// TODO: Range-based caching!  Currently we only cache the entire song.
		audioData = new float[Clip.samples * Clip.channels];
		Clip.GetData(audioData, 0);
	}

	public void ClearData()
	{
		audioData = null;
	}

	public bool IsReady()
	{
		return (audioData != null);
	}

	public void ReadLayerAudioData(int sampleTimePos, float[] data, int dataOffset, int amount)
	{
      if (audioData == null)
         return;

		int dataPos = sampleTimePos * channelCount;
		
		for (int i = 0; i < amount; ++i)
		{
			data[dataOffset + i] = audioData[dataPos + i] * volume;
		}
	}

	public void AddLayerAudioData(int sampleTimePos, float[] data, int dataOffset, int amount)
	{
      if (audioData == null)
         return;

		int dataPos = sampleTimePos * channelCount;

		for (int i = 0; i < amount; ++i)
		{
			data[dataOffset + i] += audioData[dataPos + i] * volume;
		}
	}
}
