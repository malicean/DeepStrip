using Mono.Cecil;

namespace DeepStrip
{
	internal static class StripPredicates
	{
		public static bool Field(FieldAttributes attr)
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

		public static bool Method(MethodAttributes attr)
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

		public static bool Type(TypeAttributes attr)
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
	}
}
