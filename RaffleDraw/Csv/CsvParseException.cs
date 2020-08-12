using System;

namespace Konaju.File.Csv
{
	/// <summary>
	/// Exception thrown when a parse error occurs in CSV data.
	/// </summary>
	public class CsvParseException : Exception
	{
		/// <summary>
		/// Creates an instance of CsvParseException.
		/// </summary>
		/// <param name="message">The message to pass in the exception.</param>
		public CsvParseException(string message) : base(message) { }
	}
}
