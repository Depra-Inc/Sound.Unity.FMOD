// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

namespace Depra.Sound
{
	internal static class Module
	{
		public const int DEFAULT_ORDER = 52;
		public const string LOG_FORMAT = "[Sound] {0}";
		public const string MENU_PATH = nameof(Depra) + SLASH +
		                                nameof(Sound) + SLASH +
		                                nameof(FMOD) + SLASH;

		private const string SLASH = "/";
	}
}