using System.Collections.Generic;
using CommandLine;

namespace DeepStrip
{
	internal class Options
	{
		[Option('d', "dependencies", HelpText = "The directories to find dependency assemblies in")]
		public IEnumerable<string>? DependencyDirectories { get; set; }

		[Option('i', "input", HelpText = "The file to read from. Defaults to stdin")]
		public string? InputPath { get; set; }

		[Option('o', "output", HelpText = "The file to output to. Defaults to stdout")]
		public string? OutputPath { get; set; }
	}
}
