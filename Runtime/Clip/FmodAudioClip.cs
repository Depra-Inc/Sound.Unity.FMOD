// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Clip;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceIcon("d_AudioClip Icon")]
	[SerializeReferenceMenuPath(nameof(FmodAudioClip))]
	public sealed class FmodAudioClip : IAudioClip
	{
		[SerializeField] private EventReference _event;

		public static implicit operator EventReference(FmodAudioClip clip) => clip._event;

		string IAudioClip.Name => _event.ToString();

		float IAudioClip.Duration => GetLength();

		public override string ToString() => _event.ToString();

		private float GetLength()
		{
			var description = RuntimeManager.GetEventDescription(_event);
			description.getLength(out var length);

			return length;
		}
	}
}