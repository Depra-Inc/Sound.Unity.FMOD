// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using Depra.Sound.Clip;
using Depra.Sound.Parameter;
using Depra.Sound.Source;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using static Depra.Sound.Module;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Depra.Sound.FMOD
{
	[AddComponentMenu(MENU_PATH + nameof(FmodAudioSource), DEFAULT_ORDER)]
	public sealed class FmodAudioSource : MonoBehaviour, IAudioSource
	{
		[SerializeField] private STOP_MODE _stopMode;
		private EventInstance _lastInstance;

		public event IAudioSource.PlayDelegate Started;
		public event IAudioSource.StopDelegate Stopped;

		private void OnDestroy()
		{
			if (_lastInstance.isValid())
			{
				_lastInstance.release();
			}
		}

		public bool IsPlaying => _lastInstance.isValid() &&
		                         _lastInstance.getPlaybackState(out var state) == RESULT.OK &&
		                         state == PLAYBACK_STATE.PLAYING;

		internal EventInstance LastInstance => _lastInstance;
		IAudioClipParameters IAudioSource.Parameters => new FmodAudioClipParameters(this);

		void IAudioSource.Play(IAudioClip clip)
		{
			var fmodClip = clip as FmodAudioClip;
			_lastInstance = RuntimeManager.CreateInstance(fmodClip);
			var result = _lastInstance.start();
			if (result == RESULT.OK)
			{
				return;
			}

			Debug.LogError($"Failed to start audio: {result}");
			Stopped?.Invoke(AudioStopReason.ERROR);
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