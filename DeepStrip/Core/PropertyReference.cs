using System;

namespace DeepStrip.Core
{
	internal readonly struct PropertyReference<TInstance, TRet>
	{
		private readonly Func<TInstance, TRet> _get;
		private readonly Action<TInstance, TRet> _set;

		public PropertyReference(Func<TInstance, TRet> get, Action<TInstance, TRet> set)
		{
			_get = get;
			_set = set;
		}

		public Bound Bind(TInstance instance) => new(this, instance);

		public readonly struct Bound
		{
			private readonly PropertyReference<TInstance, TRet> _property;
			private readonly TInstance _instance;

			public TRet Value
			{
				get => _property._get(_instance);
				set => _property._set(_instance, value);
			}

			public Bound(PropertyReference<TInstance, TRet> property, TInstance instance)
			{
				_property = property;
				_instance = instance;
			}
		}
	}
}
