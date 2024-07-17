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

		private static readonly Type SUPPORTED_TRACK = typeof(FMODAudioTrack);
		private static readonly Type[] SUPPORTED_TRACKS = { SUPPORTED_TRACK };

		private static IEnumerable<Type> SupportedTypes() => new[]
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
			typeof(RuntimePositionParameter)
		};

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
		IEnumerable<Type> IAudioSource.SupportedTracks => SUPPORTED_TRACKS;

		public void Play(IAudioTrack track)
		{
			var clip = track.Play(this);
			Guard.AgainstUnsupportedType(clip.GetType(), SUPPORTED_TRACK);

			Current = (FMODAudioClip) clip;
		}

		public void Stop()
		{
			if (IsPlaying == false)
			{
				return;
			}

			_lastInstance.stop(_stopMode);
			Stopped?.Invoke(AudioStopReason.STOPPED);
		}


		public void Play(FMODAudioClip clip, IEnumerable<IAudioClipParameter> parameters)
		{
			_lastInstance = RuntimeManager.CreateInstance(clip);
			if (_lastInstance.isValid() == false)
			{
				return;
			}

			foreach (var parameter in parameters)
			{
				Set(parameter);
			}

			var result = _lastInstance.start();
			if (result == RESULT.OK)
			{
				Started?.Invoke();
			}
			else
			{
				Debug.LogErrorFormat(LOG_FORMAT, $"Failed to start audio: {result}");
				Stopped?.Invoke(AudioStopReason.ERROR);
			}
		}

		public void Set(IAudioClipParameter parameter)
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
				FMODSingle single => _lastInstance.setParameterByName(single.Name, single.Value, single.IgnoreSeekSpeed),
				FMODLabel label => _lastInstance.setParameterByNameWithLabel(label.Name, label.Value,
					label.IgnoreSeekSpeed),
				FMODInteger integer => _lastInstance.setParameterByName(integer.Name, integer.Value,
					integer.IgnoreSeekSpeed),
				_ => RESULT.ERR_INVALID_PARAM
			};
			if (result != RESULT.OK)
			{
				Debug.LogErrorFormat(LOG_FORMAT,
					$"Parameter '{parameter.GetType().Name}' cannot be applied to '{name}' ({nameof(FMODAudioSource)}) with result: '{result}'");
			}
		}

		public TParameter Read<TParameter>() where TParameter : IAudioClipParameter =>
			(TParameter) Read(typeof(TParameter));

		public IAudioClipParameter Read(Type type)
		{
			return _lastInstance.isValid() ? ReadInternal() : new NullParameter();

			IAudioClipParameter ReadInternal() => type switch
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

		IEnumerable<IAudioClipParameter> IAudioSource.EnumerateParameters() => SupportedTypes().Select(Read);
	}
}