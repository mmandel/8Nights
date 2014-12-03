using UnityEngine;
using System.Collections;

public class AudioVisor
{
	AudioSource audioCom;				// The audio system to watch.
	Koreographer koreographerCom;		// The Koreographer to report audio time updates to.

	int audioBufferLen = 0;				// Used internally to track sample-time steps.

	int sampleTime = -1;				// The current sample time, possibly estimated.
	int sourceSampleTime = -1;			// The most recently read sample time from the AudioSource.

	/// <summary>
	/// Private default constructor means we require a different constructor.
	/// </summary>
	private AudioVisor(){}

	/// <summary>
	/// Initializes a new instance of the <see cref="AudioVisor"/> class.  This will 
	/// connect the AudioSource to a Koreographer.
	/// </summary>
	/// <param name="sourceCom">The AudioSource component to watch.</param>
	/// <param name="overrideKoreographer">If specified, updates are sent to this
	/// Koreographer.  Otherwise they use the default Singleton Koreographer.</param>
	public AudioVisor(AudioSource sourceCom, Koreographer overrideKoreographer = null)
	{
		int bufferNum = 0;
		AudioSettings.GetDSPBufferSize(out audioBufferLen, out bufferNum);

		// Potentially store a specific Koreographer to report to.
		koreographerCom = overrideKoreographer;
		if (koreographerCom == null)
		{
			koreographerCom = Koreographer.Instance;
		}

		audioCom = sourceCom;

		// This class shouldn't work without a valid AudioSource.
		if (audioCom == null)
		{
			System.NullReferenceException e = new System.NullReferenceException("AudioVisor does not work with a null AudioSource.");
			throw e;
		}

		// Initialize timings.
		sourceSampleTime = audioCom.timeSamples;
	}

	/// <summary>
	/// Looks at the Audio System and determines how far the audio has moved.  It then
	/// reports those results, if necessary, to the specified  Koreographer to trigger
	/// any encountered KoreographyEvents.
	/// </summary>
	public void Update()
	{
		if (audioCom.isPlaying)
		{
			// Current time update!
			int prevSampleTime = sampleTime;					// Store last frame's value.
			int curSourceSampleTime = audioCom.timeSamples;		// Grab current reported sample time from source.
			
			// Check status of Audio System this frame vs last.
			if (sourceSampleTime == curSourceSampleTime)
			{
				// We're playing but the Audio System didn't update the time.  Interpolate based on
				//  tracked time, system time, and playback speed.
				int estSampleTime = prevSampleTime + GetDeltaTimeInSamples();
				
				// Handle looping edge case.  Check if we're at or over the number of samples.  This is doable because 
				//  the reported sample time maxes out at the 0-indexed value.
				if (estSampleTime >= audioCom.clip.samples)
				{
					if (audioCom.loop)
					{
						// Process to the end of the song.
						sampleTime = audioCom.clip.samples - 1;
						Koreographer.Instance.ProcessChoreography(audioCom.clip, prevSampleTime + 1, sampleTime);
						
						// Prep for fallthrough below.
						prevSampleTime = -1;
						sampleTime = estSampleTime - audioCom.clip.samples;
					}
					else
					{
						sampleTime = audioCom.clip.samples - 1;
					}
				}
				else
				{
					// We're within range so simply use the estimated sample time!
					sampleTime = estSampleTime;
				}
			}
			else if (curSourceSampleTime < sourceSampleTime)
			{
				// Looped?  Or position was set...
				
				// Determine if this was [LIKELY - 95%(?) confidence] an automatic system loop or a manual audio change.
				int totalSampleDist = curSourceSampleTime + (audioCom.clip.samples - sourceSampleTime);
				
				// Take advantage of the fact that the audio system only reports time in increments of the AudioSettings'
				//  buffer length!
				bool bLooped = audioCom.loop && (totalSampleDist % audioBufferLen == 0);
				
				if (bLooped)
				{
					// Check that we didn't already process the loop.
					if (sourceSampleTime <= prevSampleTime)
					{
						// Didn't process the loop yet.
						
						// Store the sampleTime.  We must do this prior to calling ProcessChoreography because callbacks may be
						//  triggered that want to know the music time.
						sampleTime = audioCom.clip.samples - 1;
						
						// Play to the end.
						Koreographer.Instance.ProcessChoreography(audioCom.clip, prevSampleTime + 1, sampleTime);
						
						// Prep for beginning to curStartTime
						prevSampleTime = -1;
					}
					// else - // We've already processed the loop.  Simply fall through, using the sampleTime update below.
				}
				else
				{
					// Assume the user changed the time directly.  Also, we don't know the time they set the AudioSource to.
					//  Therefore, simply back out with a guess by how much.
					
					// Calculate the amount of samples that should have played in the time since.
					int dtInSamples = GetDeltaTimeInSamples();
					
					// Back out the prevSampleTime.  The -1 is to offset the +1 that comes later (which ensures in most cases
					//  that we don't process a single sample twice.
					prevSampleTime = Mathf.Max(0, curSourceSampleTime - dtInSamples) - 1;
				}
				
				// Make sure we're properly set up for the fall-through handling below.  This works for both cases above.
				sampleTime = curSourceSampleTime;
			}
			else
			{
				sampleTime = curSourceSampleTime;
			}
			
			// Add one to startTime because "prevSampleTime" was already checked in the previous update!
			Koreographer.Instance.ProcessChoreography(audioCom.clip, prevSampleTime + 1, sampleTime);
			
			// Ensure we're up to date with the reported source sample time.
			sourceSampleTime = curSourceSampleTime;
		}
	}
	
	/// <summary>
	/// Convert this frame's delta time from solar time to sample time.
	/// </summary>
	/// <returns>The delta time in samples.</returns>
	int GetDeltaTimeInSamples()
	{
		return (int)((float)audioCom.clip.frequency * audioCom.pitch * Time.deltaTime);
	}

	/// <summary>
	/// This is used when the AudioSource has a clip change or a jump within the audio.
	/// The AudioVisor attempts to catch "seeks" automatically.  Using this is far
	/// better.
	/// </summary>
	public void ResyncTimings()
	{
		// TODO: Optionally enable processing an Update first?
		sourceSampleTime = audioCom.timeSamples;
		sampleTime = sourceSampleTime - 1;
	}

	public int GetCurrentTimeInSamples()
	{
		return Mathf.Max(0, sampleTime);		// Use Max() because sampleTime can be -1 (during initialization/startup).
	}
}
