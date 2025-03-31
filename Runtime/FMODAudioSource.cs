// SPDX-License-Identifier: Apache-2.0
// © 2024-2025 Depra <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
	[AddComponentMenu(MENU_PATH + nameof(FMODAudioSource), DEFAULT_ORDER)]
	public sealed class FMODAudioSource : SceneAudioSource, IAudioSource<FMODAudioClip>
	{
		[SerializeField] private STOP_MODE _stopMode;

		private static readonly Type SUPPORTED_CLIP = typeof(FMODAudioClip);
		private static readonly Type[] SUPPORTED_CLIPS = { SUPPORTED_CLIP };
		private static readonly Type[] SUPPORTED_PARAMETERS =
		{
			typeof(FMODLabel),
			typeof(FMODSingle),
			typeof(FMODInteger),
			typeof(EmptyParameter),
			typeof(LabelParameter),
			typeof(PitchParameter),
			typeof(VolumeParameter),
			typeof(SingleParameter),
			typeof(IntegerParameter),
			typeof(PositionParameter),
			typeof(TransformParameter)
		};

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

		public bool IsPlaying
		{
			get
			{
				if (_cachedInstance.isValid() == false || _cachedInstance.getPlaybackState(out var state) != RESULT.OK)
				{
					VerboseInfo($"'{_cachedInstance}' is not valid!");
					return false;
				}

				VerboseInfo($"'{_cachedInstance}' playback state: {state}");
				return state == PLAYBACK_STATE.PLAYING;
			}
		}

		public FMODAudioClip Current { get; private set; }
		IAudioClip IAudioSource.Current => Current;
		IEnumerable<Type> IAudioSource.SupportedClips => SUPPORTED_CLIPS;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Stop()
		{
			if (IsPlaying)
			{
				OnStop(AudioStopReason.STOPPED);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Play(FMODAudioClip clip, IEnumerable<IAudioSourceParameter> parameters)
		{
			_cachedInstance = RuntimeManager.CreateInstance(clip);
			if (!_cachedInstance.isValid())
			{
				return;
			}

			Current = clip;
			foreach (var parameter in parameters)
			{
				Write(parameter);
			}

			var result = _cachedInstance.start();
			if (result == RESULT.OK)
			{
				Started?.Invoke();
                _cachedInstance.release();
			}
			else
			{
				VerboseError($"Failed to start audio: {result}");
				OnStop(AudioStopReason.ERROR);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(IAudioSourceParameter parameter)
		{
			var result = parameter switch
			{
				EmptyParameter => RESULT.OK,
				PitchParameter pitch => _cachedInstance.setPitch(pitch.Value),
				VolumeParameter volume => _cachedInstance.setVolume(volume.Value),
				TransformParameter target => AttachToTransform(_cachedInstance, target.Value),
				SingleParameter single => _cachedInstance.setParameterByName(single.Name, single.Value),
				IntegerParameter integer => _cachedInstance.setParameterByName(integer.Name, integer.Value),
				LabelParameter label => _cachedInstance.setParameterByNameWithLabel(label.Name, label.Value),
				PositionParameter position => _cachedInstance.set3DAttributes(position.Value.To3DAttributes()),
				RuntimePositionParameter => _cachedInstance.set3DAttributes(transform.To3DAttributes()),
				FMODSingle single => _cachedInstance.setParameterByName(single.Name, single.Value, single.IgnoreSeekSpeed),
				FMODLabel label => _cachedInstance.setParameterByNameWithLabel(label.Name, label.Value, label.IgnoreSeekSpeed),
				FMODInteger integer => _cachedInstance.setParameterByName(integer.Name, integer.Value, integer.IgnoreSeekSpeed),
				_ => RESULT.ERR_INVALID_PARAM
			};
			if (result != RESULT.OK)
			{
				VerboseError($"Parameter '{parameter.GetType().Name}' cannot be applied to '{name}' ({nameof(FMODAudioSource)}) with result: '{result}'");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IAudioSourceParameter Read(Type type) => type switch
		{
			_ when type == typeof(VolumeParameter) && _cachedInstance.getVolume(out var volume) == RESULT.OK =>
				new VolumeParameter(volume),
			_ when type == typeof(PitchParameter) && _cachedInstance.getPitch(out var pitch) == RESULT.OK =>
				new PitchParameter(pitch),
			_ when type == typeof(PositionParameter) && _cachedInstance.get3DAttributes(out var attr) == RESULT.OK =>
				new PositionParameter(new Vector3(attr.position.x, attr.position.y, attr.position.z)),
			_ when type == typeof(TransformParameter) => new TransformParameter(transform),
			_ => new NullParameter()
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void OnStop(AudioStopReason reason)
		{
			_cachedInstance.stop(_stopMode);
			_cachedInstance.release();
			_cachedInstance = default;

			Stopped?.Invoke(reason);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private RESULT AttachToTransform(EventInstance instance, Transform target)
		{
			RuntimeManager.AttachInstanceToGameObject(instance, target);
			return RESULT.OK;
		}

		void IAudioSource.Play(IAudioClip clip, IList<IAudioSourceParameter> parameters)
		{
			Guard.AgainstUnsupportedType(clip.GetType(), SUPPORTED_CLIP);
			Play((FMODAudioClip)clip, parameters);
		}

		IEnumerable<IAudioSourceParameter> IAudioSource.EnumerateParameters() => SUPPORTED_PARAMETERS.Select(Read);

		[Conditional("SOUND_DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerboseInfo(string message) => Debug.LogFormat(LOG_FORMAT, message);

		[Conditional("SOUND_DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerboseError(string message) => Debug.LogErrorFormat(LOG_FORMAT, message);
	}
}