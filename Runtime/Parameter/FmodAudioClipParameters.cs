// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using Depra.Sound.Parameter;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	internal sealed class FmodAudioClipParameters : IAudioClipParameters
	{
		private readonly FmodAudioSource _source;

		public FmodAudioClipParameters(FmodAudioSource source) => _source = source;

		IEnumerable<Type> IAudioClipParameters.SupportedTypes() => new[]
		{
			typeof(FmodFloat),
			typeof(FmodInteger),
			typeof(PitchParameter),
			typeof(VolumeParameter),
			typeof(PositionParameter)
		};

		IAudioClipParameter IAudioClipParameters.Get(Type type)
		{
			var instance = _source.LastInstance;
			if (instance.isValid() == false)
			{
				return new NullParameter();
			}

			return type switch
			{
				_ when type == typeof(VolumeParameter) && instance.getVolume(out var volume) == RESULT.OK =>
					new VolumeParameter(volume),
				_ when type == typeof(PitchParameter) && instance.getPitch(out var pitch) == RESULT.OK =>
					new PitchParameter(pitch),
				_ when type == typeof(PositionParameter) && instance.get3DAttributes(out var attr) == RESULT.OK =>
					new PositionParameter(new Vector3(attr.position.x, attr.position.y, attr.position.z)),
				_ => new NullParameter()
			};
		}

		void IAudioClipParameters.Set(IAudioClipParameter parameter)
		{
			if (_source.LastInstance.isValid() == false)
			{
				return;
			}

			switch (parameter)
			{
				case VolumeParameter volume:
					_source.LastInstance.setVolume(volume.Value);
					break;
				case PitchParameter pitch:
					_source.LastInstance.setPitch(pitch.Value);
					break;
				case PositionParameter position:
					_source.LastInstance.set3DAttributes(position.Value.To3DAttributes());
					break;
				case FmodFloat @float:
					_source.LastInstance.setParameterByName(@float.Name, @float.Value, @float.IgnoreSeekSpeed);
					break;
				case FmodInteger integer:
					_source.LastInstance.setParameterByName(integer.Name, integer.Value, integer.IgnoreSeekSpeed);
					break;
			}
		}
	}
}