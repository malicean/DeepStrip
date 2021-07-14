namespace DeepStrip.Core
{
	internal ref struct StripStats
	{
		public MemberStats Types;
		public MemberStats Fields;
		public DualMethod Properties;
		public DualMethod Events;
		public MemberStats Methods;

		public struct DualMethod
		{
			public MemberStats Both;
			public MemberStats Method1;
			public MemberStats Method2;
		}

		public struct MemberStats
		{
			public uint MemberCount;
			public uint CustomAttributeCount;
		}
	}
}
