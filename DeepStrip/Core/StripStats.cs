namespace DeepStrip.Core
{
	internal ref struct StripStats
	{
		public uint CustomAttributes;
		public uint Types;
		public uint Fields;
		public DualMethod Properties;
		public DualMethod Events;
		public uint Methods;

		public struct DualMethod
		{
			public uint Both;
			public uint Method1;
			public uint Method2;
		}
	}
}
