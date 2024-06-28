﻿using Sunlighter.MacroProtocol.AssertNotNull;
using Sunlighter.OptionLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Sunlighter.MacroProtocol.TypeTraits
{
    public interface ITypeTraits<T>
    {
        int Compare(T a, T b);
        void AddToHash(HashBuilder b, T a);
        bool CanSerialize(T a);
        void Serialize(BinaryWriter dest, T a);
        T Deserialize(BinaryReader src);
        long MeasureBytes(T a);
    }

    public static partial class Extensions
    {
        public static byte[] SerializeToBytes<T>(this ITypeTraits<T> worker, T a)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter sw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    worker.Serialize(sw, a);
                    return ms.ToArray();
                }
            }
        }

        public static T DeserializeFromBytes<T>(this ITypeTraits<T> worker, byte[] b)
        {
            using (MemoryStream ms = new MemoryStream(b))
            {
                using (BinaryReader sr = new BinaryReader(ms, Encoding.UTF8))
                {
                    return worker.Deserialize(sr);
                }
            }
        }

        public static void SerializeToFile<T>(this ITypeTraits<T> worker, string filePath, T a)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath).AssertNotNull());
            }

            using (FileStream fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 18, FileOptions.None))
            {
                using (BinaryWriter sw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    worker.Serialize(sw, a);
                }
            }
        }

        public static T DeserializeFromFile<T>(this ITypeTraits<T> worker, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 18, FileOptions.None))
            {
                using (BinaryReader sr = new BinaryReader(fs, Encoding.UTF8))
                {
                    return worker.Deserialize(sr);
                }
            }
        }

        public static T LoadOrGenerate<T>(this ITypeTraits<T> worker, string filePath, Func<T> generate)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    return worker.DeserializeFromFile(filePath);
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not read {filePath}: {exc.GetType().FullName}: {exc.Message}");
                }
            }

            T result = generate();
            worker.SerializeToFile(filePath, result);
            return result;
        }

        public static int GetBasicHashCode<T>(this ITypeTraits<T> worker, T a)
        {
            BasicHashBuilder hb = new BasicHashBuilder();
            worker.AddToHash(hb, a);
            return hb.Result;
        }

        public static byte[] GetSHA256Hash<T>(this ITypeTraits<T> worker, T a)
        {
            using (SHA256HashBuilder hb = new SHA256HashBuilder())
            {
                worker.AddToHash(hb, a);
                return hb.Result;
            }
        }
    }

#if NETSTANDARD2_0
    public abstract class Adapter<T> : IEqualityComparer<T>, IComparer<T>
    {
        protected readonly ITypeTraits<T> itemTraits;

        protected Adapter(ITypeTraits<T> itemTraits)
        {
            this.itemTraits = itemTraits;
        }

        public ITypeTraits<T> TypeTraits => itemTraits;

        public abstract int Compare(T x, T y);

        public abstract bool Equals(T x, T y);

        public int GetHashCode(T obj)
        {
            return itemTraits.GetBasicHashCode(obj);
        }

        public static Adapter<T> Create(ITypeTraits<T> itemTraits)
        {
            if (typeof(T).IsClass)
            {
                Type ac = typeof(AdapterForClass<>).MakeGenericType(typeof(T));
                ConstructorInfo ci = ac.GetRequiredConstructor(new Type[] { typeof(ITypeTraits<>).MakeGenericType(typeof(T)) });
                return (Adapter<T>)ci.Invoke(new object[] { itemTraits });
            }
            else
            {
                Type ac = typeof(AdapterForStruct<>).MakeGenericType(typeof(T));
                ConstructorInfo ci = ac.GetRequiredConstructor(new Type[] { typeof(ITypeTraits<>).MakeGenericType(typeof(T)) });
                return (Adapter<T>)ci.Invoke(new object[] { itemTraits });
            }
        }
    }

    public sealed class AdapterForClass<T> : Adapter<T>
        where T : class
    {
        public AdapterForClass(ITypeTraits<T> itemTraits)
            : base(itemTraits)
        {

        }

        public override int Compare(T x, T y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return itemTraits.Compare(x, y);
        }

        public override bool Equals(T x, T y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;

            return itemTraits.Compare(x, y) == 0;
        }
    }

    public sealed class AdapterForStruct<T> : Adapter<T>
        where T : struct
    {
        public AdapterForStruct(ITypeTraits<T> itemTraits)
            : base(itemTraits)
        {

        }

        public override int Compare(T x, T y)
        {
            return itemTraits.Compare(x, y);
        }

        public override bool Equals(T x, T y)
        {
            return itemTraits.Compare(x, y) == 0;
        }
    }

#else
    public sealed class Adapter<T> : IEqualityComparer<T>, IComparer<T>
    {
        private readonly ITypeTraits<T> itemTraits;

        private Adapter(ITypeTraits<T> itemTraits)
        {
            this.itemTraits = itemTraits;
        }

        public ITypeTraits<T> TypeTraits => itemTraits;

        public int Compare(T? x, T? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return itemTraits.Compare(x, y);
        }

        public bool Equals(T? x, T? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return itemTraits.Compare(x, y) == 0;
        }

        public int GetHashCode(T obj)
        {
            return itemTraits.GetBasicHashCode(obj);
        }

        public static Adapter<T> Create(ITypeTraits<T> itemTraits)
        {
            return new Adapter<T>(itemTraits);
        }
    }
