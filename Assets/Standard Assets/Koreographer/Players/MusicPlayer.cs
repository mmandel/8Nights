//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;

// TODO: Convert the AudioBus *AND* this class into ones that support a queue of files to playback from.  This would make transitions
//  far easier to handle.

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Koreographer/Music Players/Music Player")]
public class MusicPlayer : MonoBehaviour, KoreographerInterface
{
	public delegate void MusicEndedHandler(AudioGroup group);
	
	public event MusicEndedHandler MusicEnded;

	[SerializeField]
	AudioGroup playbackMusic = null;

	[SerializeField]
	int musicChannels = 2;

	[SerializeField]
	int musicFrequency = 44100;

	AudioGroup curMusic = null;
	AudioGroup transMusic = null;

	int curMusicTime = -1;
	int transMusicTime = 0;

	// This is necessary to store transition info in case we're told to do something like:
	//  MusicPlayer.PlayMusic();
	//  MusicPlayer.ScheduleMusic();
	// right in a row.  The underlying AudioBus needs time to move the "PlayMusic" contents out of its own
	// internal "next" audio reference.  So we store and keep trying until it goes through.  See: Update().
	struct TransitionInfo
	{
		public bool bValid;
		public AudioGroup group;
		public int curMusicTransLoc;
		public int startSampleOffset;
		public int lengthInSamples;
		public bool bReplaceIfExists;
	}
	TransitionInfo transInfo;

	public AudioBus bus = null;

	AudioSource audioCom;

	void Awake()
	{
		audioCom = GetComponent<AudioSource>();
		Koreographer.Instance.musicPlaybackController = this;
		bus.AudioEnded += OnAudioEnded;
	}

	void Start()
	{
//		bus.Init(audioCom, playbackMusic.Channels, playbackMusic.Frequency);		// CAN'T DO THIS!  Need to Init the AudioGroup before the Properties will work.
		bus.Init(audioCom, musicChannels, musicFrequency);

		if (playbackMusic != null && !playbackMusic.IsEmpty())
		{
			PlayMusic(playbackMusic);
		}
	}

	void Update()
	{
		//  Use the times to update the Koreographer.  Be sure to also notify it of looping/audio transitions.
		if (curMusic != null && bus.IsAudioPlaying(curMusic) && !bus.IsPaused())
		{
			int prevMusicTime = curMusicTime;
			curMusicTime = bus.GetSampleTimeOfAudio(curMusic);

			if (curMusicTime == prevMusicTime)
			{
				// We're playing but the Audio Bus didn't update the time.  Interpolate based on
				//  song time, system time, and playback speed.
				curMusicTime += (int)((float)curMusic.Frequency * GetPitch() * Time.deltaTime);

				// Don't go beyond the edge of the group.
				curMusicTime = Mathf.Min(curMusicTime, curMusic.TotalSampleTime);
			}

			// Add one to startTime because the "prevMusicTime" sample was already checked in the previous update!
			PerformChoreographyForTimeSlice(curMusic, prevMusicTime + 1, curMusicTime);
		}

		// Check to see if we need to try to schedule a transition.
		if (transInfo.bValid)
		{
			transInfo.bValid = false;
			ScheduleNextMusic(transInfo.group, transInfo.curMusicTransLoc, transInfo.startSampleOffset, transInfo.lengthInSamples, transInfo.bReplaceIfExists);

			// If scheduling failed, the transInfo should be regenerated from within ScheduleNextMusic(), including the bValid flag being set to true.
		}
	}

	#region Koreography Control

	void PerformChoreographyForTimeSlice(AudioGroup group, int startTime, int endTime)
	{
		for (int i = 0; i < curMusic.NumLayers; ++i)
		{
			AudioClip clip = curMusic.GetClipAtLayer(i);
			
			Koreographer.Instance.ProcessChoreography(clip, startTime, endTime);
		}
	}

	#endregion
	#region Playback Control

	public void PlayMusic(AudioGroup group, int startSampleOffset = 0, int lengthInSamples = 0, bool bReplaceIfExists = false)
	{
		// TODO: Warn if channels/frequency not matching!
		// TODO: Validate AudioGroup (!group.IsEmpty())!

		if (!group.IsReady())
		{
			group.InitLayerData();
		}

		if (curMusic != null)
		{
			transMusic = group;
			transMusicTime = startSampleOffset;
		}
		else
		{
			curMusic = group;
			curMusicTime = startSampleOffset - 1;	// -1 to get the initial sample.
		}

		group.RegisterKoreography();

		if (!bus.PlayAudio(group, startSampleOffset, lengthInSamples, bReplaceIfExists))
		{
			Debug.LogWarning("PlayMusic() failed with group: " + group + ", likely something already in the AudioBus?");
		}
	}

