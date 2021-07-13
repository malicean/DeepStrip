using System;

namespace DeepStrip
{
	internal enum ExitCode
	{
		Ok,
		InternalError,
		InvalidInput,
		InvalidOutput,
		InvalidArguments,
		InvalidModule,
		WritingError
	}

	internal static class ExtExitCode
	{
		public static ExitCode Error(this ExitCode @this, Exception e)
		{
			Console.Error.WriteLine($"{@this}: {e}");

			return @this;
		}
	}
}
