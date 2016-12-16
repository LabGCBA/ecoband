using System;

namespace EcoBand
{
	public class Characteristic
	{
		public Characteristic(String Label, String Uuint)
		{
			label = Label;
			uuint = Uuint;
		}

		public String label { get; }
		public String uuint { get; }
}
