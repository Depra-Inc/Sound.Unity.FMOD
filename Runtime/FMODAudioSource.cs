// SPDX-License-Identifier: Apache-2.0
// © 2024-2026 Depra <n.melnikov@depra.org>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using static Depra.Sound.FMOD.Module;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Depra.Sound.FMOD
{
	[AddComponentMenu(MENU_PATH + FILE_NAME, DEFAULT_ORDER)]
	public sealed class FMODAudioSource : SceneAudioSource, IAudioSource
	{
		[SerializeField] private STOP_MODE _stopMode;
		[SerializeField] private bool _autoRelease = true;

		private const string FILE_NAME = "FMOD Audio Source";
		private static readonly Type SUPPORTED_CLIP = typeof(FMODAudioClip);

		private FMODAudioClip _currentClip;
		private EventInstance _eventInstance;

		public event Action Started;
		public event Action<AudioStopReason> Stopped;

		private void OnDestroy()
		{
			if (_eventInstance.isValid())
			{
				_eventInstance.release();
			}
		}

		public bool IsPlaying => IsPlayingInternal();
		IAudioClip IAudioSource.Current => _currentClip;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Stop()
		{
			_currentClip = null;
			if (IsPlaying)
			{
				OnStop(AudioStopReason.STOPPED);
			}
		}

		public void Play(IAudioClip clip)
		{
			Guard.AgainstUnsupportedType(clip, SUPPORTED_CLIP);
			var fmodClip = (FMODAudioClip)clip;
			_eventInstance = RuntimeManager.CreateInstance(fmodClip);
			if (!_eventInstance.isValid())
			{
				return;
			}

			_currentClip = fmodClip;
			StartClip(_eventInstance);
		}

		public void Play(IAudioClip clip, ReadOnlySpan<AudioParam> parameters)
		{
			Guard.AgainstUnsupportedType(clip, SUPPORTED_CLIP);
			var fmodClip = (FMODAudioClip)clip;
			_eventInstance = RuntimeManager.CreateInstance(fmodClip);
			if (!_eventInstance.isValid())
			{
				return;
			}

			foreach (var param in parameters)
			{
				SetParameter(param);
			}

			_currentClip = fmodClip;
			StartClip(_eventInstance);
		}

		private void SetParameter(in AudioParam parameter)
		{
			RESULT result;
			var paramId = parameter.Id;
			if (paramId == AudioParamId.Volume && parameter.Type == AudioParamType.FLOAT)
			{
				result = _eventInstance.setVolume(parameter.FloatValue);
			}
			else if (paramId == AudioParamId.Loop && parameter.Type == AudioParamType.BOOL)
			{
				result = _eventInstance.setParameterByName("Loop", parameter.IntegerValue);
			}
			else if (paramId == AudioParamId.Pan && parameter.Type == AudioParamType.FLOAT)
			{
				// FMOD does not have a direct pan parameter.
				result = RESULT.ERR_UNSUPPORTED;
			}
			else if (paramId == AudioParamId.Pitch && parameter.Type == AudioParamType.FLOAT)
			{
				result = _eventInstance.setPitch(parameter.FloatValue);
			}
			else if (parameter.Type == AudioParamType.NAMED_FLOAT)
			{
				result = _eventInstance.setParameterByName(parameter.Name, parameter.FloatValue);
			}
			else if (parameter.Type == AudioParamType.NAMED_INT)
			{
				result = _eventInstance.setParameterByName(parameter.Name, parameter.IntegerValue);
			}
			else if (parameter.Type == AudioParamType.NAMED_STRING)
			{
				result = _eventInstance.setParameterByNameWithLabel(parameter.Name, parameter.StringValue);
			}
			else if (paramId == UnityAudioParamId.Position && parameter.Type == AudioParamType.FLOAT3)
			{
				var position = new Vector3(parameter.Float0, parameter.Float1, parameter.Float2);
				result = _eventInstance.set3DAttributes(position.To3DAttributes());
			}
			else if (paramId == UnityAudioParamId.Transform && parameter.ReferenceValue is Transform transformParameter)
			{
				RuntimeManager.AttachInstanceToGameObject(_eventInstance, transformParameter);
				result = RESULT.OK;
			}
			else
			{
				result = RESULT.ERR_INVALID_PARAM;
			}

			if (result != RESULT.OK)
			{
				VerboseError($"Failed to set parameter '{paramId}' with result: '{result}'");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void StartClip(EventInstance instance)
		{
			var result = instance.start();
			if (result == RESULT.OK)
			{
				Started?.Invoke();
				if (_autoRelease)
				{
					instance.release();
				}
			}
			else
			{
				VerboseError($"Failed to start audio: {result}");
				OnStop(AudioStopReason.ERROR);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void OnStop(AudioStopReason reason)
		{
			_eventInstance.stop(_stopMode);
			_eventInstance.release();
			_eventInstance = default;
			_currentClip = null;

			Stopped?.Invoke(reason);
		}

		private bool IsPlayingInternal()
		{
			if (!_eventInstance.isValid() || _eventInstance.getPlaybackState(out var state) != RESULT.OK)
			{
				VerboseInfo($"'{_eventInstance}' is not valid!");
				return false;
			}

			return state != PLAYBACK_STATE.STOPPED && state != PLAYBACK_STATE.STOPPING;
		}

		[Conditional(SOUND_DEBUG)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerboseInfo(string message) => Debug.LogFormat(LOG_FORMAT, message);

		[Conditional(SOUND_DEBUG)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerboseError(string message) => Debug.LogErrorFormat(LOG_FORMAT, message);
	}
}