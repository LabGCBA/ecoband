using System;
namespace EcoBand
{
	public class Service
	{
		public Service(String Label, String Uuint)
		{
			label = Label;
			uuint = Uuint;
		}

		public String label { get; }
		public String uuint { get; }
	}
}
