// SPDX-License-Identifier: Apache-2.0
// © 2024-2025 Depra <n.melnikov@depra.org>

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Configuration;
using Depra.Sound.Exceptions;
using FMOD;
using FMODUnity;
using JetBrains.Annotations;
using UnityEngine;
using static Depra.Sound.FMOD.Module;
using Debug = UnityEngine.Debug;

namespace Depra.Sound.FMOD
{
	[RequireComponent(typeof(StudioEventEmitter))]
	[AddComponentMenu(MENU_PATH + nameof(FMODEmitterDecorator), DEFAULT_ORDER)]
	internal sealed class FMODEmitterDecorator : MonoBehaviour
	{
		[SerializeField] private FMODAudioClip _clip;

		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private IAudioSourceParameter[] _parameters;

		private StudioEventEmitter _emitter;

		private void Awake()
		{
			_emitter = GetComponent<StudioEventEmitter>();
			Guard.AgainstNull(_emitter, nameof(_emitter));
		}

		[UsedImplicitly]
		public bool IsPlaying => _emitter && _emitter.IsPlaying();

		[UsedImplicitly]
		public void Play()
		{
			_emitter.EventReference = _clip;
			_emitter.Play();

			foreach (var parameter in _parameters)
			{
				if (!Write(parameter))
				{
					Debug.LogWarning($"[FMODEmitterDecorator] Failed to set parameter '{parameter.GetType().Name}'.");
				}
			}
		}

		[UsedImplicitly]
		public void Stop() => _emitter.Stop();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool Write(IAudioSourceParameter parameter)
		{
			RESULT result;
			switch (parameter)
			{
				case EmptyParameter:
					result = RESULT.OK;
					break;
				case SingleParameter single:
					_emitter.SetParameter(single.Name, single.Value);
					result = RESULT.OK;
					break;
				case IntegerParameter integer:
					_emitter.SetParameter(integer.Name, integer.Value);
					result = RESULT.OK;
					break;
				case LoopParameter loop:
					_emitter.SetParameter(FMODLoop.DEFAULT_NAME, FMODLoop.Convert(loop));
					result = RESULT.OK;
					break;
				case FMODSingle single:
					_emitter.SetParameter(single.Name, single.Value, single.IgnoreSeekSpeed);
					result = RESULT.OK;
					break;
				case FMODInteger integer:
					_emitter.SetParameter(integer.Name, integer.Value, integer.IgnoreSeekSpeed);
					result = RESULT.OK;
					break;
				default:
					result = RESULT.ERR_INVALID_PARAM;
					break;
			}

			if (result == RESULT.OK)
			{
				return true;
			}

			VerboseError(
				$"Parameter '{parameter.GetType().Name}' cannot be applied to '{name}' ({nameof(FMODAudioSource)}) with result: '{result}'");
			return false;
		}

		[Conditional(SOUND_DEBUG)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void VerboseError(string message) => Debug.LogErrorFormat(LOG_FORMAT, message);
	}
}