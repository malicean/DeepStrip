using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace DeepStrip.Core
{
	internal class Stripper
	{
		private static readonly HashSet<string> NamespaceWhitelist = new()
		{
			"Microsoft.CodeAnalysis",
			"System.Diagnostics.CodeAnalysis",
			"System.Runtime.CompilerServices",
		};

		private readonly ModuleDefinition _module;

		public Stripper(ModuleDefinition module)
		{
			_module = module;
		}

		public void Strip() => Strip(_module.Types);

		private void Strip(IList<TypeDefinition> types)
		{
			for (var i = types.Count - 1; i >= 0; --i)
			{
				var type = types[i];

				if (NamespaceWhitelist.Contains(type.Namespace))
					continue;

				if (StripPredicates.Type(type.Attributes))
				{
					types.RemoveAt(i);
					continue;
				}

				type.CustomAttributes.RemoveWhere(x =>
				{
					var attr = x.AttributeType.Resolve();
					return StripPredicates.Type(attr.Attributes) && !NamespaceWhitelist.Contains(attr.Namespace);
				});

				type.Fields.RemoveWhere(x => StripPredicates.Field(x.Attributes));

				{
					var getter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
						x => x.GetMethod,
						(x, v) => x.GetMethod = v
					);
					var setter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
						x => x.SetMethod,
						(x, v) => x.SetMethod = v
					);

					DualStripper(type, x => x.Properties, getter, setter);
				}

				{
					var add = new PropertyReference<EventDefinition, MethodDefinition?>(
						x => x.AddMethod,
						(x, v) => x.AddMethod = v
					);
					var remove = new PropertyReference<EventDefinition, MethodDefinition?>(
						x => x.RemoveMethod,
						(x, v) => x.RemoveMethod = v
					);

					DualStripper(type, x => x.Events, add, remove);
				}

				{
					var methods = type.Methods;
					for (var j = methods.Count - 1; j >= 0; --j)
					{
						var method = methods[j];

						if (StripPredicates.Method(method.Attributes))
						{
							methods.RemoveAt(j);
							continue;
						}

						Gut(method);
					}
				}

				{
					var nestedTypes = type.NestedTypes;
					type.NestedTypes.RemoveWhere(x => x.IsNestedAssembly);

					Strip(nestedTypes);
				}
			}
		}

		private void DualStripper<T>(TypeDefinition type, Func<TypeDefinition, Collection<T>> collectionGetter,
			PropertyReference<T, MethodDefinition?> method1, PropertyReference<T, MethodDefinition?> method2)
		{
			var collection = collectionGetter(type);
			for (var i = collection.Count - 1; i >= 0; --i)
			{
				var item = collection[i];

				var bound1 = method1.Bind(item);
				var bound2 = method2.Bind(item);

				var (first, second) =
					(new DualMethodData(bound1.Value), new DualMethodData(bound2.Value));

				void Optimize(ref DualMethodData data)
				{
					var method = data.Method;
					ref var strip = ref data.Strip;

					if (method is null)
					{
						strip = true;
						return;
					}

					strip = StripPredicates.Method(method.Attributes);
					if (strip)
						type.Methods.Remove(method);
					else
						Gut(method);
				}

				Optimize(ref first);
				Optimize(ref second);

				if (first.Strip)
				{
					if (second.Strip)
						collection.RemoveAt(i);
					else
						bound1.Value = null;
				}
				else if (second.Strip)
					bound2.Value = null;
			}
		}

		private void Gut(MethodDefinition method) => method.Body = new MethodBody(method);

		private struct DualMethodData
		{
			public readonly MethodDefinition? Method;
			public bool Strip;

			public DualMethodData(MethodDefinition? method)
			{
				Method = method;
				Strip = false;
			}
		}
	}
}
