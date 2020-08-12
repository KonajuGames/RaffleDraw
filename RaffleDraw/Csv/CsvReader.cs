using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Konaju.File.Csv
{
	/// <summary>
	/// Methods for parsing comma-separated values (CSV) data.  Abides by RFC 4180.
	/// </summary>
	static public class CsvReader
	{
		/// <summary>
		/// Read and parse the CSV data from the stream into a list of record types.
		/// </summary>
		/// <typeparam name="T">The type that represents a record in the data.</typeparam>
		/// <param name="stream">The source of the CSV data.</param>
		/// <returns>A list of instances of type <c>T</c> parsed from the data.</returns>
		/// <exception cref="CsvParseException">A <c>CsvParseException</c> is thrown when the input data
		/// does not conform to the expected format for CSV data.</exception>
		static public IList<T> Read<T>(Stream stream) where T: new()
		{
			using (var reader = new StreamReader(stream))
			{
				var result = new List<T>();
				int lineNumber = 0;

				// Read the column names from the first line
				var line = reader.ReadLine();
				++lineNumber;
				var headers = line.Split(',');

				// Find which fields and properties in T we should process
				var memberMap = new Dictionary<string, MemberInfo>();
				CsvTypeUtilities.InterrogateType(typeof(T), memberMap, CsvInterrogateFlags.Read);

				var builder = new StringBuilder();

				// Read the value lines
				while (!reader.EndOfStream)
				{
					// Must use object as the type for the record so structs get boxed. This allows the setting of the field values
					// by reflection to work. The record is unboxed before adding to the list.
					object record = default(T);
					bool recordCreated = false;
					int columnIndex = 0;
					bool inQuotes = false;
					char lastChar = (char)0;
					++lineNumber;
					while (true)
					{
						int nextChar = reader.Read();
						if (nextChar < 0)
							break;
						char c = (char)nextChar;
						switch (c)
						{
							case '"':
								if (lastChar != '"' && builder.Length == 0)
									inQuotes = true;
								else if (inQuotes)
								{
									// Check the next character
									nextChar = reader.Peek();
									if (nextChar < 0)
									{
										inQuotes = false;
										continue;
									}
									char c2 = (char)nextChar;
									if (c2 == '"')
									{
										// Escaping a quote, so consume the character
										reader.Read();
										builder.Append(c2);
									}
									else if (c2 == ',' || c2 == '\r' || c2 == '\n')
										// End of field
										inQuotes = false;
									else
										throw new CsvParseException("Unexpected quote marks in data at line " + lineNumber);
								}
								else
									throw new CsvParseException("Unexpected quote marks in data at line " + lineNumber);
								break;
							case '\r':
								// Ignore carriage-return if the next character is a newline
								nextChar = reader.Peek();
								if (nextChar < 0)
									continue;
								// If the next character is not a newline, treat this carriage return as the newline
								if ((char)nextChar != '\n')
									goto case '\n';
								break;
							case '\n':
								if (inQuotes)
								{
									// Line break in a quoted field means add the newline to the field and continue on the next line
									builder.Append(c);
								}
								else
								{
									// Ignore any blank lines
									if (!(columnIndex == 0 && builder.Length == 0))
									{
										// End of record, so add the last column and append it to the list
										// Is there a field mapped to this column?
										if (columnIndex != headers.Length - 1)
											throw new CsvParseException("Incorrect number of fields on line " + lineNumber);
										TrySetValue<T>(headers[columnIndex], builder, memberMap, ref record, ref recordCreated);
										builder.Length = 0;

										result.Add((T)record);
										columnIndex = 0;
										recordCreated = false;
									}
								}
								++lineNumber;
								break;
							case ',':
								if (inQuotes)
									builder.Append(c);
								else
								{
									// Is there a field mapped to this column?
									if (columnIndex >= headers.Length - 1)
										throw new CsvParseException("Too many fields on line " + lineNumber);
									TrySetValue<T>(headers[columnIndex], builder, memberMap, ref record, ref recordCreated);
									builder.Length = 0;
									++columnIndex;
								}
								break;
							default:
								builder.Append(c);
								break;
						}
						lastChar = c;
					}

					// Was there a field being built up?
					if (builder.Length > 0 || lastChar == ',')
					{
						// Is there a field mapped to this column?
						if (columnIndex != headers.Length - 1)
							throw new CsvParseException("Incorrect number of fields on line " + lineNumber);
						TrySetValue<T>(headers[columnIndex], builder, memberMap, ref record, ref recordCreated);
						builder.Length = 0;

						if (recordCreated)
							result.Add((T)record);
					}
				}

				return result;
			}
		}

		static bool TrySetValue<T>(string header, StringBuilder builder, Dictionary<string, MemberInfo> memberMap, ref object record, ref bool recordCreated) where T : new()
		{
			bool result = false;
            if (memberMap.TryGetValue(header, out MemberInfo memberInfo))
            {
                if (!recordCreated)
                {
                    record = new T();
                    recordCreated = true;
                }
                switch (memberInfo.MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var fieldInfo = (FieldInfo)memberInfo;
                            if (fieldInfo.FieldType.IsEnum)
                                fieldInfo.SetValue(record, Enum.Parse(fieldInfo.FieldType, builder.ToString()));
                            else
                                fieldInfo.SetValue(record, Convert.ChangeType(builder.ToString(), fieldInfo.FieldType, CultureInfo.InvariantCulture));
                            result = true;
                        }
                        break;
                    case MemberTypes.Property:
                        {
                            var propertyInfo = (PropertyInfo)memberInfo;
                            if (propertyInfo.PropertyType.IsEnum)
                                propertyInfo.SetValue(record, Enum.Parse(propertyInfo.PropertyType, builder.ToString()), null);
                            else
                                propertyInfo.SetValue(record, Convert.ChangeType(builder.ToString(), propertyInfo.PropertyType, CultureInfo.InvariantCulture), null);
                            result = true;
                        }
                        break;
                }
            }
            return result;
		}
	}
}
