// SPDX-License-Identifier: Apache-2.0
// © 2024-2026 Depra <n.melnikov@depra.org>

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Depra.Sound.Configuration;
using Depra.Sound.Exceptions;
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

		private EventInstance _cachedInstance;

		public event Action Started;
		public event Action<AudioStopReason> Stopped;

		private void OnDestroy()
		{
			if (_cachedInstance.isValid())
			{
				_cachedInstance.release();
			}
		}

		public bool IsPlaying => IsPlayingInternal();
		public FMODAudioClip Current { get; private set; }
		IAudioClip IAudioSource.Current => Current;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Stop()
		{
			Current = default;
			if (IsPlaying)
			{
				OnStop(AudioStopReason.STOPPED);
			}
		}

		public void Play(IAudioClip clip)
		{
			Guard.AgainstUnsupportedType(clip, SUPPORTED_CLIP);
			var fmodClip = (FMODAudioClip)clip;
			_cachedInstance = RuntimeManager.CreateInstance(fmodClip);
			if (!_cachedInstance.isValid())
			{
				return;
			}

			Current = fmodClip;
			StartClip(_cachedInstance);
		}

		public void Play(IAudioClip clip,
			ReadOnlySpan<AudioParam> staticParams,
			ReadOnlySpan<AudioParam> dynamicParams)
		{
			Guard.AgainstUnsupportedType(clip, SUPPORTED_CLIP);
			var fmodClip = (FMODAudioClip)clip;
			_cachedInstance = RuntimeManager.CreateInstance(fmodClip);
			if (!_cachedInstance.isValid())
			{
				return;
			}

			foreach (var parameter in staticParams)
			{
				SetParameter(parameter);
			}

			foreach (var parameter in dynamicParams)
			{
				SetParameter(parameter);
			}

			Current = fmodClip;
			StartClip(_cachedInstance);
		}

		public void SetParameter(in AudioParam parameter)
		{
			RESULT result;
			var parameterId = parameter.Id;
			if (parameterId == AudioParamId.Volume && parameter.Type == AudioParamType.FLOAT)
			{
				result = _cachedInstance.setVolume(parameter.FloatValue);
			}
			else if (parameterId == AudioParamId.Loop && parameter.Type == AudioParamType.BOOL)
			{
				result = _cachedInstance.setParameterByName("Loop", parameter.IntegerValue);
			}
			else if (parameterId == AudioParamId.Pan && parameter.Type == AudioParamType.FLOAT)
			{
				result = RESULT.ERR_UNSUPPORTED;
				// FMOD does not have a direct pan parameter.
			}
			else if (parameterId == AudioParamId.Pitch && parameter.Type == AudioParamType.FLOAT)
			{
				result = _cachedInstance.setPitch(parameter.FloatValue);
			}
			else if (parameterId == UnityAudioParamId.Position && parameter.Type == AudioParamType.VECTOR3)
			{
				var position = new Vector3(parameter.Float0, parameter.Float1, parameter.Float2);
				result = _cachedInstance.set3DAttributes(position.To3DAttributes());
			}
			else if (parameterId == UnityAudioParamId.Transform && parameter.Type == AudioParamType.REFERENCE &&
			         parameter.ReferenceValue is Transform transformParameter)
			{
				RuntimeManager.AttachInstanceToGameObject(_cachedInstance, transformParameter);
				result = RESULT.OK;
			}
			else if (parameterId == UnityAudioParamId.LabeledInt)
			{
				result = _cachedInstance.setParameterByName(parameter.ReferenceValue as string, parameter.IntegerValue);
			}
			else if (parameterId == UnityAudioParamId.LabeledFloat)
			{
				result = _cachedInstance.setParameterByName(parameter.ReferenceValue as string, parameter.FloatValue);
			}
			else
			{
				result = RESULT.ERR_INVALID_PARAM;
			}

//LabelParameter label => _cachedInstance.setParameterByNameWithLabel(label.Name, label.Value),
			if (result != RESULT.OK)
			{
				VerboseError($"Failed to set parameter '{parameterId}' with result: '{result}'");
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
			_cachedInstance.stop(_stopMode);
			_cachedInstance.release();
			_cachedInstance = default;

			Stopped?.Invoke(reason);
		}

		private bool IsPlayingInternal()
		{
			if (!_cachedInstance.isValid() || _cachedInstance.getPlaybackState(out var state) != RESULT.OK)
			{
				VerboseInfo($"'{_cachedInstance}' is not valid!");
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