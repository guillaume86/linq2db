﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Configuration;
	using Data;

	public static class MySqlTools
	{
		private static readonly Lazy<IDataProvider> _mySqlDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new MySqlDataProvider(ProviderName.MySqlOfficial);

			DataConnection.AddDataProvider(provider);

			if (DetectedProviderName == ProviderName.MySqlOfficial)
				DataConnection.AddDataProvider(ProviderName.MySql, provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _mySqlConnectorDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new MySqlDataProvider(ProviderName.MySqlConnector);

			DataConnection.AddDataProvider(provider);

			if (DetectedProviderName == ProviderName.MySqlConnector)
				DataConnection.AddDataProvider(ProviderName.MySql, provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.IsGlobal)
				return null;

			switch (css.ProviderName)
			{
				case ""                                          :
				case null                                        :
					if (css.Name.Contains("MySql"))
						goto case "MySql";
					break;
				case "MySql.Data"                                : return _mySqlDataProvider.Value;
				case "MySqlConnector"                            : return _mySqlConnectorDataProvider.Value;
				case "MySql"                                     :
				case var provider when provider.Contains("MySql"):

					if (css.Name.Contains("MySqlConnector"))
						return _mySqlConnectorDataProvider.Value;

					if (css.Name.Contains("MySql"))
						return _mySqlDataProvider.Value;

					return GetDataProvider();
			}

			return null;
		}

		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			switch (providerName)
			{
				case ProviderName.MySqlOfficial : return _mySqlDataProvider.Value;
				case ProviderName.MySqlConnector: return _mySqlConnectorDataProvider.Value;
			}

			return DetectedProviderName == ProviderName.MySqlOfficial
				? _mySqlDataProvider.Value
				: _mySqlConnectorDataProvider.Value;
		}

		static string? _detectedProviderName;

		public static string  DetectedProviderName =>
			_detectedProviderName ?? (_detectedProviderName = DetectProviderName());

		static string DetectProviderName()
		{
			try
			{
				var path = typeof(MySqlTools).Assembly.GetPath();

				if (!File.Exists(Path.Combine(path, $"{MySqlWrappers.MySqlDataAssemblyName}.dll")))
					if (File.Exists(Path.Combine(path, $"{MySqlWrappers.MySqlConnectorAssemblyName}.dll")))
						return ProviderName.MySqlConnector;
			}
			catch (Exception)
			{
			}

			return ProviderName.MySqlOfficial;
		}

		public static void ResolveMySql(string path, string? assemblyName)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(
				path,
				assemblyName
					?? (DetectedProviderName == ProviderName.MySqlOfficial
						? MySqlWrappers.MySqlDataAssemblyName
						: MySqlWrappers.MySqlConnectorAssemblyName));
		}

		public static void ResolveMySql(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, assembly.FullName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection               dataConnection,
			IEnumerable<T>               source,
			int                          maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>?  rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
