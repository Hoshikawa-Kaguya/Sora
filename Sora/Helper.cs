using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Sora
{
    public static class Helper
    {
        public static T CreateInstance<T>()
        {
            return (T) FormatterServices.GetUninitializedObject(typeof(T));
        }

        public static object CreateInstance(this Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }

        public static T Convert<T>(this string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T) converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static object Convert(this string input, Type type)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                return converter.ConvertFromString(input);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}