using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepStrip.Core
{
	internal static class Members
	{
		public static void Strip(ModuleDefinition module) => Types.Strip(module.Types);

		private static class CustomAttributes
		{
			public static readonly HashSet<string> NamespaceWhitelist = new()
			{
				"Microsoft.CodeAnalysis",
				"System.Diagnostics.CodeAnalysis",
				"System.Runtime.CompilerServices"
			};

			private static bool Predicate(CustomAttribute attr) => attr.Constructor is null;

			public static void Strip(IList<CustomAttribute> attributes) => attributes.RemoveWhere(Predicate);
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

			public static void Strip(IList<FieldDefinition> fields)
			{
				for (var i = fields.Count - 1; i >= 0; --i)
				{
					var field = fields[i];

					if (Predicate(field.Attributes))
						fields.RemoveAt(i);
				}
			}
		}

		private static class Properties
		{
			public static void Strip(IList<PropertyDefinition> properties, ICollection<MethodDefinition> methods)
			{
				var getter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
					x => x.GetMethod,
					(x, v) => x.GetMethod = v
				);
				var setter = new PropertyReference<PropertyDefinition, MethodDefinition?>(
					x => x.SetMethod,
					(x, v) => x.SetMethod = v
				);

				for (var i = properties.Count - 1; i >= 0; --i)
				{
					var property = properties[i];

					if (DualStripper(methods, property, getter, setter))
						properties.RemoveAt(i);
				}
			}
		}

		private static class Events
		{
			public static void Strip(IList<EventDefinition> events, ICollection<MethodDefinition> methods)
			{
				var add = new PropertyReference<EventDefinition, MethodDefinition?>(
					x => x.AddMethod,
					(x, v) => x.AddMethod = v
				);
				var remove = new PropertyReference<EventDefinition, MethodDefinition?>(
					x => x.RemoveMethod,
					(x, v) => x.RemoveMethod = v
				);

				for (var i = events.Count - 1; i >= 0; --i)
				{
					var @event = events[i];

					if (DualStripper(methods, @event, add, remove))
						events.RemoveAt(i);
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

			public static void Strip(IList<MethodDefinition> methods, bool isAttr)
			{
				for (var i = methods.Count - 1; i >= 0; --i)
				{
					var method = methods[i];

					if (Predicate(method.Attributes) && (!isAttr || !method.IsConstructor))
					{
						methods.RemoveAt(i);
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

			private static void StripMembersRecursive(IList<TypeDefinition> types)
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
							continue;
						}

						isAttr = true;
					}

					Fields.Strip(type.Fields);
					Properties.Strip(type.Properties, type.Methods);
					Events.Strip(type.Events, type.Methods);
					Methods.Strip(type.Methods, isAttr);

					StripMembersRecursive(type.NestedTypes);
				}
			}

			private static void StripAttributesRecursive(IEnumerable<TypeDefinition> types)
			{
				foreach (var type in types)
				{
					CustomAttributes.Strip(type.CustomAttributes);

					foreach (var field in type.Fields)
						CustomAttributes.Strip(field.CustomAttributes);
					foreach (var property in type.Properties)
						CustomAttributes.Strip(property.CustomAttributes);
					foreach (var @event in type.Events)
						CustomAttributes.Strip(@event.CustomAttributes);
					foreach (var method in type.Methods)
						CustomAttributes.Strip(method.CustomAttributes);

					StripAttributesRecursive(type.NestedTypes);
				}
			}

			public static void Strip(IList<TypeDefinition> types)
			{
				StripMembersRecursive(types);
				StripAttributesRecursive(types);
			}
		}

		private static bool DualStripper<T>(ICollection<MethodDefinition> methods, T item, PropertyReference<T, MethodDefinition?> method1,
			PropertyReference<T, MethodDefinition?> method2)
		{
			var bound1 = method1.Bind(item);
			var bound2 = method2.Bind(item);

			var (first, second) =
				(new DualMethodData(bound1.Value), new DualMethodData(bound2.Value));

			void Optimize(ref DualMethodData data)
			{
				var method = data.Method;
				ref var stripped = ref data.Stripped;

				if (method is null)
				{
					stripped = true;
					return;
				}

				stripped = Methods.Predicate(method.Attributes);
				if (stripped)
					methods.Remove(method);
				else
					Methods.Gut(method);
			}

			Optimize(ref first);
			Optimize(ref second);

			if (first.Stripped)
			{
				if (second.Stripped)
					return true;

				bound1.Value = null;
			}
			else if (second.Stripped)
				bound2.Value = null;

			return false;
		}

		private struct DualMethodData
		{
			public readonly MethodDefinition? Method;
			public bool Stripped;

			public DualMethodData(MethodDefinition? method)
			{
				Method = method;
				Stripped = false;
			}
		}
	}
}
