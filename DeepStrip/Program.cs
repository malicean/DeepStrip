using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using DeepStrip.Core;
using Mono.Cecil;

namespace DeepStrip
{
	internal static class Program
	{
		private static int Main(string[] args) => (int) MainWithEnum(args);

		private static ExitCode MainWithEnum(IEnumerable<string> args) =>
			new Parser(x =>
				{
					x.EnableDashDash = true;
					x.HelpWriter = Console.Error;
				})
				.ParseArguments<Options>(args)
				.MapResult(MainWithOpt, _ => ExitCode.InvalidArguments);

		private static ExitCode MainWithOpt(Options opt)
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

                    	using (module)
                    	{
	                        Console.WriteLine(module.Assembly.Name);

                    		Members.Strip(module);

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

	                        string message;
	                        if (!opt.MachineReadable)
	                        {
		                        var mag = (i: len.i.GetMagnitude(), o: len.o.GetMagnitude());
		                        message =
			                        $"{mag.o.ScaleNumeric(len.o):F1} {mag.o} / {mag.i.ScaleNumeric(len.i):F1} {mag.i} ({(double) len.o / len.i:P0})";
	                        }
	                        else
		                        message = $"{len.o} / {len.i}";

	                        Console.WriteLine(message);
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
