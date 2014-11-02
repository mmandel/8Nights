//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Koreographer/Music Players/Simple Music Player")]
public class SimpleMusicPlayer : MonoBehaviour, KoreographerInterface
{
   public bool Loop = false;

   //# of seconds ahead of real time to send events out
   public float EventPreRoll = 0.0f;

	[SerializeField]
	Koreography koreography = null;

	int sampleTime = -1;

	int audioBufferLen = 0;

	void Awake()
	{
		Koreographer.Instance.musicPlaybackController = this;
	}

	void Start()
	{
		// Store the buffer length.  This will help us determine looping situations.
		int bufferNum = 0;
		AudioSettings.GetDSPBufferSize(out audioBufferLen, out bufferNum);

		if (koreography != null)
		{
			LoadSong(koreography);
		}
	}

	void Update()
	{
      if (audio.isPlaying)
      {
         // Current time update!
         int prevSampleTime = sampleTime;			// Store last frame's value.
         int curSampleTime = audio.timeSamples;		// Start updating for this frame.
         //process pre-roll
         curSampleTime += (int)((float)koreography.SourceClip.frequency * audio.pitch * EventPreRoll);
         if (curSampleTime == prevSampleTime)
         {
            // We're playing but the Audio System didn't update the time.  Interpolate based on
            //  song time, system time, and playback speed.
            curSampleTime += (int)((float)koreography.SourceClip.frequency * audio.pitch * Time.deltaTime);

            // Handle looping edge case.
            if (curSampleTime >= koreography.SourceClip.samples)
            {
               if (audio.loop)
               {
                  Koreographer.Instance.ProcessChoreography(koreography.SourceClip, prevSampleTime + 1, koreography.SourceClip.samples);

                  // Prep for fallthrough below.
                  prevSampleTime = -1;
                  curSampleTime -= koreography.SourceClip.samples;
               }
               else
               {
                  curSampleTime = koreography.SourceClip.samples - 1;
               }
            }
         }
         else if (curSampleTime < prevSampleTime)
         {
            // Looped?  Or position was set...

            // Determine if this was [LIKELY - 95%(?) confidence] an automatic system loop or a manual audio change.
            int totalSampleDist = curSampleTime + (koreography.SourceClip.samples - prevSampleTime);

            // Take advantage of the fact that the audio system only reports time in increments of the AudioSettings'
            //  buffer length!
            if (totalSampleDist % audioBufferLen == 0)
            {
               // Play to the end.
               Koreographer.Instance.ProcessChoreography(koreography.SourceClip, prevSampleTime + 1, koreography.SourceClip.samples);

               // Prep for beginning to curStartTime
               prevSampleTime = -1;
            }
            else
            {
               // Assume the user changed the time directly.  Also, we don't know the time they set the AudioSource to.
               //  Therefore, simply back out with a guess by how much.

               // Calculate the amount of samples that should have played in the time since.
               int dtInSamples = (int)((float)koreography.SourceClip.frequency * audio.pitch * Time.deltaTime);

               // Back out the prevSampleTime.
               prevSampleTime = Mathf.Max(0, curSampleTime - dtInSamples) - 1;
            }
         }

         // Sanity check.
         if (curSampleTime < prevSampleTime)
         {
            // This appears to happen a lot at the beginning of a song.  Perhaps the Koreographer shouldn't make any assumptions until
            //  audio.timeSamples returns something non-zero...
            Debug.LogWarning("Prev Sample Time is greater than curSampleTime!  Bad estimation?  Prev: " + prevSampleTime + ", Curr: " + curSampleTime);
         }

         sampleTime = curSampleTime;				// Store sampleTime.  May be requested by callbacks triggered by the following update.

         // Add one to startTime because "prevSampleTime" was already checked in the previous update!
         Koreographer.Instance.ProcessChoreography(koreography.SourceClip, prevSampleTime + 1, curSampleTime);
      }
      else
      {
         if (Loop)
            Play();
      }
	}

	#region Playback Control

	public void LoadSong(Koreography koreo, int startSampleTime = 0, bool autoPlay = true)
	{
		Koreographer.Instance.UnloadKoreography(koreography);
		koreography = koreo;
		Koreographer.Instance.LoadKoreography(koreography);
		
		audio.clip = koreography.SourceClip;
		audio.timeSamples = startSampleTime;

		if (autoPlay)
		{
			audio.Play();
		}
	}

	public void Play()
	{
		if (!audio.isPlaying)
		{
			audio.Play();
		}
	}

	public void Stop()
	{
		audio.Stop();
	}

	public void Pause()
	{
		audio.Pause();
	}

	#endregion
	#region KoreographerInterface Methods

	public int GetSampleTimeForClip(AudioClip clip)
	{
		return Mathf.Max(0, sampleTime);		// Use Max() because sampleTime can be -1 (during initialization/startup).
	}

	public bool GetIsPlaying(AudioClip clip)
	{
		return audio.isPlaying;
	}

	public float GetPitch()
	{
		return audio.pitch;
	}

	public AudioClip GetCurrentClip()
	{
		AudioClip clip = null;
		if (koreography != null)
		{
			clip = koreography.SourceClip;
		}
		return clip;
	}

	#endregion
}
