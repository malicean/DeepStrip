using System;
using System.IO;
using System.Text;
using CommandLine;
using DeepStrip.Core;
using Mono.Cecil;

namespace DeepStrip
{
	internal static class Program
	{
		private static int Main(string[] args) =>
			(int) new Parser(x =>
				{
					x.EnableDashDash = true;
					x.HelpWriter = Console.Error;
				})
				.ParseArguments<Options>(args)
				.MapResult(MainParsed, _ => ExitCode.InvalidArguments);

		private static ExitCode MainParsed(Options opt)
		{
#if !DEBUG
			try
#endif
			{
				FileStream input;
				try
				{
					input = new FileStream(opt.InputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				}
				catch (Exception e)
				{
					return ExitCode.InvalidInput.Error(e);
				}

				using (input)
				{
					FileStream output;
#if DEBUG
					try
#endif
					{
						output = new FileStream(opt.OutputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
					}
#if DEBUG
					catch (Exception e)
					{
						return ExitCode.InvalidOutput.Error(e);
					}
#endif

					using (output)
                    {
                    	var resolver = new DefaultAssemblyResolver();
                    	{
                    		var include = opt.IncludeDirectories;
                    		if (include is not null)
                    			foreach (var item in include)
                    				resolver.AddSearchDirectory(item);
                    	}

                    	var parameters = new ReaderParameters()
                    	{
                    		AssemblyResolver = resolver
                    	};

                    	ModuleDefinition module;
#if !DEBUG
                    	try
#endif
                    	{
                    		module = ModuleDefinition.ReadModule(input, parameters);
                    	}
#if !DEBUG
                    	catch (Exception e)
                    	{
                    		return ExitCode.InvalidModule.Error(e);
                    	}
#endif

	                    StripStats stats;
                    	using (module)
                    	{
	                        if (opt.Verbose)
		                        Console.WriteLine($"Read '{opt.InputPath}': {module.Assembly.Name}");

	                        stats = Members.Strip(module);

#if !DEBUG
                    		try
#endif
                    		{
                    			module.Write(output);
                    		}
#if !DEBUG
                    		catch (Exception e)
                    		{
                    			Console.Error.WriteLine("Writing error. " + e);

                    			return ExitCode.WritingError;
                    		}
#endif
                    	}

                        if (opt.Verbose)
	                        PrintStatistics((i: input.Length, o: output.Length), stats);
                    }

                    return ExitCode.Ok;
				}
			}
#if !DEBUG
			catch (Exception e)
			{
				Console.Error.WriteLine("Unhandled exception. " + e);

				return ExitCode.InternalError;
			}
#endif
		}

		// I know this is really ugly but it works and doesn't require a third party library
		private static void PrintStatistics((long i, long o) len, StripStats stats)
		{
			var mag = (i: len.i.GetMagnitude(), o: len.o.GetMagnitude());
            var scaled = (i: mag.i.ScaleNumeric(len.i), o: mag.o.ScaleNumeric(len.o));
            var ratio = 1 - (double) len.o / len.i;
            var sizes = (i: $"{scaled.i:F1} {mag.i}", o: $"{scaled.o:F1} {mag.o}", ratio: ratio.ToString("P0"));
            var width = 1 + Math.Max(stats.Max.Width(), Math.Max(sizes.i.Length, Math.Max(sizes.o.Length, sizes.ratio.Length)));

            const string headerText = "Statistics";
            var headerWidth = width + 23 - headerText.Length - 2;
            var headerLeft = headerWidth / 2;

            var builder = new StringBuilder()
                .Append("┌────────────────────────").Append('─', width).Append("─┐").AppendLine()
                .Append("│ ").Append('#', headerLeft).Append(' ').Append(headerText).Append(' ').Append('#', headerWidth - headerLeft).Append(" │").AppendLine()
                .Append("├────────────────────────").Append('─', width).Append("─┤").AppendLine()
                .Append("│ Sizes                  ").Append(' ', width).Append(" │").AppendLine()
                .Append("│ ├── Source ............").AppendPadLeft(sizes.i, width).Append(" │").AppendLine()
                .Append("│ ├── Result ............").AppendPadLeft(sizes.o, width).Append(" │").AppendLine()
                .Append("│ └── Truncation Ratio ..").AppendPadLeft(sizes.ratio, width).Append(" │").AppendLine()
                .Append("│                        ").Append(' ', width).Append(" │").AppendLine()
                .Append("│ Types .................").AppendPadLeft(stats.Types.MemberCount, width).Append(" │").AppendLine()
                .Append("│ ├── Attributes ........").AppendPadLeft(stats.Types.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ ├── Fields ............").AppendPadLeft(stats.Fields.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │   └── Attributes ....").AppendPadLeft(stats.Fields.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ ├── Properties ........").AppendPadLeft(stats.Properties.Both.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │   ├── Attributes ....").AppendPadLeft(stats.Properties.Both.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ │   ├── Getters .......").AppendPadLeft(stats.Properties.Method1.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │   │   └── Attributes ").AppendPadLeft(stats.Properties.Method1.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ │   └── Setters .......").AppendPadLeft(stats.Properties.Method2.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │       └── Attributes ").AppendPadLeft(stats.Properties.Method2.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ ├── Events ............").AppendPadLeft(stats.Events.Both.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │   ├── Attributes ....").AppendPadLeft(stats.Events.Both.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ │   ├── Adders ........").AppendPadLeft(stats.Events.Method1.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │   │   └── Attributes ").AppendPadLeft(stats.Events.Method1.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ │   └── Removers ......").AppendPadLeft(stats.Events.Method2.MemberCount, width).Append(" │").AppendLine()
                .Append("│ │       └── Attributes ").AppendPadLeft(stats.Events.Method2.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("│ └── Methods ...........").AppendPadLeft(stats.Methods.MemberCount, width).Append(" │").AppendLine()
                .Append("│     └── Attributes ....").AppendPadLeft(stats.Methods.CustomAttributeCount, width).Append(" │").AppendLine()
                .Append("└────────────────────────").Append('─', width).Append("─┘");

            Console.WriteLine(builder.ToString());
		}
	}
}
