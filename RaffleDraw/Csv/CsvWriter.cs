using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Konaju.File.Csv
{
	/// <summary>
	/// Methods for writing comma-separated values (CSV) data.  Abides by RFC 4180.
	/// </summary>
	static public class CsvWriter
	{
		/// <summary>
		/// Write the record items to a CSV-formatted data stream.
		/// </summary>
		/// <typeparam name="T">The type that represents a record in the data.</typeparam>
		/// <param name="stream">The stream to write the CSV data to.</param>
		/// <param name="items">A list of instances of type <c>T</c> to write to the stream.</param>
		/// <exception cref="CsvWriteException">A <c>CsvWriteException</c> is thrown when the input data
		/// cannot be formatted to comply with the CSV format.</exception>
		static public void Write<T>(Stream stream, IList<T> items) where T: new()
		{
			using (var writer = new StreamWriter(stream))
			{
				// Find which fields and properties in T we should process and the column names
				var headers = new List<string>();
				var memberMap = new Dictionary<string, MemberInfo>();
				CsvTypeUtilities.InterrogateType(typeof(T), memberMap, CsvInterrogateFlags.Write, headers);

				// Write the headers
				for (int i = 0; i < headers.Count - 1; ++i)
				{
					writer.Write(headers[i]);
					writer.Write(",");
				}
				writer.WriteLine(headers[headers.Count - 1]);

				// Write each item
				var stringType = typeof(string);
				int count = items.Count;
				for (int i = 0; i < count; ++i)
				{
					var item = items[i];
					for (int h = 0; h < headers.Count; ++h)
					{
                        if (h > 0)
                        {
                            writer.Write(",");
                        }
                        // Get the member info for this column
                        if (memberMap.TryGetValue(headers[h], out MemberInfo memberInfo))
                        {
                            switch (memberInfo.MemberType)
                            {
                                case MemberTypes.Field:
                                    {
                                        var fieldInfo = (FieldInfo)memberInfo;
                                        var value = Convert.ToString(fieldInfo.GetValue(item), CultureInfo.InvariantCulture);
                                        if (fieldInfo.FieldType == stringType)
                                            value = "\"" + value + "\"";
                                        writer.Write(value);
                                    }
                                    break;
                                case MemberTypes.Property:
                                    {
                                        var propertyInfo = (PropertyInfo)memberInfo;
                                        var value = Convert.ToString(propertyInfo.GetValue(item, null), CultureInfo.InvariantCulture);
                                        if (propertyInfo.PropertyType == stringType)
                                            value = "\"" + value + "\"";
                                        writer.Write(value);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            throw new CsvWriteException("Could not find member info for column " + headers[i]);
                        }
                    }

                    if (i < count - 1)
                    {
                        writer.WriteLine();
                    }
				}
			}
		}
	}
}
