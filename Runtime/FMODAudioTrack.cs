// SPDX-License-Identifier: Apache-2.0
// © 2024-2025 Depra <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using Depra.SerializeReference.Extensions;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceIcon("d_AudioClip Icon")]
	[SerializeReferenceMenuPath(nameof(FMODAudioTrack))]
	public sealed class FMODAudioTrack : IAudioTrack
	{
		[SerializeField] private EventReference _event;

		[SerializeReferenceDropdown]
		[UnityEngine.SerializeReference]
		private IAudioSourceParameter[] _parameters;

		public FMODAudioTrack() { }
		public FMODAudioTrack(EventReference @event) => _event = @event;

		void IAudioTrack.ExtractSegments(IList<AudioTrackSegment> segments) =>
			segments.Add(new AudioTrackSegment(new FMODAudioClip(_event), _parameters));
	}
}