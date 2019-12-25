﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.DB2
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class DB2Tools
	{
		private static readonly Lazy<IDataProvider> _db2DataProviderzOS = new Lazy<IDataProvider>(() =>
		{
			var provider = new DB2DataProvider(ProviderName.DB2zOS, DB2Version.zOS);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _db2DataProviderLUW = new Lazy<IDataProvider>(() =>
		{
			var provider = new DB2DataProvider(ProviderName.DB2LUW, DB2Version.LUW);

			DataConnection.AddDataProvider(ProviderName.DB2, provider);
			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case ""             :
				case null           :

					if (css.Name == "DB2")
						goto case "DB2";
					break;

				case "IBM.Data.DB2.Core" :
					goto case "DB2";

				case "DB2"               :
				case "IBM.Data.DB2"      :

					if (css.Name.Contains("LUW") || css.Name.Contains("z/OS") || css.Name.Contains("zOS"))
						break;

					if (AutoDetectProvider)
					{
						try
						{
							// TODO: mapping schema parameter will be removed by next commit
							DB2Wrappers.Initialize(new Mapping.MappingSchema());
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = DB2Wrappers.CreateDB2Connection(cs))
							{
								conn.Open();

								var iszOS = conn.eServerType == DB2Wrappers.DB2ServerTypes.DB2_390;

								return iszOS ? _db2DataProviderzOS.Value : _db2DataProviderLUW.Value;
							}
						}
						catch
						{
						}
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(DB2Version version = DB2Version.LUW)
		{
			if (version == DB2Version.zOS)
				return _db2DataProviderzOS.Value;

			return _db2DataProviderLUW.Value;
		}

		public static void ResolveDB2(string path)
		{
			new AssemblyResolver(path, DB2Wrappers.AssemblyName);
		}

		public static void ResolveDB2(Assembly assembly)
		{
			new AssemblyResolver(assembly, DB2Wrappers.AssemblyName);
		}

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided DB2 connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbConnection connection, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="version">DB2 version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbTransaction transaction, DB2Version version = DB2Version.LUW)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		/// <summary>
		/// Default bulk copy mode, used for DB2 by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
		/// methods, if mode is not specified explicitly.
		/// Default value: <see cref="BulkCopyType.ProviderSpecific"/>.
		/// </summary>
		public static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection dataConnection,
			IEnumerable<T> source,
			int maxBatchSize = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType = BulkCopyType.ProviderSpecific,
					MaxBatchSize = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection dataConnection,
			IEnumerable<T> source,
			int? bulkCopyTimeout = null,
			bool keepIdentity = false,
			int notifyAfter = 0,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType = BulkCopyType.ProviderSpecific,
					BulkCopyTimeout = bulkCopyTimeout,
					KeepIdentity = keepIdentity,
					NotifyAfter = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
