using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepStrip.Core
{
	internal static class Members
	{
		public static StripStats Strip(ModuleDefinition module) => Types.Strip(module.Types);

		private static class CustomAttributes
		{
			public static readonly HashSet<string> NamespaceWhitelist = new()
			{
				"Microsoft.CodeAnalysis",
				"System.Diagnostics.CodeAnalysis",
				"System.Runtime.CompilerServices"
			};

			private static bool Predicate(CustomAttribute attr) => attr.Constructor is null;

			public static void Strip(IList<CustomAttribute> attributes, ref StripStats stats) => attributes.RemoveWhere(Predicate, ref stats.CustomAttributes);
		}

		private static class Fields
		{
			private static bool Predicate(FieldAttributes attr)
			{
				var masked = attr & (FieldAttributes.FieldAccessMask & ~FieldAttributes.Static);
				return masked switch
				{
					FieldAttributes.Private => true,
					FieldAttributes.Assembly => true,
					FieldAttributes.FamANDAssem => true,
					_ => false
				};
			}

			public static void Strip(IList<FieldDefinition> fields, ref StripStats stats) =>
				fields.RemoveWhere(x => Predicate(x.Attributes), ref stats.Fields);
		}

		private static class Properties
		{
			public static void Strip(IList<PropertyDefinition> properties, ICollection<MethodDefinition> methods, ref StripStats stats)
			{
				var getter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
					x => x.GetMethod,
					(x, v) => x.GetMethod = v
				);
				var setter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
					x => x.SetMethod,
					(x, v) => x.SetMethod = v
				);

				ref var pstats = ref stats.Properties;
				for (var i = properties.Count - 1; i >= 0; --i)
				{
					var property = properties[i];

					if (property.HasOtherMethods)
						throw new NotSupportedException("An event had other methods: " + property);

					if (DualMethodStrip(property, getter, setter, methods, ref pstats))
					{
						properties.RemoveAt(i);
						++pstats.Both;
					}
				}
			}
		}

		private static class Events
		{
			public static void Strip(IList<EventDefinition> events, ICollection<MethodDefinition> methods, ref StripStats stats)
			{
				var add = new PropertyReference<EventDefinition, MethodDefinition?>(
					x => x.AddMethod,
					(x, v) => x.AddMethod = v
				);
				var remove = new PropertyReference<EventDefinition, MethodDefinition?>(
					x => x.RemoveMethod,
					(x, v) => x.RemoveMethod = v
				);

				ref var estats = ref stats.Events;
				for (var i = events.Count - 1; i >= 0; --i)
				{
					var @event = events[i];

					if (@event.InvokeMethod is not null)
						throw new NotSupportedException("An event had an invoke method: " + @event);

					if (@event.HasOtherMethods)
						throw new NotSupportedException("An event had other methods: " + @event);

					if (DualMethodStrip(@event, add, remove, methods, ref estats))
					{
						events.RemoveAt(i);
						++estats.Both;
					}
				}
			}
		}

		private static class Methods
		{
			public static bool Predicate(MethodAttributes attr)
			{
				var masked = attr & MethodAttributes.MemberAccessMask;
				return masked switch
				{
					MethodAttributes.Private => true,
					MethodAttributes.Assembly => true,
					MethodAttributes.FamANDAssem => true,
					_ => false
				};
			}

			public static void Gut(MethodDefinition method) => method.Body = new MethodBody(method);

			public static void Strip(IList<MethodDefinition> methods, bool isAttr, ref StripStats stats)
			{
				const MethodSemanticsAttributes ignoreSemantics =
					MethodSemanticsAttributes.AddOn |
					MethodSemanticsAttributes.RemoveOn |
					MethodSemanticsAttributes.Getter |
					MethodSemanticsAttributes.Setter;

				for (var i = methods.Count - 1; i >= 0; --i)
				{
					var method = methods[i];

					if (
						(!isAttr || !method.IsConstructor) &&
						(method.SemanticsAttributes & ignoreSemantics) == MethodSemanticsAttributes.None &&
						Predicate(method.Attributes))
					{
						methods.RemoveAt(i);
						++stats.Methods;
						continue;
					}

					Gut(method);
				}
			}
		}

		private static class Types
		{
			private static bool Predicate(TypeAttributes attr)
			{
				var masked = attr & TypeAttributes.VisibilityMask;
				return masked switch
				{
					TypeAttributes.NotPublic => true,
					TypeAttributes.NestedPrivate => true,
					TypeAttributes.NestedAssembly => true,
					TypeAttributes.NestedFamANDAssem => true,
					_ => false
				};
			}

			private static void StripMembersRecursive(IList<TypeDefinition> types, ref StripStats stats)
			{
				for (var i = types.Count - 1; i >= 0; --i)
				{
					var type = types[i];
					var isAttr = false;

					if (Predicate(type.Attributes))
					{
						if (type.BaseType?.FullName != "System.Attribute" || !CustomAttributes.NamespaceWhitelist.Contains(type.Namespace))
						{
							types.RemoveAt(i);
							++stats.Types;
							continue;
						}

						isAttr = true;
					}

					Fields.Strip(type.Fields, ref stats);
					Properties.Strip(type.Properties, type.Methods, ref stats);
					Events.Strip(type.Events, type.Methods, ref stats);
					Methods.Strip(type.Methods, isAttr, ref stats);

					StripMembersRecursive(type.NestedTypes, ref stats);
				}
			}

			private static void StripAttributesRecursive(IEnumerable<TypeDefinition> types, ref StripStats stats)
			{
				foreach (var type in types)
				{
					CustomAttributes.Strip(type.CustomAttributes, ref stats);

					foreach (var field in type.Fields)
						CustomAttributes.Strip(field.CustomAttributes, ref stats);
					foreach (var property in type.Properties)
						CustomAttributes.Strip(property.CustomAttributes, ref stats);
					foreach (var @event in type.Events)
						CustomAttributes.Strip(@event.CustomAttributes, ref stats);
					foreach (var method in type.Methods)
						CustomAttributes.Strip(method.CustomAttributes, ref stats);

					StripAttributesRecursive(type.NestedTypes, ref stats);
				}
			}

			public static StripStats Strip(IList<TypeDefinition> types)
			{
				var stats = new StripStats();
				StripMembersRecursive(types, ref stats);
				StripAttributesRecursive(types, ref stats);

				return stats;
			}
		}

		private static bool DualMethodStrip<T>(T item, PropertyReference<T, MethodDefinition?> method1,
			PropertyReference<T, MethodDefinition?> method2, ICollection<MethodDefinition> methods, ref StripStats.DualMethod stats)
		{
			var bound1 = method1.Bind(item);
			var bound2 = method2.Bind(item);

			bool Optimize(PropertyReference<T, MethodDefinition?>.Bound bound, ref uint stats)
			{
				var method = bound.Value;
				if (method is null)
					return true;

				if (Methods.Predicate(method.Attributes))
				{
					methods.Remove(method);
					bound.Value = null;
					++stats;

					return true;
				}

				Methods.Gut(method);
				return false;
			}

			// Yes, this MUST be & and not && because both sides must run
			return Optimize(bound1, ref stats.Method1) & Optimize(bound2, ref stats.Method2);
		}
	}
}
