// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.Sound.Clip;
using Depra.Sound.Parameter;
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

		public event IAudioSource.PlayDelegate Started;
		public event IAudioSource.StopDelegate Stopped;

		public bool IsPlaying => _lastInstance.isValid() &&
		                         _lastInstance.getPlaybackState(out var state) == RESULT.OK &&
		                         state == PLAYBACK_STATE.PLAYING;

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

		public IAudioClipParameter GetParameter(Type type)
		{
			if (_lastInstance.isValid() == false)
			{
				return new NullParameter();
			}

			return type switch
			{
				_ when type == typeof(VolumeParameter) && _lastInstance.getVolume(out var volume) == RESULT.OK =>
					new VolumeParameter(volume),
				_ when type == typeof(PitchParameter) && _lastInstance.getPitch(out var pitch) == RESULT.OK =>
					new PitchParameter(pitch),
				_ => new NullParameter()
			};
		}

		public void SetParameter(IAudioClipParameter parameter)
		{
			if (_lastInstance.isValid() == false)
			{
				return;
			}

			switch (parameter)
			{
				case VolumeParameter volume:
					_lastInstance.setVolume(volume.Value);
					break;
				case PitchParameter pitch:
					_lastInstance.setPitch(pitch.Value);
					break;
			}
		}
	}
}