	public void ScheduleNextMusic(AudioGroup group, int curMusicTransLoc = 0, int startSampleOffset = 0, int lengthInSamples = 0, bool bReplaceIfExists = false)
	{
		// TODO: Warn if channels/frequency not matching!
		// TODO: Validate AudioGroup (!group.IsEmpty())!

		if (!group.IsReady())
		{
			group.InitLayerData();		// Delay this?
		}

		if (curMusic == null)
		{
			// Just play now.
			curMusic = group;
			curMusicTime = startSampleOffset - 1;	// -1 to get the initial sample.

			bus.PlayAudio(group, startSampleOffset, lengthInSamples, bReplaceIfExists);
		}
		else if (transMusic == null || bReplaceIfExists)
		{
			if (bus.IsNextSongScheduled())
			{
				// Next song is already in, meaning we're likely waiting for curSong to transition in.  Save until buffers clear enough
				//  to allow scheduling.
				if (!transInfo.bValid || bReplaceIfExists)
				{
					// Override or just make sure there's nothing there.
					transMusic = null;
					transMusicTime = 0;

					// Set up info for another try later!
					transInfo.bValid = true;
					transInfo.group = group;
					transInfo.curMusicTransLoc = curMusicTransLoc;
					transInfo.startSampleOffset = startSampleOffset;
					transInfo.lengthInSamples = lengthInSamples;
					transInfo.bReplaceIfExists = bReplaceIfExists;
				}
				else
				{
					Debug.LogWarning("MusicPlayer::ScheduleNextMusic() - Transition music already scheduled!");
				}
			}
			else
			{
				// Set up the transition.
				transMusic = group;
				transMusicTime = startSampleOffset;

				// Koreography registration occurs later.  That way we don't double-up or trigger unwanted samples until later.
				bus.PlayAudioScheduled(group, curMusicTransLoc, startSampleOffset, lengthInSamples, bReplaceIfExists);
			}
		}
		// else - don't do anything - we already have music scheduled and we were told NOT to replace it.
	}

	public void Pause()
	{
		bus.Pause();
	}

	public void Continue()
	{
		bus.Continue();
	}

	public void Stop()
	{
		// Trigger the callback.  This will trigger OnAudioEnded() which will
		//  cause all the Koreographer and AudioGroup cleanup.
		bus.Stop(true);
	}

	#endregion
	#region AudioBus Callbacks

	void OnAudioEnded(AudioGroup group, int sampleTime)
	{
		if (curMusic == group)
		{
			if (sampleTime != curMusicTime)
			{
				PerformChoreographyForTimeSlice(curMusic, curMusicTime + 1, sampleTime);
			}

			// Save some time if we're simply replaying the previous music.
			if (curMusic != transMusic)
			{
				group.UnregisterKoreography();	// Clean out Koreography linkages.
				group.ClearLayerData();			// Free up some space.
			}

			// Make sure we've loaded the new Koreography.
			if (transMusic != null && !transMusic.IsKoreographyRegistered())
			{
				transMusic.RegisterKoreography();
			}

			curMusic = transMusic;
			transMusic = null;

			curMusicTime = transMusicTime - 1;	// -1 to get the initial sample.
			transMusicTime = 0;

			// Trigger the callback!
			if (MusicEnded != null)
			{
				MusicEnded(group);
			}
		}
		else
		{
			Debug.LogWarning("Unexpected music has completed playback.");
		}
	}

	#endregion
	#region KoreographerInterface Methods

	public int GetSampleTimeForClip(AudioClip clip)
	{
		int sampleTime = 0;
		if (curMusic != null && curMusic.ContainsClip(clip))
		{
			sampleTime = Mathf.Max(0, curMusicTime);	// Initialized to -1 for startup.  Handle this, particularly for transitioning.
		}
		else if (transMusic != null && transMusic.ContainsClip(clip))
		{
			sampleTime = transMusicTime;
		}
		return sampleTime;
	}

	public bool GetIsPlaying(AudioClip clip)
	{
		bool bPlaying = false;
		if (curMusic != null && curMusic.ContainsClip(clip))
		{
			bPlaying = bus.IsAudioPlaying(curMusic);
		}
		else if (transMusic != null && transMusic.ContainsClip(clip))
		{
			bPlaying = bus.IsAudioPlaying(transMusic);
		}
		return bPlaying;
	}

	public float GetPitch()
	{
		float pitch = 1f;
		if (bus != null)
		{
			pitch = bus.Pitch;
		}
		return pitch;
	}

	public AudioClip GetCurrentClip()
	{
		AudioClip clip = null;
		if (curMusic != null)
		{
			clip = curMusic.GetBaseClip();
		}
		else if (transMusic != null)
		{
			clip = transMusic.GetBaseClip();
		}
		return clip;
	}

	#endregion
}
