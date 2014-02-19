using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archon.Depends.Views
{
	public class Program
	{
		private static bool SaveToDatabase;
		private static string ApplicationName;

		static void Main(string[] args)
		{
			if (string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || string.IsNullOrEmpty(args[2]))
				throw new ApplicationException("Usage: <Application Name> <Path to view resources> <True/false SaveToDatabase>");

			var views = Directory.GetFiles(args[1], "*.spark", SearchOption.AllDirectories).Union(Directory.GetFiles(args[1], "*.cshtml", SearchOption.AllDirectories));

			ApplicationName = args[0];
			SaveToDatabase = bool.Parse(args[2]);

			foreach (var v in views)
			{
				Console.WriteLine(v);

				using (var conn = new SqlConnection("server=devdb01;database=Dependencies;integrated security=true;"))
				{
					conn.Open();

					if (SaveToDatabase)
						AddView(conn, v);

					conn.Close();
				}
			}

			Console.WriteLine();
			Console.WriteLine(args[0]);
			Console.WriteLine("Views: " + views.Count());
		}

		private static void AddView(SqlConnection conn, string viewName)
		{
			var viewId = GetViewId(conn, viewName);
			var applicationId = GetApplicationId(conn);

			if (applicationId != 0)
			{
				using (SqlCommand command = conn.CreateCommand())
				{
					command.CommandText = "SELECT COUNT(*) FROM dep.Views WHERE name = @viewName AND appId = @applicationId";
					command.Parameters.AddWithValue("@viewName", viewName);
					command.Parameters.AddWithValue("@applicationId", applicationId);

					var views = (int)command.ExecuteScalar();

					if (views == 0)
					{
						using (var write = conn.CreateCommand())
						{
							write.CommandText = "INSERT INTO dep.Views VALUES(@viewName, @viewName, @applicationId)";
							write.Parameters.AddWithValue("@viewName", viewName);
							write.Parameters.AddWithValue("@applicationId", applicationId);
							write.ExecuteNonQuery();
						}
					}
				}
			}
		}


		private static int GetViewId(SqlConnection conn, string viewName)
		{
			using (SqlCommand command = conn.CreateCommand())
			{
				command.CommandText = "SELECT Id FROM dep.Views WHERE name = @viewName";
				command.Parameters.AddWithValue("@viewName", viewName);

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
	}
}
