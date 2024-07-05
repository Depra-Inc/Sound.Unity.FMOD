// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Parameter;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceIcon("d_FilterByLabel")]
	public sealed class FmodLabel : IAudioClipParameter
	{
		[field: SerializeField] public string Name { get; private set; }
		[field: SerializeField] public string Value { get; private set; }
		[field: SerializeField] public bool IgnoreSeekSpeed { get; private set; }

		public FmodLabel() { }

		public FmodLabel(string name, string value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}