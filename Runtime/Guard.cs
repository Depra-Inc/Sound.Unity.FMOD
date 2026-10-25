using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Depra.Sound.FMOD
{
	internal static class Guard
	{
		[Conditional("SOUND_DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AgainstNull(object value, string parameterName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(parameterName);
			}
		}

		[Conditional("SOUND_DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AgainstUnsupportedType(IAudioClip actual, Type requiredType)
		{
			var actualType = actual.GetType();
			if (actualType!= requiredType)
			{
				throw new AudioClipTypeUnsupported(actualType, requiredType);
			}
		}
	}
}