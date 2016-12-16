using System;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand
{
	public class Band
	{
		public Band(IAdapter adapter)
		{
			Adapter = adapter;
		}

		protected readonly IAdapter Adapter;
		protected const string DeviceIdKey = "DeviceIdNavigationKey";
		protected const string ServiceIdKey = "ServiceIdNavigationKey";
		protected const string CharacteristicIdKey = "CharacteristicIdNavigationKey";
		protected const string DescriptorIdKey = "DescriptorIdNavigationKey";
	}
}
