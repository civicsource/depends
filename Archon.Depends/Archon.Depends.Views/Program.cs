using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archon.Depends.Views
{
	public class Program
	{
		static void Main(string[] args)
		{
			if (string.IsNullOrEmpty(args[0]))
				throw new ApplicationException("Need path to code base as argument");

			var views = Directory.GetFiles(args[0], "*.spark", SearchOption.AllDirectories).Union(Directory.GetFiles(args[0], "*.cshtml", SearchOption.AllDirectories));

			foreach (var v in views)
			{
				Console.WriteLine(Path.GetFileName(v));
			}

			Console.WriteLine();
			Console.WriteLine(args[0]);
			Console.WriteLine("Views: " + views.Count());
		}
	}
}
