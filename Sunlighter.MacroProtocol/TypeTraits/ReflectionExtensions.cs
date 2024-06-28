using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Sunlighter.MacroProtocol.AssertNotNull;

namespace Sunlighter.MacroProtocol.TypeTraits
{
    public static class ReflectionExtensions
    {
        public static MethodInfo GetRequiredMethod(this Type t, string name, BindingFlags flags, Type[] parameterTypes)
        {
#if NETSTANDARD2_0
            MethodInfo m = t.GetMethod(name, flags, Type.DefaultBinder, parameterTypes, null);
#else
            MethodInfo? m = t.GetMethod(name, flags, Type.DefaultBinder, parameterTypes, null);
#endif
            if (m is null) throw new Exception($"Method {GetTypeName(t)}.{name}({string.Join(", ", parameterTypes.Select(t2 => GetTypeName(t2)))}) not found");
            return m;
        }

        public static ConstructorInfo GetRequiredConstructor(this Type t, Type[] parameterTypes)
        {
#if NETSTANDARD2_0
            ConstructorInfo c = t.GetConstructor(parameterTypes);
#else
            ConstructorInfo? c = t.GetConstructor(parameterTypes);
#endif
            if (c is null) throw new Exception($"Constructor {GetTypeName(t)}({string.Join(", ", parameterTypes.Select(t2 => GetTypeName(t2)))}) not found");
            return c;
        }

        public static PropertyInfo GetRequiredProperty(this Type t, string name, BindingFlags flags)
        {
#if NETSTANDARD2_0
            PropertyInfo p = t.GetProperty(name, flags);
#else
            PropertyInfo? p = t.GetProperty(name, flags);
#endif
            if (p is null) throw new Exception($"Property {GetTypeName(t)}.{name} not found");
            return p;
        }

        public static PropertyInfo GetRequiredProperty(this Type t, string name, BindingFlags flags, Type propertyType, Type[] parameterTypes)
        {
#if NETSTANDARD2_0
            PropertyInfo p = t.GetProperty(name, flags, Type.DefaultBinder, propertyType, parameterTypes, null);
#else
            PropertyInfo? p = t.GetProperty(name, flags, Type.DefaultBinder, propertyType, parameterTypes, null);
#endif
            if (p is null) throw new Exception($"Property {GetTypeName(t)}.{name}, of type {GetTypeName(propertyType)}, with arguments ({string.Join(", ", parameterTypes.Select(GetTypeName))}) not found");
            return p;
        }

        public static FieldInfo GetRequiredField(this Type t, string name, BindingFlags flags)
        {
#if NETSTANDARD2_0
            FieldInfo f = t.GetField(name, flags);
#else
            FieldInfo? f = t.GetField(name, flags);
#endif
            if (f is null) throw new Exception($"Field {GetTypeName(t)}.{name} not found");
            return f;
        }

        private static readonly Lazy<ImmutableSortedDictionary<Type, string>> commonTypes =
            new Lazy<ImmutableSortedDictionary<Type, string>>(GetCommonTypes, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ImmutableSortedDictionary<Type, string> GetCommonTypes()
        {
            return ImmutableSortedDictionary<Type, string>.Empty
                .WithComparers(TypeTypeTraits.Adapter)
                .Add(typeof(bool), "bool")
                .Add(typeof(byte), "byte")
                .Add(typeof(short), "short")
                .Add(typeof(int), "int")
                .Add(typeof(long), "long")
                .Add(typeof(sbyte), "sbyte")
                .Add(typeof(ushort), "ushort")
                .Add(typeof(uint), "uint")
                .Add(typeof(ulong), "ulong")
                .Add(typeof(char), "char")
                .Add(typeof(string), "string")
                .Add(typeof(float), "float")
                .Add(typeof(double), "double")
                .Add(typeof(decimal), "decimal")
                .Add(typeof(BigInteger), "BigInteger")
                .Add(typeof(ImmutableList<>), "ImmutableList")
                .Add(typeof(ImmutableSortedSet<>), "ImmutableSortedSet")
                .Add(typeof(ImmutableSortedDictionary<,>), "ImmutableSortedDictionary");
        }

        private static readonly Lazy<Regex> backTick = new Lazy<Regex>(() => new Regex("`[1-9][0-9]*", RegexOptions.None), LazyThreadSafetyMode.ExecutionAndPublication);

        internal static Regex BackTickRegex => backTick.Value;

        private static readonly ConditionalWeakTable<Type, string> typeNameTable = new ConditionalWeakTable<Type, string>();

        public static string GetTypeName(Type t)
        {
            return typeNameTable.GetValue(t, t2 => GetTypeNameInternal(t2));
        }

        private static string GetTypeNameInternal(Type t)
        {
            if (t.IsGenericType)
            {
                if (t.IsGenericTypeDefinition)
                {
                    if (commonTypes.Value.ContainsKey(t))
                    {
                        return commonTypes.Value[t];
                    }
                    else
                    {
                        return t.FullNameNotNull().UpTo(backTick.Value);
                    }
                }
                else
                {
                    Type gtd = t.GetGenericTypeDefinition();
                    string canonicalName;
                    if (commonTypes.Value.ContainsKey(gtd))
                    {
                        canonicalName = commonTypes.Value[gtd];
                    }
                    else
                    {
                        canonicalName = gtd.FullNameNotNull().UpTo(backTick.Value);
                    }

                    return $"{canonicalName}<{string.Join(",", t.GetGenericArguments().Select(t2 => GetTypeName(t2)))}>";
                }
            }
            else if (t.IsArray)
            {
                return GetTypeName(t.GetElementType().AssertNotNull()) + "[]";
            }
            else
            {
                if (commonTypes.Value.ContainsKey(t))
                {
                    return commonTypes.Value[t];
                }
                else
                {
                    return t.FullNameNotNull();
                }
            }
        }
    }

    public static partial class Extensions
    {
        public static string UpTo(this string str, Regex r)
        {
            Match m = r.Match(str);
            if (m.Success)
            {
                return str.Substring(0, m.Index);
            }
            else
            {
                return str;
            }
        }
    }
}
