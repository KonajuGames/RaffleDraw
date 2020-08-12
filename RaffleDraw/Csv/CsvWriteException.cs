using System;

namespace Konaju.File.Csv
{
	/// <summary>
	/// Exception thrown when a write error occurs in CSV data.
	/// </summary>
	public class CsvWriteException : Exception
	{
		/// <summary>
		/// Creates an instance of CsvWriteException.
		/// </summary>
		/// <param name="message">The message to pass in the exception.</param>
		public CsvWriteException(string message)
			: base(message)
		{
		}
	}
}
