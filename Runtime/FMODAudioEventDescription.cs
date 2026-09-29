using System;
using System.Collections.Generic;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Configuration;
using Depra.Sound.Runtime;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	internal sealed class FMODAudioEventDescription
	{
		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private FMODAudioClip _clip;

		[SerializeField] private List<FMODAudioParamOverride> _parameters;
		[SerializeField] private bool _is3D;

		public EventReference Event => _clip?.Event ?? default;

		public IAudioEventDescription Compile()
		{
			var parameters = new List<AudioParam>();
			foreach (var parameter in _parameters)
			{
				if (parameter.Enabled && parameter.IsSupported)
				{
					parameters.Add(parameter.IsDiscrete
						? AudioParam.CustomRef(UnityAudioParamId.LabeledInt, UnityAudioParamId.LabeledInt,
							parameter.Name,
							integerValue: Mathf.RoundToInt(parameter.Value))
						: AudioParam.CustomRef(UnityAudioParamId.LabeledFloat, UnityAudioParamId.LabeledFloat,
							parameter.Name,
							float0: parameter.Value));
				}
			}

			var requirements = _is3D
				? new AudioEventRequirements(new List<IAudioEventRequirement> { new PositionRequirement() })
				: null;
			return new RuntimeAudioEvent(_clip, requirements, parameters.ToArray());
		}

#if UNITY_EDITOR
		public static FMODAudioEventDescription Create(EventReference reference,
			IReadOnlyList<FMODAudioParamOverride> parameters, bool is3D) => new()
		{
			_clip = new FMODAudioClip(reference),
			_parameters = new List<FMODAudioParamOverride>(parameters),
			_is3D = is3D
		};
#endif

		internal void PreserveOverrides(FMODAudioEventDescription previous)
		{
			foreach (var parameter in _parameters)
			{
				foreach (var oldParameter in previous._parameters)
				{
					if (parameter.Name != oldParameter.Name)
					{
						continue;
					}

					parameter.Enabled = oldParameter.Enabled;
					parameter.Value = Mathf.Clamp(oldParameter.Value, parameter.Minimum, parameter.Maximum);
					break;
				}
			}
		}
	}
}