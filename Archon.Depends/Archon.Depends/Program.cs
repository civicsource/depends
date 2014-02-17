using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Archon.Depends
{
	public class Program
	{
		static void Main(string[] args)
		{
			if (string.IsNullOrEmpty(args[0]))
				throw new ApplicationException("need to provide DLL argument");

			string dll = args[0];

			Assembly assembly = Assembly.LoadFrom(dll);

			var controllers = assembly.GetTypes()
				.Where(a => !a.IsAbstract && !a.IsInterface && a.Name.EndsWith("Controller"));

			int actions = 0;

			foreach (var c in controllers)
			{
				Console.WriteLine(c.Name);

				var methods = c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
					.Where(m => !m.IsSpecialName);

				foreach (var a in methods)
				{
					Console.WriteLine("\t" + a.Name);
				}

				actions += methods.Count();
			}

			Console.WriteLine();
			Console.WriteLine(args[0]);
			Console.WriteLine("Controllers:" + controllers.Count());
			Console.WriteLine("Actions:" + actions);
		}
	}
}
