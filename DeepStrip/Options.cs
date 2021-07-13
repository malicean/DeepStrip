using System.Collections.Generic;
using CommandLine;

namespace DeepStrip
{
	internal class Options
	{
		[Option('d', "dependencies",
			Required = false,
			HelpText = "Directories that contain dependency assemblies")]
		public IEnumerable<string>? DependencyDirectories { get; set; }

#if DEBUG
		[Value(0, Required = true)]
		public string Path { get; set; }
#endif
	}
}
