// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	public sealed class FMODInteger : IAudioSourceParameter
	{
		[field: SerializeField] public string Name { get; private set; }
		[field: SerializeField] public int Value { get; private set; }
		[field: SerializeField] public bool IgnoreSeekSpeed { get; private set; }

		public FMODInteger() { }
		public FMODInteger(string name, int value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}

	[Serializable]
	[SerializeReferenceIcon("d_FilterByLabel")]
	public sealed class FMODLabel : IAudioSourceParameter
	{
		[field: SerializeField] public string Name { get; private set; }
		[field: SerializeField] public string Value { get; private set; }
		[field: SerializeField] public bool IgnoreSeekSpeed { get; private set; }

		public FMODLabel() { }
		public FMODLabel(string name, string value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}

	[Serializable]
	public sealed class FMODSingle : IAudioSourceParameter
	{
		[field: SerializeField] public string Name { get; private set; }
		[field: SerializeField] public float Value { get; private set; }
		[field: SerializeField] public bool IgnoreSeekSpeed { get; private set; }

		public FMODSingle() { }
		public FMODSingle(string name, float value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}