using System.Collections.Generic;
using CommandLine;

namespace DeepStrip
{
	public class Options
	{
		[Option('i', "include",
			Required = false,
			HelpText = "Directories that contain dependency assemblies")]
		public IEnumerable<string>? IncludeDirectories { get; set; }

#if DEBUG
		[Value(0, Required = true)]
		public string Path { get; set; }
#endif
	}
}
