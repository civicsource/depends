using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Archon.Depends
{
	public class Program
	{
		private static string ApplicationName;
		private static string ApplicationDll;
		private static bool SaveToDatabase;

		static void Main(string[] args)
		{
			VerifyArguments(args);

			Assembly assembly = Assembly.LoadFrom(ApplicationDll);

			int actions = 0;
			IEnumerable<Type> controllers = null;

			using (var conn = new SqlConnection("server=devdb01;database=Dependencies;integrated security=true;"))
			{
				conn.Open();

				if (SaveToDatabase)
					AddApplication(conn);

				controllers = assembly.GetTypes()
					.Where(a => !a.IsAbstract && !a.IsInterface && a.Name.EndsWith("Controller"));

				foreach (var controller in controllers)
				{
					Console.WriteLine(controller.Name);

					var attributes = controller.GetCustomAttributes(typeof(AuthorizeAttribute));

					foreach (var a in attributes)
					{
						var attribute = ((AuthorizeAttribute)a);

						if (attribute.Roles != null)
						{
							Console.WriteLine("\t" + attribute.Roles);
						}
						else
						{
							Console.WriteLine("\t Authorize Attribute");
						}
					}

					var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
						.Where(m => !m.IsSpecialName);

					foreach (var action in methods)
					{
						Console.WriteLine("\t\t" + action.Name);

						if (SaveToDatabase)
							AddControllerAction(conn, controller, action);

						var methodAttributes = action.GetCustomAttributes(typeof(AuthorizeAttribute));

						foreach (var m in methodAttributes)
						{
							var attribute = ((AuthorizeAttribute)m);

							if (attribute.Roles != null)
							{
								Console.WriteLine("\t\t\t" + attribute.Roles);
							}
							else
							{
								Console.WriteLine("\t\t\t Authorize Attribute");
							}
						}
					}

					actions += methods.Count();
				}

				conn.Close();
			}

			Console.WriteLine();
			Console.WriteLine(args[0]);
			Console.WriteLine("Controllers:" + controllers.Count());
			Console.WriteLine("Actions:" + actions);
		}

		private static void VerifyArguments(string[] args)
		{
			if (string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
				throw new ApplicationException("Usage:  <Application Name> <Application Main dll> <Add/Refresh database>");

			ApplicationName = args[0];
			ApplicationDll = args[1];
			SaveToDatabase = bool.Parse(args[2]);
		}

		private static void AddApplication(SqlConnection conn)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT COUNT(*) FROM dep.Applications WHERE name = @applicationName";
				command.Parameters.AddWithValue("@applicationName", ApplicationName);

				var applications = (int)command.ExecuteScalar();

				if (applications == 0)
				{
					using (var write = conn.CreateCommand())
					{
						write.CommandText = "INSERT INTO dep.Applications VALUES(@applicationName)";
						write.Parameters.AddWithValue("@applicationName", ApplicationName);
						write.ExecuteNonQuery();
					}
				}
			}
		}

		private static void AddController(SqlConnection conn, Type controller)
		{
			var applicationId = GetApplicationId(conn);

			if (applicationId != 0)
			{
				using (SqlCommand command = conn.CreateCommand())
				{
					command.CommandText = "SELECT COUNT(*) FROM dep.Controllers WHERE name = @controllerName";
					command.Parameters.AddWithValue("@controllerName", controller.FullName);

					var controllers = (int)command.ExecuteScalar();

					if (controllers == 0)
					{
						using (var write = conn.CreateCommand())
						{
							write.CommandText = "INSERT INTO dep.Controllers VALUES(@controllerName, 0, @appId)";
							write.Parameters.AddWithValue("@controllerName", controller.FullName);
							write.Parameters.AddWithValue("@appId", applicationId);
							write.ExecuteNonQuery();
						}
					}
				}
			}
		}

		private static void AddControllerAction(SqlConnection conn, Type controller, MethodInfo action)
		{
			var controllerId = GetControllerId(conn, controller.FullName);

			if (controllerId != 0)
			{
				using (var write = conn.CreateCommand())
				{
					write.CommandText = "INSERT INTO dep.Actions VALUES(@actionName, null, @controllerId)";
					write.Parameters.AddWithValue("@actionName", action.Name);
					write.Parameters.AddWithValue("@controllerId", controllerId);
					write.ExecuteNonQuery();
				}
			}
		}

		private static int GetApplicationId(SqlConnection conn)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT Id FROM dep.Applications WHERE name = @applicationName";
				command.Parameters.AddWithValue("@applicationName", ApplicationName);

				using (SqlDataReader data = command.ExecuteReader())
				{
					if (data.HasRows)
					{
						data.Read();
						return (int)data["ID"];
					}
					else
						return 0;
				}
			}
		}

		private static int GetControllerId(SqlConnection conn, string name)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT Id FROM dep.Controllers WHERE name = @controllerName";
				command.Parameters.AddWithValue("@controllername", name);

				using (SqlDataReader data = command.ExecuteReader())
				{
					if (data.HasRows)
					{
						data.Read();
						return (int)data["Id"];
					}
					else
						return 0;
				}
			}
		}
	}
}
