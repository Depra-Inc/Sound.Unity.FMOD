using System;
using System.Collections.Generic;
using System.Linq;
using Depra.SerializeReference.Extensions;
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
			foreach (var fmodParam in _parameters)
			{
				if (fmodParam.Enabled && fmodParam.IsSupported)
				{
					parameters.Add(Compile(fmodParam));
				}
			}

			var contract = new AudioEventContract(parameters.ToArray(), null);

			return new RuntimeAudioEvent(_clip, contract);
		}

		private AudioParam Compile(FMODAudioParamOverride param) => param.Type switch
		{
			FMODAudioParamOverride.ParamType.DISCRETE => AudioParam.NamedInt(AudioParamId.Custom, param.Name, (int)param.Value),
			FMODAudioParamOverride.ParamType.CONTINUOUS => AudioParam.NamedFloat(AudioParamId.Custom, param.Name, param.Value),
			FMODAudioParamOverride.ParamType.LABELED => AudioParam.NamedString(AudioParamId.Custom, param.Name, param.Labels[(int)param.Value]),
			_ => default
		};

#if UNITY_EDITOR
		public static FMODAudioEventDescription Create(EventReference reference,
			IReadOnlyList<FMODAudioParamOverride> parameters, bool is3D) => new()
		{
			_clip = new FMODAudioClip(reference),
			_parameters = new List<FMODAudioParamOverride>(parameters),
			_is3D = is3D
		};

		internal FMODAudioEventDescription Clone() => new()
		{
			_clip = _clip == null ? null : new FMODAudioClip(_clip.Event),
			_parameters = _parameters?.Select(CloneParameter).ToList() ?? new List<FMODAudioParamOverride>(),
			_is3D = _is3D
		};

		private static FMODAudioParamOverride CloneParameter(FMODAudioParamOverride parameter) => new()
		{
			Name = parameter.Name,
			Minimum = parameter.Minimum,
			Maximum = parameter.Maximum,
			DefaultValue = parameter.DefaultValue,
			Value = parameter.Value,
			Labels = parameter.Labels == null ? Array.Empty<string>() : (string[])parameter.Labels.Clone(),
			Type = parameter.Type,
			IsSupported = parameter.IsSupported,
			Enabled = parameter.Enabled
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