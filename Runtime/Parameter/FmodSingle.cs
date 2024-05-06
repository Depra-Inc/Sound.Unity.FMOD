// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using Depra.Sound.Parameter;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	public sealed class FmodSingle : IAudioClipParameter
	{
		[field: SerializeField] public string Name { get; private set; }
		[field: SerializeField] public float Value { get; private set; }
		[field: SerializeField] public bool IgnoreSeekSpeed { get; private set; }

		public FmodSingle(string name, float value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}