using System;
using System.Text;

namespace DeepStrip
{
	internal static class Extensions
	{
		public static int Width(this int @this) => @this <= 1 ? 1 : (int) Math.Ceiling(Math.Log10(@this));

		private static StringBuilder AppendPadLeftInternal(this StringBuilder @this, int width, int paddingWidth, char paddingChar)
		{
			var padding = paddingWidth - width;
			return padding == 0 ? @this : @this.Append(paddingChar, padding - 1).Append(' ');
		}

		public static StringBuilder AppendPadLeft(this StringBuilder @this, int value, int paddingWidth, char paddingChar = '.') =>
			@this.AppendPadLeftInternal(value.Width(), paddingWidth, paddingChar).Append(value);

		public static StringBuilder AppendPadLeft(this StringBuilder @this, string value, int paddingWidth, char paddingChar = ' ') =>
			@this.AppendPadLeftInternal(value.Length, paddingWidth, paddingChar).Append(value);
	}
}
