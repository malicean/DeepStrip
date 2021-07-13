using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using DeepStrip.Core;
using DeepStrip.Streams;
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
				Stream input;
				{
					var path = opt.InputPath;
					try
					{
						input = path is not null
							? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
							: new BufferedStandardInput();
					}
					catch (Exception e)
					{
						return ExitCode.InvalidInput.Error(e);
					}
				}

				using (input)
				{
					Stream output;
                    {
                    	var path = opt.OutputPath;
                    	try
                    	{
                    		output = path is not null
                    			? new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None)
                    			: new BufferedStandardOutput();
                    	}
                    	catch (Exception e)
                    	{
                    		return ExitCode.InvalidOutput.Error(e);
                    	}
                    }

                    using (output)
                    {
                    	var resolver = new DefaultAssemblyResolver();
                    	{
                    		var include = opt.DependencyDirectories;
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
                    		var stripper = new Stripper(module);
                    		stripper.Strip();

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

#if DEBUG
	                    {
	                        var i = input.Length;
	                        var o = output.Length;
	                        var deflation = 1 - (double) o / i;
	                    }
#endif
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
