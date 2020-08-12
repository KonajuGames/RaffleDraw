using System;

namespace Konaju.File.Csv
{
	/// <summary>
	/// Allows a field to specify a column name in the CSV file.
	/// </summary>
	public class CsvColumnAttribute : Attribute
	{
		/// <summary>
		/// The name of the column in the CSV file.
		/// </summary>
		public string ColumnName { get; private set; }

		/// <summary>
		/// Creates an instance of CsvColumnAttribute with the column name.
		/// </summary>
		/// <param name="columnName"></param>
		public CsvColumnAttribute(string columnName)
		{
			ColumnName = columnName;
		}
	}
}
