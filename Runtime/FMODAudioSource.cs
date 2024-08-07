// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using System.Linq;
using Depra.Sound.Configuration;
using Depra.Sound.Exceptions;
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
	[AddComponentMenu(MENU_PATH + nameof(FMODAudioSource), DEFAULT_ORDER)]
	public sealed class FMODAudioSource : SceneAudioSource, IAudioSource<FMODAudioClip>
	{
		[SerializeField] private STOP_MODE _stopMode;

		private static readonly Type SUPPORTED_CLIP = typeof(FMODAudioClip);
		private static readonly Type[] SUPPORTED_CLIPS = { SUPPORTED_CLIP };

		private EventInstance _lastInstance;

		public event Action Started;
		public event Action<AudioStopReason> Stopped;

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

		public FMODAudioClip Current { get; private set; }
		IAudioClip IAudioSource.Current => Current;
		IEnumerable<Type> IAudioSource.SupportedClips => SUPPORTED_CLIPS;

		public void Stop()
		{
			if (IsPlaying)
			{
				OnStop(AudioStopReason.STOPPED);
			}
		}

		public void Play(FMODAudioClip clip, IEnumerable<IAudioSourceParameter> parameters)
		{
			_lastInstance = RuntimeManager.CreateInstance(clip);
			if (_lastInstance.isValid() == false)
			{
				return;
			}

			Current = clip;
			foreach (var parameter in parameters)
			{
				Write(parameter);
			}

			var result = _lastInstance.start();
			if (result == RESULT.OK)
			{
				Started?.Invoke();
			}
			else
			{
				Debug.LogErrorFormat(LOG_FORMAT, $"Failed to start audio: {result}");
				OnStop(AudioStopReason.ERROR);
			}
		}

		public void Write(IAudioSourceParameter parameter)
		{
			var result = parameter switch
			{
				EmptyParameter => RESULT.OK,
				PitchParameter pitch => _lastInstance.setPitch(pitch.Value),
				VolumeParameter volume => _lastInstance.setVolume(volume.Value),
				SingleParameter single => _lastInstance.setParameterByName(single.Name, single.Value),
				IntegerParameter integer => _lastInstance.setParameterByName(integer.Name, integer.Value),
				LabelParameter label => _lastInstance.setParameterByNameWithLabel(label.Name, label.Value),
				PositionParameter position => _lastInstance.set3DAttributes(position.Value.To3DAttributes()),
				RuntimePositionParameter => _lastInstance.set3DAttributes(transform.To3DAttributes()),
				AttachedTransformParameter toAttach => _lastInstance.set3DAttributes(toAttach.Value.To3DAttributes()),
				FMODSingle single => _lastInstance.setParameterByName(single.Name, single.Value, single.IgnoreSeekSpeed),
				FMODLabel label => _lastInstance.setParameterByNameWithLabel(label.Name, label.Value, label.IgnoreSeekSpeed),
				FMODInteger integer => _lastInstance.setParameterByName(integer.Name, integer.Value, integer.IgnoreSeekSpeed),
				_ => RESULT.ERR_INVALID_PARAM
			};
			if (result != RESULT.OK)
			{
				Debug.LogErrorFormat(LOG_FORMAT,
					$"Parameter '{parameter.GetType().Name}' cannot be applied to '{name}' ({nameof(FMODAudioSource)}) with result: '{result}'");
			}
		}

		public IAudioSourceParameter Read(Type type)
		{
			return _lastInstance.isValid() ? ReadInternal() : new NullParameter();

			IAudioSourceParameter ReadInternal() => type switch
			{
				_ when type == typeof(VolumeParameter) && _lastInstance.getVolume(out var volume) == RESULT.OK =>
					new VolumeParameter(volume),
				_ when type == typeof(PitchParameter) && _lastInstance.getPitch(out var pitch) == RESULT.OK =>
					new PitchParameter(pitch),
				_ when type == typeof(PositionParameter) && _lastInstance.get3DAttributes(out var attr) == RESULT.OK =>
					new PositionParameter(new Vector3(attr.position.x, attr.position.y, attr.position.z)),
				_ => new NullParameter()
			};
		}

		private void OnStop(AudioStopReason reason)
		{
			_lastInstance.stop(_stopMode);
			_lastInstance.release();
			_lastInstance = default;

			Stopped?.Invoke(reason);
		}

		private IEnumerable<Type> SupportedParameterTypes() => new[]
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
			typeof(AttachedTransformParameter)
		};

		void IAudioSource.Play(IAudioClip clip, IEnumerable<IAudioSourceParameter> parameters)
		{
			Guard.AgainstUnsupportedType(clip.GetType(), SUPPORTED_CLIP);
			Play((FMODAudioClip) clip, parameters);
		}

		IEnumerable<IAudioSourceParameter> IAudioSource.EnumerateParameters() => SupportedParameterTypes().Select(Read);
	}
}