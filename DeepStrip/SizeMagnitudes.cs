using System;

namespace DeepStrip
{
	internal enum SizeMagnitudes : byte
	{
		B,
		KiB,
		MiB,
		GiB,
		TiB
	}

	internal static class ExtSizeMagnitudes
	{
		private enum SizeMagnitudesValues : long
		{
			B = 1 << 10 * SizeMagnitudes.B,
			KiB = 1 << 10 * SizeMagnitudes.KiB,
			MiB = 1 << 10 * SizeMagnitudes.MiB,
			GiB = 1 << 10 * SizeMagnitudes.GiB,
			TiB = (long) 1 << 10 * SizeMagnitudes.TiB,
		}

		public static long GetNumeric(this SizeMagnitudes @this) => 1 << 10 * (byte) @this;

		public static SizeMagnitudes GetMagnitude(this long @this) =>
			@this switch
			{
				< 0 => throw new ArgumentOutOfRangeException(nameof(@this), @this, null),
				< (long) SizeMagnitudesValues.KiB => SizeMagnitudes.B,
				< (long) SizeMagnitudesValues.MiB => SizeMagnitudes.KiB,
				< (long) SizeMagnitudesValues.GiB => SizeMagnitudes.MiB,
				< (long) SizeMagnitudesValues.TiB => SizeMagnitudes.GiB,
				>= (long) SizeMagnitudesValues.TiB => throw new ArgumentOutOfRangeException(nameof(@this), @this, null),
			};

		public static double ScaleNumeric(this SizeMagnitudes @this, long bytes) => (double) bytes / @this.GetNumeric();
	}
}
