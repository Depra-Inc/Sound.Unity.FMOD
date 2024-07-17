// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceIcon("d_AudioClip Icon")]
	[SerializeReferenceMenuPath(nameof(FMODAudioTrack))]
	public sealed class FMODAudioTrack : IAudioTrack<FMODAudioSource>
	{
		[SerializeField] private EventReference _event;

		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private IAudioClipParameter[] _parameters;

		public FMODAudioTrack() { }
		public FMODAudioTrack(EventReference @event) => _event = @event;

		public IAudioClip Play(FMODAudioSource source)
		{
			var clip = new FMODAudioClip(_event);
			source.Play(clip, _parameters);

			return clip;
		}

		IAudioClip IAudioTrack.Play(IAudioSource source) => Play((FMODAudioSource) source);
	}
}