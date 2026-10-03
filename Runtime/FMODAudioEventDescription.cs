using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Runtime;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	internal sealed class FMODAudioEventDescription : ISerializationCallbackReceiver
	{
		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private FMODAudioClip _clip;

		[SerializeField] private List<FMODAudioParamOverride> _parameters;

		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private List<IAudioParamDescription> _contractParameters;

		[SerializeField] private bool _is3D;

		public EventReference Event => _clip?.Event ?? default;

		public IAudioEventDescription Compile()
		{
			var exportedSource = _parameters ?? new List<FMODAudioParamOverride>();
			var contractSource = _contractParameters ?? new List<IAudioParamDescription>();

			var exportedParams = new List<AudioParam>();
			foreach (var fmodParam in exportedSource)
			{
				if (fmodParam is { Enabled: true, IsSupported: true })
				{
					exportedParams.Add(Compile(fmodParam));
				}
			}

			var optionalParams = new List<AudioParam>();
			foreach (var parameter in contractSource)
			{
				if (parameter != null)
				{
					optionalParams.Add(parameter.Compile());
				}
			}

			return new RuntimeAudioEvent(_clip, new AudioEventContract(
				exportedParams.ToArray(),
				optionalParams.ToArray()));
		}

		internal void PreserveOverrides(FMODAudioEventDescription previous)
		{
			if (previous == null)
			{
				return;
			}

			var parameters = _parameters ?? new List<FMODAudioParamOverride>();
			var previousParameters = previous._parameters ?? new List<FMODAudioParamOverride>();
			foreach (var parameter in parameters)
			{
				if (parameter == null)
				{
					continue;
				}

				foreach (var oldParameter in previousParameters)
				{
					if (oldParameter == null)
					{
						continue;
					}

					if (parameter.Name != oldParameter.Name)
					{
						continue;
					}

					parameter.Enabled = oldParameter.Enabled;
					parameter.Value = Mathf.Clamp(oldParameter.Value, parameter.Minimum, parameter.Maximum);
					break;
				}
			}

			if ((_contractParameters == null || _contractParameters.Count == 0) &&
			    previous._contractParameters is { Count: > 0 })
			{
				_contractParameters = CloneContractParameters(previous._contractParameters);
			}
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() { }

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			_parameters ??= new List<FMODAudioParamOverride>();
			_contractParameters ??= new List<IAudioParamDescription>();
		}

		private static List<IAudioParamDescription> CloneContractParameters(
			IEnumerable<IAudioParamDescription> parameters)
		{
			if (parameters == null)
			{
				return new List<IAudioParamDescription>();
			}

			var clones = new List<IAudioParamDescription>();
			foreach (var parameter in parameters)
			{
				clones.Add(CloneContractParameter(parameter));
			}

			return clones;
		}

		private static IAudioParamDescription CloneContractParameter(IAudioParamDescription parameter)
		{
			if (parameter == null)
			{
				return null;
			}

			var type = parameter.GetType();
			var clone = Activator.CreateInstance(type);
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var field in fields)
			{
				field.SetValue(clone, field.GetValue(parameter));
			}

			return (IAudioParamDescription)clone;
		}

		private AudioParam Compile(FMODAudioParamOverride param) => param.Type switch
		{
			FMODAudioParamOverride.ParamType.DISCRETE => AudioParam.NamedInt(AudioParamId.Custom, param.Name,
				(int)param.Value),
			FMODAudioParamOverride.ParamType.CONTINUOUS => AudioParam.NamedFloat(AudioParamId.Custom, param.Name,
				param.Value),
			FMODAudioParamOverride.ParamType.LABELED => AudioParam.NamedString(AudioParamId.Custom, param.Name,
				param.Labels[(int)param.Value]),
			_ => default
		};

#if UNITY_EDITOR
		public static FMODAudioEventDescription Create(EventReference reference,
			IReadOnlyList<FMODAudioParamOverride> parameters, bool is3D) => new()
		{
			_clip = new FMODAudioClip(reference),
			_parameters = (parameters ?? Array.Empty<FMODAudioParamOverride>()).Select(CloneParameter).ToList(),
			_contractParameters = new List<IAudioParamDescription>(),
			_is3D = is3D
		};

		internal FMODAudioEventDescription Clone() => new()
		{
			_clip = _clip == null ? null : new FMODAudioClip(_clip.Event),
			_parameters = _parameters == null
				? new List<FMODAudioParamOverride>()
				: _parameters.Select(CloneParameter).ToList(),
			_contractParameters = CloneContractParameters(_contractParameters),
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
	}
}