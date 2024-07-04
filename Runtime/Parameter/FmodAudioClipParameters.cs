// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using Depra.Sound.Parameter;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Depra.Sound.FMOD
{
	internal sealed class FmodAudioClipParameters : IAudioClipParameters
	{
		private readonly FmodAudioSource _source;

		public FmodAudioClipParameters(FmodAudioSource source) => _source = source;

		private IAudioClipParameter GetInternal(EventInstance instance, Type type) => type switch
		{
			_ when type == typeof(VolumeParameter) && instance.getVolume(out var volume) == RESULT.OK =>
				new VolumeParameter(volume),
			_ when type == typeof(PitchParameter) && instance.getPitch(out var pitch) == RESULT.OK =>
				new PitchParameter(pitch),
			_ when type == typeof(PositionParameter) && instance.get3DAttributes(out var attr) == RESULT.OK =>
				new PositionParameter(new Vector3(attr.position.x, attr.position.y, attr.position.z)),
			_ => new NullParameter()
		};

		private RESULT SetInternal(EventInstance instance, IAudioClipParameter parameter) => parameter switch
		{
			EmptyParameter => RESULT.OK,
			PitchParameter pitch => instance.setPitch(pitch.Value),
			VolumeParameter volume => instance.setVolume(volume.Value),
			SingleParameter single => instance.setParameterByName(single.Name, single.Value),
			IntegerParameter integer => instance.setParameterByName(integer.Name, integer.Value),
			LabelParameter label => instance.setParameterByNameWithLabel(label.Name, label.Value),
			PositionParameter position => instance.set3DAttributes(position.Value.To3DAttributes()),
			RuntimePositionParameter => instance.set3DAttributes(_source.transform.To3DAttributes()),
			FmodSingle single => instance.setParameterByName(single.Name, single.Value, single.IgnoreSeekSpeed),
			FmodLabel label => instance.setParameterByNameWithLabel(label.Name, label.Value, label.IgnoreSeekSpeed),
			FmodInteger integer => instance.setParameterByName(integer.Name, integer.Value, integer.IgnoreSeekSpeed),
			_ => RESULT.ERR_INVALID_PARAM
		};

		IEnumerable<Type> IAudioClipParameters.SupportedTypes() => new[]
		{
			typeof(FmodLabel),
			typeof(FmodSingle),
			typeof(FmodInteger),
			typeof(EmptyParameter),
			typeof(LabelParameter),
			typeof(PitchParameter),
			typeof(VolumeParameter),
			typeof(SingleParameter),
			typeof(IntegerParameter),
			typeof(PositionParameter),
			typeof(RuntimePositionParameter)
		};

		IAudioClipParameter IAudioClipParameters.Get(Type type)
		{
			var instance = _source.LastInstance;
			return instance.isValid() ? GetInternal(instance, type) : new NullParameter();
		}

		void IAudioClipParameters.Set(IAudioClipParameter parameter)
		{
			var instance = _source.LastInstance;
			if (instance.isValid() == false)
			{
				return;
			}

			var result = SetInternal(instance, parameter);
			if (result != RESULT.OK)
			{
				Debug.LogError($"Failed to set parameter {parameter.GetType().Name}: {result}");
			}
		}
	}
}