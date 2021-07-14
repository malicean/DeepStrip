using System;

namespace DeepStrip.Core
{
	internal ref struct StripStats
	{
		public MemberStats Types;
		public MemberStats Fields;
		public DualMethod Properties;
		public DualMethod Events;
		public MemberStats Methods;

		public int Max => Math.Max(Types.Max, Math.Max(Fields.Max, Math.Max(Properties.Max, Math.Max(Events.Max, Methods.Max))));

		public struct DualMethod
		{
			public MemberStats Both;
			public MemberStats Method1;
			public MemberStats Method2;

			public int Max => Math.Max(Both.Max, Math.Max(Method1.Max, Method2.Max));
		}

		public struct MemberStats
		{
			public int MemberCount;
			public int CustomAttributeCount;

			public int Max => Math.Max(MemberCount, CustomAttributeCount);
		}
	}
}
