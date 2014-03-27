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

					if (SaveToDatabase)
						AddController(conn, controller);

					ProcessAttributes(conn, controller);

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
								foreach(var at in attribute.Roles.Split(','))
								{
									Console.WriteLine("\t\t\t" + at.Trim());

									if (SaveToDatabase)
										AddActionPermission(conn, at.Trim(), action);
								}
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

		private static void ProcessAttributes(SqlConnection conn, Type controller)
		{
			var attributes = controller.GetCustomAttributes(typeof(AuthorizeAttribute));

			foreach (var a in attributes)
			{
				var attribute = ((AuthorizeAttribute)a);

				if (attribute.Roles != null)
				{
					foreach (var at in attribute.Roles.Split(','))
					{
						Console.WriteLine("\t" + at.Trim());

						if (SaveToDatabase)
							AddControllerPermission(conn, controller, at);
					}
				}
				else
				{
					Console.WriteLine("\t Authorize Attribute");
				}
			}
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

		private static void AddPermission(SqlConnection conn, string name)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT COUNT(*) FROM dep.Permissions WHERE name = @permissionName";
				command.Parameters.AddWithValue("@permissionName", name);

				var permissions = (int)command.ExecuteScalar();

				if (permissions == 0)
				{
					using (var write = conn.CreateCommand())
					{
						write.CommandText = "INSERT INTO dep.Permissions VALUES(@permissionName, null)";
						write.Parameters.AddWithValue("@permissionName", name);
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

		private static void AddControllerPermission(SqlConnection conn, Type controller, string permission)
		{
			var controllerId = GetControllerId(conn, controller.FullName);
			var permissionId = GetPermissionId(conn, permission);

			if (permissionId == 0)
				AddPermission(conn, permission);

			permissionId = GetPermissionId(conn, permission);

			if (controllerId != 0 && permissionId != 0)
			{
				using (var write = conn.CreateCommand())
				{
					write.CommandText = "INSERT INTO dep.ControllerPermissions VALUES(@controllerId, @permissionId)";
					write.Parameters.AddWithValue("@controllerId", controllerId);
					write.Parameters.AddWithValue("@permissionId", permissionId);
					write.ExecuteNonQuery();
				}
			}
		}

		private static void AddActionPermission(SqlConnection conn, string permission, MethodInfo action)
		{
			var actionId = GetActionId(conn, action.Name);
			var permissionId = GetPermissionId(conn, permission);

			if (permissionId == 0)
				AddPermission(conn, permission);

			permissionId = GetPermissionId(conn, permission);

			if (actionId != 0 && permissionId != 0)
			{
				using (var write = conn.CreateCommand())
				{
					write.CommandText = "INSERT INTO dep.ActionPermissions VALUES(@actionId, @permissionId)";
					write.Parameters.AddWithValue("@actionId", actionId);
					write.Parameters.AddWithValue("@permissionId", permissionId);
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

		private static int GetPermissionId(SqlConnection conn, string name)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT Id FROM dep.Permissions WHERE name = @permissionName";
				command.Parameters.AddWithValue("@permissionName", name);

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

		private static int GetActionId(SqlConnection conn, string name)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT Id FROM dep.Actions WHERE name = @actionName";
				command.Parameters.AddWithValue("@actionName", name);

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
