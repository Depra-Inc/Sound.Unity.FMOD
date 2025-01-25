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
		private IAudioSourceParameter[] _parameters;

		public FMODAudioTrack() { }
		public FMODAudioTrack(EventReference @event) => _event = @event;

		public void Play(FMODAudioSource source) => source.Play(new FMODAudioClip(_event), _parameters);

		void IAudioTrack.Play(IAudioSource source) => source.Play(new FMODAudioClip(_event), _parameters);

		AudioTrackSegment[] IAudioTrack.Deconstruct() => new[]
		{
			new AudioTrackSegment(new FMODAudioClip(_event), _parameters)
		};
	}
}