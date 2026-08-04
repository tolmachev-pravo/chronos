using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Chronos.Application.Common.Extensions
{
    public static class EnumExtensions
    {
        public static string ToEnumString<T>(this T type)
            where T : Enum
        {
            var enumType = typeof(T);
            var name = Enum.GetName(enumType, type);
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
            return enumMemberAttribute.Value;
        }
    }
}
