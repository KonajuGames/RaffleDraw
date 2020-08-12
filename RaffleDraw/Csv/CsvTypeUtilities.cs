using System;
using System.Collections.Generic;
using System.Reflection;

namespace Konaju.File.Csv
{
	static class CsvTypeUtilities
	{
		internal static void InterrogateType(Type type, Dictionary<string, MemberInfo> memberMap, CsvInterrogateFlags flags, List<string> headers = null)
		{
			var columnAttributeType = typeof(CsvColumnAttribute);
			var serializeAttributeType = typeof(CsvSerializeAttribute);
			var ignoreAttributeType = typeof(CsvIgnoreAttribute);

			// Find which fields in the type we should process
			var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			var count = members.Length;
			for (int i = 0; i < count; ++i)
			{
				var member = members[i];
				var columnName = member.Name;
				// Does the field have a CsvColumn attribute overriding the field name as the column name?
				var attrs = member.GetCustomAttributes(columnAttributeType, true);
				if (attrs.Length > 0)
				{
					columnName = ((CsvColumnAttribute)attrs[0]).ColumnName;
				}
				var isPublic = false;
				var isAccessible = false;
				var skip = false;
				switch (member.MemberType)
				{
					case MemberTypes.Field:
						isPublic = isAccessible = ((FieldInfo)member).IsPublic;
						break;
					case MemberTypes.Property:
						{
							var propertyInfo = (PropertyInfo)member;
							// We don't handle indexed properties
							if (propertyInfo.GetIndexParameters().Length == 0)
							{
								// If we are writing the CSV, the property must have a get method
								if (flags == CsvInterrogateFlags.Write)
								{
									var getter = propertyInfo.GetGetMethod();
									isAccessible = getter != null;
									isPublic = isAccessible && getter.IsPublic;
								}
								// If we are reading the CSV, the property must have a set method
								else if (flags == CsvInterrogateFlags.Read)
								{
									var setter = propertyInfo.GetSetMethod();
									isAccessible = setter != null;
									isPublic = isAccessible && setter.IsPublic;
								}
							}
						}
						break;
					default:
						columnName = string.Empty;
						skip = true;
						break;
				}
                if (skip)
                {
                    continue;
                }

				if (isPublic)
				{
					// Should we ignore this public member?
					attrs = member.GetCustomAttributes(ignoreAttributeType, true);
					if (attrs.Length > 0)
						columnName = string.Empty;
				}
				else
				{
					// Not public, so unless it has the CsvSerialize attribute we should ignore it
					// If it is a property and does not have the corresponding get or set method, we can't serialize at all
					attrs = member.GetCustomAttributes(serializeAttributeType, true);
					if (!isAccessible && attrs.Length == 0)
						columnName = string.Empty;
				}

				// We have a valid column name, so add it to the collections
				if (!string.IsNullOrEmpty(columnName))
				{
					if (headers != null)
					{
						headers.Add(columnName);
					}
					memberMap[columnName] = member;
				}
			}
		}
	}
}