#endif

    public sealed class StringTypeTraits : ITypeTraits<string>
    {
        private StringTypeTraits() { }

        public static StringTypeTraits Value { get; } = new StringTypeTraits();

        public int Compare(string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal);
        }

        public void AddToHash(HashBuilder b, string a)
        {
            b.Add(HashToken.String);
            b.Add(Encoding.UTF8.GetBytes(a));
        }

        public bool CanSerialize(string a) => true;

        public void Serialize(BinaryWriter dest, string a)
        {
            dest.Write(a);
        }

        public string Deserialize(BinaryReader src)
        {
            return src.ReadString();
        }

        public long MeasureBytes(string a)
        {
            int len = Encoding.UTF8.GetByteCount(a);
            int lenlen =
                (len < (1 << 7)) ? 1 :
                (len < (1 << 14)) ? 2 :
                (len < (1 << 21)) ? 3 :
                (len < (1 << 28)) ? 4 : 5;

            return (long)lenlen + len;
        }
    }

    public sealed class Int32TypeTraits : ITypeTraits<int>
    {
        private Int32TypeTraits() { }

        public static Int32TypeTraits Value { get; } = new Int32TypeTraits();

        public int Compare(int a, int b)
        {
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
        }

        public void AddToHash(HashBuilder b, int a)
        {
            b.Add(HashToken.Int32);
            b.Add(BitConverter.GetBytes(a));
        }

        public bool CanSerialize(int a) => true;

        public void Serialize(BinaryWriter dest, int a)
        {
            dest.Write(a);
        }

        public int Deserialize(BinaryReader src)
        {
            return src.ReadInt32();
        }

        public long MeasureBytes(int a)
        {
            return 4L;
        }
    }

    public sealed class BooleanTypeTraits : ITypeTraits<bool>
    {
        private BooleanTypeTraits() { }

        public static BooleanTypeTraits Value { get; } = new BooleanTypeTraits();

        public int Compare(bool a, bool b)
        {
            if (a == b) return 0;
            if (!a) return -1;
            return 1;
        }

        public void AddToHash(HashBuilder b, bool a)
        {
            b.Add(HashToken.Boolean);
            b.Add(a ? (byte)1 : (byte)0);
        }

        public bool CanSerialize(bool a) => true;

        public void Serialize(BinaryWriter dest, bool item)
        {
            dest.Write(item);
        }

        public bool Deserialize(BinaryReader src)
        {
            return src.ReadBoolean();
        }

        public long MeasureBytes(bool a)
        {
            return 1L;
        }
    }

    public sealed class UnitTypeTraits<T> : ITypeTraits<T>
    {
        private readonly uint hashToken;
        private readonly T value;

        public UnitTypeTraits(uint hashToken, T value)
        {
            this.hashToken = hashToken;
            this.value = value;
        }

        public int Compare(T a, T b)
        {
            return 0;
        }

        public void AddToHash(HashBuilder b, T a)
        {
            b.Add(hashToken);
        }

        public bool CanSerialize(T a) => true;

        public void Serialize(BinaryWriter dest, T a)
        {
            // do nothing
        }

        public T Deserialize(BinaryReader src)
        {
            return value;
        }

        public long MeasureBytes(T a)
        {
            return 0L;
        }
    }

    public sealed class TupleTypeTraits<T, U> : ITypeTraits<Tuple<T, U>>
    {
        private readonly ITypeTraits<T> item1Traits;
        private readonly ITypeTraits<U> item2Traits;

        public TupleTypeTraits(ITypeTraits<T> item1Traits, ITypeTraits<U> item2Traits)
        {
            this.item1Traits = item1Traits;
            this.item2Traits = item2Traits;
        }

        public int Compare(Tuple<T, U> a, Tuple<T, U> b)
        {
            int r = item1Traits.Compare(a.Item1, b.Item1);
            if (r != 0) return r;
            return item2Traits.Compare(a.Item2, b.Item2);
        }

        public void AddToHash(HashBuilder b, Tuple<T, U> a)
        {
            b.Add(HashToken.Tuple2);
            item1Traits.AddToHash(b, a.Item1);
            item2Traits.AddToHash(b, a.Item2);
        }

        public bool CanSerialize(Tuple<T, U> a) => item1Traits.CanSerialize(a.Item1) && item2Traits.CanSerialize(a.Item2);

        public void Serialize(BinaryWriter dest, Tuple<T, U> item)
        {
            item1Traits.Serialize(dest, item.Item1);
            item2Traits.Serialize(dest, item.Item2);
        }

        public Tuple<T, U> Deserialize(BinaryReader src)
        {
            T item1 = item1Traits.Deserialize(src);
            U item2 = item2Traits.Deserialize(src);
            return new Tuple<T, U>(item1, item2);
        }

        public long MeasureBytes(Tuple<T, U> a)
        {
            return item1Traits.MeasureBytes(a.Item1) + item2Traits.MeasureBytes(a.Item2);
        }
    }

    public sealed class ValueTupleTypeTraits<T, U> : ITypeTraits<(T, U)>
    {
        private readonly ITypeTraits<T> item1Traits;
        private readonly ITypeTraits<U> item2Traits;

        public ValueTupleTypeTraits(ITypeTraits<T> item1Traits, ITypeTraits<U> item2Traits)
        {
            this.item1Traits = item1Traits;
            this.item2Traits = item2Traits;
        }

        public int Compare((T, U) a, (T, U) b)
        {
            int r = item1Traits.Compare(a.Item1, b.Item1);
            if (r != 0) return r;
            return item2Traits.Compare(a.Item2, b.Item2);
        }

        public void AddToHash(HashBuilder b, (T, U) a)
        {
            b.Add(HashToken.Tuple2);
            item1Traits.AddToHash(b, a.Item1);
            item2Traits.AddToHash(b, a.Item2);
        }

        public bool CanSerialize((T, U) a) => item1Traits.CanSerialize(a.Item1) && item2Traits.CanSerialize(a.Item2);

        public void Serialize(BinaryWriter dest, (T, U) item)
        {
            item1Traits.Serialize(dest, item.Item1);
            item2Traits.Serialize(dest, item.Item2);
        }

        public (T, U) Deserialize(BinaryReader src)
        {
            T item1 = item1Traits.Deserialize(src);
            U item2 = item2Traits.Deserialize(src);
            return (item1, item2);
        }

        public long MeasureBytes((T, U) a)
        {
            return item1Traits.MeasureBytes(a.Item1) + item2Traits.MeasureBytes(a.Item2);
        }
    }

    public sealed class TupleTypeTraits<T, U, V> : ITypeTraits<Tuple<T, U, V>>
    {
        private readonly ITypeTraits<T> item1Traits;
        private readonly ITypeTraits<U> item2Traits;
        private readonly ITypeTraits<V> item3Traits;

        public TupleTypeTraits(ITypeTraits<T> item1Traits, ITypeTraits<U> item2Traits, ITypeTraits<V> item3Traits)
        {
            this.item1Traits = item1Traits;
            this.item2Traits = item2Traits;
            this.item3Traits = item3Traits;
        }

        public int Compare(Tuple<T, U, V> a, Tuple<T, U, V> b)
        {
            int r = item1Traits.Compare(a.Item1, b.Item1);
            if (r != 0) return r;
            r = item2Traits.Compare(a.Item2, b.Item2);
            if (r != 0) return r;
            return item3Traits.Compare(a.Item3, b.Item3);
        }

        public void AddToHash(HashBuilder b, Tuple<T, U, V> a)
        {
            b.Add(HashToken.Tuple3);
            item1Traits.AddToHash(b, a.Item1);
            item2Traits.AddToHash(b, a.Item2);
            item3Traits.AddToHash(b, a.Item3);
        }

        public bool CanSerialize(Tuple<T, U, V> a) =>
            item1Traits.CanSerialize(a.Item1) &&
            item2Traits.CanSerialize(a.Item2) &&
            item3Traits.CanSerialize(a.Item3);

        public void Serialize(BinaryWriter dest, Tuple<T, U, V> item)
        {
            item1Traits.Serialize(dest, item.Item1);
            item2Traits.Serialize(dest, item.Item2);
            item3Traits.Serialize(dest, item.Item3);
        }

        public Tuple<T, U, V> Deserialize(BinaryReader src)
        {
            T item1 = item1Traits.Deserialize(src);
            U item2 = item2Traits.Deserialize(src);
            V item3 = item3Traits.Deserialize(src);
            return new Tuple<T, U, V>(item1, item2, item3);
        }

        public long MeasureBytes(Tuple<T, U, V> a)
        {
            return item1Traits.MeasureBytes(a.Item1) + item2Traits.MeasureBytes(a.Item2) + item3Traits.MeasureBytes(a.Item3);
        }
    }

    public sealed class ValueTupleTypeTraits<T, U, V> : ITypeTraits<(T, U, V)>
    {
        private readonly ITypeTraits<T> item1Traits;
        private readonly ITypeTraits<U> item2Traits;
        private readonly ITypeTraits<V> item3Traits;

        public ValueTupleTypeTraits(ITypeTraits<T> item1Traits, ITypeTraits<U> item2Traits, ITypeTraits<V> item3Traits)
        {
            this.item1Traits = item1Traits;
            this.item2Traits = item2Traits;
            this.item3Traits = item3Traits;
        }

        public int Compare((T, U, V) a, (T, U, V) b)
        {
            int r = item1Traits.Compare(a.Item1, b.Item1);
            if (r != 0) return r;
            r = item2Traits.Compare(a.Item2, b.Item2);
            if (r != 0) return r;
            return item3Traits.Compare(a.Item3, b.Item3);
        }

        public void AddToHash(HashBuilder b, (T, U, V) a)
        {
            b.Add(HashToken.Tuple3);
            item1Traits.AddToHash(b, a.Item1);
            item2Traits.AddToHash(b, a.Item2);
            item3Traits.AddToHash(b, a.Item3);
        }

        public bool CanSerialize((T, U, V) a) =>
            item1Traits.CanSerialize(a.Item1) &&
            item2Traits.CanSerialize(a.Item2) &&
            item3Traits.CanSerialize(a.Item3);

        public void Serialize(BinaryWriter dest, (T, U, V) item)
        {
            item1Traits.Serialize(dest, item.Item1);
            item2Traits.Serialize(dest, item.Item2);
            item3Traits.Serialize(dest, item.Item3);
        }

        public (T, U, V) Deserialize(BinaryReader src)
        {
            T item1 = item1Traits.Deserialize(src);
            U item2 = item2Traits.Deserialize(src);
            V item3 = item3Traits.Deserialize(src);
            return (item1, item2, item3);
        }

        public long MeasureBytes((T, U, V) a)
        {
            return item1Traits.MeasureBytes(a.Item1) + item2Traits.MeasureBytes(a.Item2) + item3Traits.MeasureBytes(a.Item3);
        }
    }

    public sealed class OptionTypeTraits<T> : ITypeTraits<Option<T>>
    {
        private readonly ITypeTraits<T> itemTraits;

        public OptionTypeTraits(ITypeTraits<T> itemTraits)
        {
            this.itemTraits = itemTraits;
        }

        public int Compare(Option<T> a, Option<T> b)
        {
            if (!a.HasValue && !b.HasValue) return 0;
            if (!a.HasValue) return -1;
            if (!b.HasValue) return 1;
            return itemTraits.Compare(a.Value, b.Value);
        }

        public void AddToHash(HashBuilder b, Option<T> a)
        {
            if (a.HasValue)
            {
                b.Add((byte)1);
                itemTraits.AddToHash(b, a.Value);
            }
            else
            {
                b.Add((byte)0);
            }
        }

        public bool CanSerialize(Option<T> a) => !a.HasValue || itemTraits.CanSerialize(a.Value);

        public void Serialize(BinaryWriter dest, Option<T> a)
        {
            if (a.HasValue)
            {
                dest.Write(true);
                itemTraits.Serialize(dest, a.Value);
            }
            else
            {
                dest.Write(false);
            }
        }

        public Option<T> Deserialize(BinaryReader src)
        {
            bool hasValue = src.ReadBoolean();
            if (hasValue)
            {
                T value = itemTraits.Deserialize(src);
                return Option<T>.Some(value);
            }
            else
            {
                return Option<T>.None;
            }
        }

        public long MeasureBytes(Option<T> a)
        {
            if (a.HasValue)
            {
                return 1L + itemTraits.MeasureBytes(a.Value);
            }
            else
            {
                return 1L;
            }
        }
    }

    public sealed class ConvertTypeTraits<T, U> : ITypeTraits<T>
    {
        private readonly Func<T, U> convert;
        private readonly ITypeTraits<U> itemTraits;
        private readonly Func<U, T> convertBack;

        public ConvertTypeTraits(Func<T, U> convert, ITypeTraits<U> itemTraits, Func<U, T> convertBack)
        {
            this.convert = convert;
            this.itemTraits = itemTraits;
            this.convertBack = convertBack;
        }

        public int Compare(T a, T b)
        {
            return itemTraits.Compare(convert(a), convert(b));
        }

        public void AddToHash(HashBuilder b, T a)
        {
            itemTraits.AddToHash(b, convert(a));
        }

        public bool CanSerialize(T a) => itemTraits.CanSerialize(convert(a));

        public void Serialize(BinaryWriter dest, T a)
        {
            itemTraits.Serialize(dest, convert(a));
        }

        public T Deserialize(BinaryReader src)
        {
            return convertBack(itemTraits.Deserialize(src));
        }

        public long MeasureBytes(T a)
        {
            return itemTraits.MeasureBytes(convert(a));
        }
    }

    public sealed class GuardException : Exception
    {
        public GuardException(string message) : base(message) { }
    }

    public sealed class GuardedTypeTraits<T> : ITypeTraits<T>
    {
        private readonly Func<T, bool> isOk;
        private readonly ITypeTraits<T> itemTraits;

        public GuardedTypeTraits(Func<T, bool> isOk, ITypeTraits<T> itemTraits)
        {
            this.isOk = isOk;
            this.itemTraits = itemTraits;
        }

        public int Compare(T a, T b)
        {
            if (isOk(a) && isOk(b))
            {
                return itemTraits.Compare(a, b);
            }
            else
            {
                throw new GuardException($"Guard failed for {typeof(T).FullName}");
            }
        }

        public void AddToHash(HashBuilder b, T a)
        {
            if (isOk(a))
            {
                itemTraits.AddToHash(b, a);
            }
            else
            {
                throw new GuardException($"Guard failed for {typeof(T).FullName}");
            }
        }

        public bool CanSerialize(T a) => isOk(a) && itemTraits.CanSerialize(a);

        public void Serialize(BinaryWriter dest, T a)
        {
            if (isOk(a))
            {
                itemTraits.Serialize(dest, a);
            }
            else
            {
                throw new GuardException($"Guard failed for {typeof(T).FullName}");
            }
        }

        public T Deserialize(BinaryReader src)
        {
            return itemTraits.Deserialize(src);
        }

        public long MeasureBytes(T a)
        {
            if (isOk(a))
            {
                return itemTraits.MeasureBytes(a);
            }
            else
            {
                throw new GuardException($"Guard failed for {typeof(T).FullName}");
            }
        }
    }

    public sealed class RecursiveTypeTraits<T> : ITypeTraits<T>
    {
#if NETSTANDARD2_0
        private ITypeTraits<T> itemTraits;
#else
        private ITypeTraits<T>? itemTraits;
#endif

        public RecursiveTypeTraits()
        {
            itemTraits = null;
        }

        public void Set(ITypeTraits<T> itemTraits)
        {
            if (this.itemTraits == null)
            {
                this.itemTraits = itemTraits;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} already set");
            }
        }

        public int Compare(T a, T b)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                return itemTraits.Compare(a, b);
            }
        }

        public void AddToHash(HashBuilder b, T a)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                itemTraits.AddToHash(b, a);
            }
        }

        public bool CanSerialize(T a)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                return itemTraits.CanSerialize(a);
            }
        }

        public void Serialize(BinaryWriter dest, T a)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                itemTraits.Serialize(dest, a);
            }
        }

        public T Deserialize(BinaryReader src)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                return itemTraits.Deserialize(src);
            }
        }

        public long MeasureBytes(T a)
        {
            if (itemTraits == null)
            {
                throw new InvalidOperationException($"{nameof(RecursiveTypeTraits<T>)} not set");
            }
            else
            {
                return itemTraits.MeasureBytes(a);
            }
        }
    }

    public interface IUnionCaseTypeTraits<T>
    {
        bool CanConvert(T a);
        ITypeTraits<T> Traits { get; }
        string Name { get; }
    }

    public sealed class UnionCaseTypeTraits<T, U> : IUnionCaseTypeTraits<T>
    {
        private readonly Func<T, bool> canConvert;

        public UnionCaseTypeTraits(string name, Func<T, bool> canConvert, Func<T, U> convert, ITypeTraits<U> itemTraits, Func<U, T> convertBack)
        {
            this.canConvert = canConvert;
            Traits = new ConvertTypeTraits<T, U>(convert, itemTraits, convertBack);
            Name = name;
        }

        public bool CanConvert(T a)
        {
            return canConvert(a);
        }

        public ITypeTraits<T> Traits { get; }

        public string Name { get; }
    }

    public sealed class UnionCaseTypeTraits2<T, U> : IUnionCaseTypeTraits<T> where U : T
    {
        public UnionCaseTypeTraits2(string name, ITypeTraits<U> itemTraits)
        {
            Traits = new ConvertTypeTraits<T, U>
            (
                t =>
                {
                    if (t is U u)
                    {
                        return u;
                    }
                    else
                    {
                        throw new InvalidCastException($"{typeof(T).FullName} => {typeof(U).FullName} failed");
                    }
                },
                itemTraits,
                u => u
            );
            Name = name;
        }

        public bool CanConvert(T a)
        {
            return (a is U);
        }

        public ITypeTraits<T> Traits { get; }

        public string Name { get; }
    }

    public sealed class UnionTypeTraits<T> : ITypeTraits<T>
    {
        private readonly ImmutableList<IUnionCaseTypeTraits<T>> cases;
        private readonly ImmutableDictionary<string, int> caseIndexFromName;

        public UnionTypeTraits(ImmutableList<IUnionCaseTypeTraits<T>> cases)
        {
            this.cases = cases;
            ImmutableDictionary<string, int>.Builder index = ImmutableDictionary<string, int>.Empty.ToBuilder();
            foreach (int i in Enumerable.Range(0, cases.Count))
            {
                if (index.ContainsKey(cases[i].Name))
                {
                    throw new InvalidOperationException($"Duplicate name {cases[i].Name}");
                }
                else
                {
                    index.Add(cases[i].Name, i);
                }
            }
            caseIndexFromName = index.ToImmutable();
        }

        private int GetCase(T a)
        {
            int i = 0;
            int iEnd = cases.Count;
            while (i < iEnd)
            {
                if (cases[i].CanConvert(a)) return i;
                ++i;
            }
            return -1;
        }

        public int Compare(T a, T b)
        {
            int ca = GetCase(a);
            int cb = GetCase(b);

            if (ca < 0 || cb < 0) throw new InvalidOperationException("Unrecognized case");

            if (ca < cb) return -1;
            if (ca > cb) return 1;

            return cases[ca].Traits.Compare(a, b);
        }

        public void AddToHash(HashBuilder b, T a)
        {
            int ca = GetCase(a);

            if (ca < 0) throw new InvalidOperationException("Unrecognized case");

            b.Add(HashToken.Union);
            b.Add(ca);
            cases[ca].Traits.AddToHash(b, a);
        }

        public bool CanSerialize(T a)
        {
            int ca = GetCase(a);
            return ca >= 0 && cases[ca].Traits.CanSerialize(a);
        }

        public void Serialize(BinaryWriter dest, T a)
        {
            int ca = GetCase(a);
            if (ca < 0) throw new InvalidOperationException("Unrecognized case");

            dest.Write(cases[ca].Name);
            cases[ca].Traits.Serialize(dest, a);
        }

        public T Deserialize(BinaryReader src)
        {
            string caseName = src.ReadString();
            if (caseIndexFromName.ContainsKey(caseName))
            {
                int ca = caseIndexFromName[caseName];
                return cases[ca].Traits.Deserialize(src);
            }
            else
            {
                throw new InvalidOperationException($"Unrecognized case {caseName} in file");
            }
        }

        public long MeasureBytes(T a)
        {
            int ca = GetCase(a);
            if (ca < 0) throw new InvalidOperationException("Unrecognized case");

            return StringTypeTraits.Value.MeasureBytes(cases[ca].Name) + cases[ca].Traits.MeasureBytes(a);
        }
    }

    public sealed class ListTypeTraits<T> : ITypeTraits<ImmutableList<T>>
    {
        private readonly ITypeTraits<T> itemTraits;

        public ListTypeTraits(ITypeTraits<T> itemTraits)
        {
            this.itemTraits = itemTraits;
        }

        public int Compare(ImmutableList<T> a, ImmutableList<T> b)
        {
            int i = 0;
            while (true)
            {
                if (i == a.Count && i == b.Count) return 0;
                if (i == a.Count) return -1;
                if (i == b.Count) return 1;

                int r = itemTraits.Compare(a[i], b[i]);
                if (r != 0) return r;

                ++i;
            }
        }

        public void AddToHash(HashBuilder b, ImmutableList<T> a)
        {
            b.Add(HashToken.List);
            b.Add(a.Count);
            foreach (T item in a)
            {
                itemTraits.AddToHash(b, item);
            }
        }

        public bool CanSerialize(ImmutableList<T> a) => a.All(i => itemTraits.CanSerialize(i));

        public void Serialize(BinaryWriter dest, ImmutableList<T> a)
        {
            dest.Write(a.Count);
            foreach (T item in a)
            {
                itemTraits.Serialize(dest, item);
            }
        }

        public ImmutableList<T> Deserialize(BinaryReader src)
        {
            int count = src.ReadInt32();
            ImmutableList<T>.Builder result = ImmutableList<T>.Empty.ToBuilder();
            for (int i = 0; i < count; ++i)
            {
                result.Add(itemTraits.Deserialize(src));
            }
            return result.ToImmutable();
        }

        public long MeasureBytes(ImmutableList<T> a)
        {
            return 4L + a.Select(i => itemTraits.MeasureBytes(i)).Sum();
        }
    }

    public sealed class SetTypeTraits<T> : ITypeTraits<ImmutableSortedSet<T>>
    {
        private readonly ImmutableSortedSet<T> empty;
        private readonly ITypeTraits<T> itemTraits;

        public SetTypeTraits(ImmutableSortedSet<T> empty, ITypeTraits<T> itemTraits)
        {
            this.empty = empty;
            this.itemTraits = itemTraits;
        }

        public SetTypeTraits(ITypeTraits<T> itemTraits)
            : this
            (
                ImmutableSortedSet<T>.Empty.WithComparer(Adapter<T>.Create(itemTraits)),
                itemTraits
            )
        {
            // empty
        }

        public ImmutableSortedSet<T> Empty => empty;

        public int Compare(ImmutableSortedSet<T> a, ImmutableSortedSet<T> b)
        {
            int i = 0;
            while (true)
            {
                if (i == a.Count && i == b.Count) return 0;
                if (i == a.Count) return -1;
                if (i == b.Count) return 1;

                int r = itemTraits.Compare(a[i], b[i]);
                if (r != 0) return r;

                ++i;
            }
        }

        public void AddToHash(HashBuilder b, ImmutableSortedSet<T> a)
        {
            b.Add(HashToken.Set);
            b.Add(a.Count);
            foreach (T item in a)
            {
                itemTraits.AddToHash(b, item);
            }
        }

        public bool CanSerialize(ImmutableSortedSet<T> a) => a.All(i => itemTraits.CanSerialize(i));

        public void Serialize(BinaryWriter dest, ImmutableSortedSet<T> a)
        {
            dest.Write(a.Count);
            foreach (T item in a)
            {
                itemTraits.Serialize(dest, item);
            }
        }

        public ImmutableSortedSet<T> Deserialize(BinaryReader src)
        {
            int count = src.ReadInt32();
            ImmutableSortedSet<T>.Builder result = empty.ToBuilder();
            for (int i = 0; i < count; ++i)
            {
                result.Add(itemTraits.Deserialize(src));
            }
            return result.ToImmutable();
        }

        public long MeasureBytes(ImmutableSortedSet<T> a)
        {
            return 4L + a.Select(i => itemTraits.MeasureBytes(i)).Sum();
        }
    }

    public sealed class DictionaryTypeTraits<K, V> : ITypeTraits<ImmutableSortedDictionary<K, V>>
