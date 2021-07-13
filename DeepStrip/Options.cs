using System.Collections.Generic;
using CommandLine;

namespace DeepStrip
{
	internal class Options
	{
		[Option('i', "include", HelpText = "The directories to find dependency assemblies in")]
		public IEnumerable<string>? IncludeDirectories { get; set; }

#pragma warning disable 8618
		[Value(0, Required = true, MetaName = "INPUT", HelpText = "The file to read from")]
		public string InputPath { get; set; }

		[Value(1, Required = true, MetaName = "OUTPUT", HelpText = "The file to write to")]
		public string OutputPath { get; set; }
#pragma warning restore 8618
	}
}
