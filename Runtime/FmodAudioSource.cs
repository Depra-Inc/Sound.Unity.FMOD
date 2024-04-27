// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using Depra.Sound.Clip;
using Depra.Sound.Source;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Depra.Sound.FMOD
{
	public sealed class FmodAudioSource : MonoBehaviour, IAudioSource
	{
		[SerializeField] private STOP_MODE _stopMode;

		private EventInstance _lastInstance;

		public bool IsPlaying => _lastInstance.isValid() &&
		                         _lastInstance.getPlaybackState(out var state) == RESULT.OK &&
		                         state == PLAYBACK_STATE.PLAYING;

		float IAudioSource.Volume
		{
			get => RuntimeManager.GetBus("bus:/").getVolume(out var volume) == RESULT.OK ? volume : 0f;
			set => RuntimeManager.GetBus("bus:/").setVolume(value);
		}

		private void OnDestroy()
		{
			if (_lastInstance.isValid())
			{
				_lastInstance.release();
			}
		}

		void IAudioSource.Play(IAudioClip clip)
		{
			var fmodClip = clip as FmodAudioClip;
			_lastInstance = RuntimeManager.CreateInstance(fmodClip);
			_lastInstance.start();
		}

		void IAudioSource.Stop()
		{
			if (IsPlaying)
			{
				_lastInstance.stop(_stopMode);
			}
		}
	}
}