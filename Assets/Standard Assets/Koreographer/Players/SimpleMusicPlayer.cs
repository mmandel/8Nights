//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Koreographer/Music Players/Simple Music Player")]
public class SimpleMusicPlayer : MonoBehaviour, KoreographerInterface
{
	[SerializeField]
	Koreography koreography = null;

	AudioVisor visor = null;

	AudioSource audioCom = null;

	void Awake()
	{
		audioCom = GetComponent<AudioSource>();
		Koreographer.Instance.musicPlaybackController = this;

		visor = new AudioVisor(audioCom);
	}

	void Start()
	{
		if (koreography != null)
		{
         LoadSong(koreography, 0, audio.playOnAwake);
		}
	}

	void Update()
	{
		// Ensure that the visor gets the update call.
		visor.Update();
	}

	#region Playback Control

	public void LoadSong(Koreography koreo, int startSampleTime = 0, bool autoPlay = true)
	{
		Koreographer.Instance.UnloadKoreography(koreography);
		koreography = koreo;

		if (koreography != null)
		{
			Koreographer.Instance.LoadKoreography(koreography);
			
			audioCom.clip = koreography.SourceClip;

			SeekToSample(startSampleTime);

			if (autoPlay)
			{
				audioCom.Play();
			}
		}
	}

	public void Play()
	{
		if (!audioCom.isPlaying)
		{
			audioCom.Play();
		}
	}

	public void Stop()
	{
		audioCom.Stop();
	}

	public void Pause()
	{
		audioCom.Pause();
	}

	public void SeekToSample(int targetSample)
	{
		audioCom.timeSamples = targetSample;

		visor.ResyncTimings();
	}

	#endregion
	#region KoreographerInterface Methods

	public int GetSampleTimeForClip(AudioClip clip)
	{
		int sampleTime = 0;
		if (clip == audioCom.clip)
		{
			sampleTime = visor.GetCurrentTimeInSamples();
		}
		return sampleTime;
	}

	public bool GetIsPlaying(AudioClip clip)
	{
		return (clip == audioCom.clip) && audioCom.isPlaying;
	}

	public float GetPitch()
	{
		return audioCom.pitch;
	}

	public AudioClip GetCurrentClip()
	{
		return audioCom.clip;
	}

	#endregion
}