#if !NETSTANDARD2_0
        where K : notnull
#endif
    {
        private readonly ImmutableSortedSet<K> emptySet;
        private readonly ImmutableSortedDictionary<K, V> emptyDict;
        private readonly ITypeTraits<K> keyTraits;
        private readonly ITypeTraits<V> valueTraits;

        public DictionaryTypeTraits
        (
            ImmutableSortedSet<K> emptySet,
            ImmutableSortedDictionary<K, V> emptyDict,
            ITypeTraits<K> keyTraits,
            ITypeTraits<V> valueTraits
        )
        {
            this.emptySet = emptySet;
            this.emptyDict = emptyDict;
            this.keyTraits = keyTraits;
            this.valueTraits = valueTraits;
        }

        public DictionaryTypeTraits
        (
            ITypeTraits<K> keyTraits,
            ITypeTraits<V> valueTraits
        )
            : this
            (
                ImmutableSortedSet<K>.Empty.WithComparer(Adapter<K>.Create(keyTraits)),
                ImmutableSortedDictionary<K, V>.Empty.WithComparers(Adapter<K>.Create(keyTraits), Adapter<V>.Create(valueTraits)),
                keyTraits,
                valueTraits
            )
        {
            // empty
        }

        public ImmutableSortedSet<K> EmptyKeySet => emptySet;

        public ImmutableSortedDictionary<K, V> Empty => emptyDict;

        public int Compare(ImmutableSortedDictionary<K, V> a, ImmutableSortedDictionary<K, V> b)
        {
            ImmutableSortedSet<K> keys = emptySet
                .Union(a.Keys)
                .Union(b.Keys);

            foreach (K key in keys)
            {
                if (!a.ContainsKey(key)) return 1;
                if (!b.ContainsKey(key)) return -1;

                int r = valueTraits.Compare(a[key], b[key]);
                if (r != 0) return r;
            }

            return 0;
        }

        public void AddToHash(HashBuilder b, ImmutableSortedDictionary<K, V> a)
        {
            b.Add(HashToken.Dictionary);
            b.Add(a.Count);
            foreach (KeyValuePair<K, V> kvp in a)
            {
                keyTraits.AddToHash(b, kvp.Key);
                valueTraits.AddToHash(b, kvp.Value);
            }
        }

        public bool CanSerialize(ImmutableSortedDictionary<K, V> a) => a.All(kvp => keyTraits.CanSerialize(kvp.Key) && valueTraits.CanSerialize(kvp.Value));

        public void Serialize(BinaryWriter dest, ImmutableSortedDictionary<K, V> a)
        {
            dest.Write(a.Count);
            foreach (KeyValuePair<K, V> kvp in a)
            {
                keyTraits.Serialize(dest, kvp.Key);
                valueTraits.Serialize(dest, kvp.Value);
            }
        }

        public ImmutableSortedDictionary<K, V> Deserialize(BinaryReader src)
        {
            int count = src.ReadInt32();
            ImmutableSortedDictionary<K, V>.Builder result = emptyDict.ToBuilder();
            for (int i = 0; i < count; ++i)
            {
                K k = keyTraits.Deserialize(src);
                V v = valueTraits.Deserialize(src);
                result.Add(k, v);
            }
            return result.ToImmutable();
        }

        public long MeasureBytes(ImmutableSortedDictionary<K, V> a)
        {
            return 4L + a.Select(kvp => keyTraits.MeasureBytes(kvp.Key) + valueTraits.MeasureBytes(kvp.Value)).Sum();
        }
    }

    public static class AssemblyTypeTraits
    {
        private static readonly Lazy<ITypeTraits<Assembly>> typeTraits = new Lazy<ITypeTraits<Assembly>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ImmutableSortedDictionary<string, Assembly> GetAssembliesByName()
        {
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            ImmutableSortedDictionary<string, ImmutableList<Assembly>> duplicates =
                ImmutableSortedDictionary<string, ImmutableList<Assembly>>.Empty;

            void addDuplicate(string name, Assembly a)
            {
                duplicates = duplicates.SetItem(name, duplicates.GetValueOrDefault(name, ImmutableList<Assembly>.Empty).Add(a));
            }

            ImmutableSortedDictionary<string, Assembly>.Builder results = ImmutableSortedDictionary<string, Assembly>.Empty.ToBuilder();

            foreach (Assembly a in allAssemblies)
            {
                string name = a.FullNameNotNull();
                if (duplicates.ContainsKey(name))
                {
                    addDuplicate(name, a);
                }
                else if (results.ContainsKey(name))
                {
                    Assembly old = results[name];
                    results.Remove(name);
                    addDuplicate(name, old);
                    addDuplicate(name, a);
                }
                else
                {
                    results.Add(name, a);
                }
            }

            if (!duplicates.IsEmpty)
            {
                System.Diagnostics.Debug.WriteLine("Duplicate assemblies:");
                foreach (KeyValuePair<string, ImmutableList<Assembly>> kvp in duplicates)
                {
                    System.Diagnostics.Debug.WriteLine($"  {kvp.Key}:");
                    foreach (Assembly a in kvp.Value)
                    {
                        System.Diagnostics.Debug.WriteLine($"    {a.Location}");
                    }
                }
            }

            return results.ToImmutable();
        }

        private static ITypeTraits<Assembly> GetTypeTraits()
        {
            object syncRoot = new object();
            ImmutableSortedDictionary<string, Assembly> nameToAssembly = GetAssembliesByName();

            return new ConvertTypeTraits<Assembly, string>
            (
                a => a.FullNameNotNull(),
                StringTypeTraits.Value,
                n =>
                {
                    lock (syncRoot)
                    {
                        if (nameToAssembly.ContainsKey(n))
                        {
                            return nameToAssembly[n];
                        }
                        else
                        {
                            nameToAssembly = GetAssembliesByName();
                            return nameToAssembly[n]; // if it throws, we did what we could
                        }
                    }
                }
            );
        }

        public static ITypeTraits<Assembly> Value => typeTraits.Value;

        private static readonly Lazy<Adapter<Assembly>> adapter = new Lazy<Adapter<Assembly>>(() => Adapter<Assembly>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<Assembly> Adapter => adapter.Value;
    }

    public static class TypeTypeTraits
    {
        private static readonly Lazy<ITypeTraits<Type>> typeTraits = new Lazy<ITypeTraits<Type>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<Type> GetTypeTraits()
        {
            RecursiveTypeTraits<Type> recurse = new RecursiveTypeTraits<Type>();

            ITypeTraits<Type> t = new GuardedTypeTraits<Type>
            (
                t0 => !t0.IsByRef && !t0.IsPointer,
                new UnionTypeTraits<Type>
                (
                    new IUnionCaseTypeTraits<Type>[]
                    {
                        new UnionCaseTypeTraits<Type, ValueTuple<string, Assembly>>
                        (
                            "plainType",
                            t1 => !t1.IsGenericType && !t1.IsArray,
                            t2 => (t2.FullNameNotNull(), t2.Assembly),
                            new ValueTupleTypeTraits<string, Assembly>
                            (
                                StringTypeTraits.Value,
                                AssemblyTypeTraits.Value
                            ),
                            tu => tu.Item2.GetType(tu.Item1, true).AssertNotNull()
                        ),
                        new UnionCaseTypeTraits<Type, ValueTuple<string, Assembly, int>>
                        (
                            "openGeneric",
                            t3 => t3.IsGenericTypeDefinition,
                            t4 => (t4.FullNameNotNull(), t4.Assembly, t4.GetGenericArguments().Length),
                            new ValueTupleTypeTraits<string, Assembly, int>
                            (
                                StringTypeTraits.Value,
                                AssemblyTypeTraits.Value,
                                Int32TypeTraits.Value
                            ),
                            tu =>
                            {
                                try
                                {
                                    return tu.Item2.GetTypes().Where(t10 => t10.FullName == tu.Item1 && t10.IsGenericTypeDefinition && t10.GetGenericArguments().Length == tu.Item3).Single();
                                }
                                catch(InvalidOperationException)
                                {
                                    throw new Exception($"Could not find open generic, assembly {tu.Item2.FullName}, name {tu.Item1}, number of args {tu.Item3}");
                                }
                            }
                        ),
                        new UnionCaseTypeTraits<Type, ValueTuple<string, Assembly, ImmutableList<Type>>>
                        (
                            "closedGeneric",
                            t5 => t5.IsGenericType && !t5.IsGenericTypeDefinition,
                            t6 => (t6.GetGenericTypeDefinition().FullNameNotNull(), t6.Assembly, t6.GetGenericArguments().ToImmutableList()),
                            new ValueTupleTypeTraits<string, Assembly, ImmutableList<Type>>
                            (
                                StringTypeTraits.Value,
                                AssemblyTypeTraits.Value,
                                new ListTypeTraits<Type>(recurse)
                            ),
                            tu =>
                            {
                                try
                                {
                                    Type openGeneric = tu.Item2.GetTypes().Where(t11 => t11.FullName == tu.Item1 && t11.IsGenericTypeDefinition && t11.GetGenericArguments().Length == tu.Item3.Count).Single();

                                    return openGeneric.MakeGenericType(tu.Item3.ToArray());
                                }
                                catch(InvalidOperationException)
                                {
                                    throw new Exception($"Could not find open generic, assembly {tu.Item2.FullName}, name {tu.Item1}, number of args {tu.Item3.Count}");
                                }
                            }
                        ),
                        new UnionCaseTypeTraits<Type, Type>
                        (
                            "array",
                            t7 => t7.IsArray,
                            t8 => t8.GetElementType().AssertNotNull(),
                            recurse,
                            t9 => t9.MakeArrayType()
                        )
                    }
                    .ToImmutableList()
                )
            );

            recurse.Set(t);

            return t;
        }

        public static ITypeTraits<Type> Value => typeTraits.Value;

        private static readonly Lazy<Adapter<Type>> adapter = new Lazy<Adapter<Type>>(() => Adapter<Type>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<Type> Adapter => adapter.Value;
    }
}
