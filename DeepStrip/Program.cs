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
	                        Console.WriteLine(module.Assembly.Name);

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

                        {
	                        var len = (i: input.Length, o: output.Length);
	                        var mag = (i: len.i.GetMagnitude(), o: len.o.GetMagnitude());

	                        var builder = new StringBuilder()
		                        .AppendLine()
		                        .Append("Custom Attributes : ").Append(stats.CustomAttributes).AppendLine()
		                        .Append("Types             : ").Append(stats.Types).AppendLine()
		                        .Append("Fields            : ").Append(stats.Fields).AppendLine()
		                        .Append("Properties        : ").Append(stats.Properties.Both).AppendLine()
		                        .Append("    Getters       : ").Append(stats.Properties.Method1).AppendLine()
		                        .Append("    Setters       : ").Append(stats.Properties.Method2).AppendLine()
		                        .Append("Events            : ").Append(stats.Events.Both).AppendLine()
		                        .Append("    Adders        : ").Append(stats.Events.Method1).AppendLine()
		                        .Append("    Removers      : ").Append(stats.Events.Method2).AppendLine()
		                        .Append("Methods           : ").Append(stats.Methods).AppendLine()
		                        .AppendLine()
		                        .AppendFormat("{0:F1} {1} / {2:F1} {3} ({4:P0})", mag.o.ScaleNumeric(len.o), mag.o, mag.i.ScaleNumeric(len.i), mag.i, (double) len.o / len.i);

	                        Console.WriteLine(builder.ToString());
	                    }
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
	}
}
