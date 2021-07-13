using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepStrip.Core
{
	internal class Stripper
	{
		private static readonly HashSet<string> AttributeWhitelist = new()
		{
			"Microsoft.CodeAnalysis.EmbeddedAttribute",
			"System.Diagnostics.CodeAnalysis.AllowNullAttribute",
			"System.Diagnostics.CodeAnalysis.DisallowNullAttribute",
			"System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute",
			"System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute",
			"System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute",
			"System.Diagnostics.CodeAnalysis.MaybeNullAttribute",
			"System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute",
			"System.Diagnostics.CodeAnalysis.MemberNotNullAttribute",
			"System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute",
			"System.Diagnostics.CodeAnalysis.NotNullAttribute",
			"System.Diagnostics.CodeAnalysis.NotNullWhenAttribute",
			"System.Runtime.CompilerServices.IsExternalInit",
			"System.Runtime.CompilerServices.IsReadOnlyAttribute",
			"System.Runtime.CompilerServices.NullableAttribute",
			"System.Runtime.CompilerServices.NullableContextAttribute"
		};

		private readonly ModuleDefinition _module;

		public Stripper(ModuleDefinition module)
		{
			_module = module;
		}

		public void Strip() => Strip(_module.Types);

		private void Strip(IList<CustomAttribute> attributes) =>
			attributes.RemoveWhere(x =>
			{
				var type = x.AttributeType.Resolve();
				return StripPredicates.Type(type.Attributes) && !AttributeWhitelist.Contains(type.Namespace);
			});

		private void Strip(IList<TypeDefinition> types)
		{
			for (var i = types.Count - 1; i >= 0; --i)
			{
				var type = types[i];

				if (AttributeWhitelist.Contains(type.Namespace))
					continue;

				if (StripPredicates.Type(type.Attributes))
				{
					types.RemoveAt(i);
					continue;
				}

				Strip(type.CustomAttributes);

				{
					var fields = type.Fields;
					for (var j = fields.Count - 1; j >= 0; --j)
					{
						var field = fields[j];
						if (StripPredicates.Field(field.Attributes))
						{
							fields.RemoveAt(j);
							continue;
						}

						Strip(field.CustomAttributes);
					}
				}

				{
					var getter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
						x => x.GetMethod,
						(x, v) => x.GetMethod = v
					);
					var setter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
						x => x.SetMethod,
						(x, v) => x.SetMethod = v
					);

					var properties = type.Properties;
					for (var j = properties.Count - 1; j >= 0; --j)
					{
						var property = properties[j];

						if (DualStripper(type, property, getter, setter))
							type.Properties.RemoveAt(j);

						Strip(property.CustomAttributes);
					}
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

					var events = type.Events;
					for (var j = events.Count - 1; j >= 0; --j)
					{
						var @event = events[j];

						if (DualStripper(type, @event, add, remove))
							type.Events.RemoveAt(j);

						Strip(@event.CustomAttributes);
					}
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

						Strip(method.CustomAttributes);
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

		private bool DualStripper<T>(TypeDefinition type, T item, PropertyReference<T, MethodDefinition?> method1,
			PropertyReference<T, MethodDefinition?> method2)
		{
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
					return true;
				else
					bound1.Value = null;
			}
			else if (second.Strip)
				bound2.Value = null;

			return false;
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
