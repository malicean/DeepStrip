using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;
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
				.MapResult(MainWithOpt, _ => ExitCode.BadArguments);

		private static ExitCode MainWithOpt(Options opt)
		{
#if !DEBUG
			try
#endif
			{


				using var inbuffer = new MemoryStream();
				{
#if DEBUG
					using var stdin = new FileStream(opt.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
#else
					using var stdin = Console.OpenStandardInput();
#endif
					stdin.CopyTo(inbuffer);
					inbuffer.Position = 0;
				}

				using var outbuffer = new MemoryStream((int) Math.Min(64 * 1024 * 1024, inbuffer.Length));
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
						module = ModuleDefinition.ReadModule(inbuffer, parameters);
					}
#if !DEBUG
					catch (Exception e)
					{
						Console.Error.WriteLine("Invalid module. " + e);

						return ExitCode.BadModule;
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
							module.Write(outbuffer);
						}
#if !DEBUG
						catch (Exception e)
						{
							Console.Error.WriteLine("Writing error. " + e);

							return ExitCode.WritingError;
						}
#endif
					}

					outbuffer.Position = 0;
				}

#if DEBUG
				{
					var i = inbuffer.Length;
					var o = outbuffer.Length;
					Console.WriteLine($"{o} / {i} B ({1 - (double) o / i:P0} deflation)");
				}
#else
				{
					using var stdout = Console.OpenStandardOutput();
					outbuffer.CopyTo(stdout);
				}
#endif

				return ExitCode.Ok;
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
