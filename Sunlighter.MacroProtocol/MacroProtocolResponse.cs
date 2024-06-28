using Sunlighter.MacroProtocol.TypeTraits;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sunlighter.MacroProtocol
{
    public abstract class MacroProtocolResponse
    {
        private static Lazy<ITypeTraits<MacroProtocolResponse>> typeTraits =
            new Lazy<ITypeTraits<MacroProtocolResponse>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<MacroProtocolResponse> GetTypeTraits()
        {
            return new UnionTypeTraits<MacroProtocolResponse>
            (
                ImmutableList<IUnionCaseTypeTraits<MacroProtocolResponse>>.Empty.Add
                (
                    new UnionCaseTypeTraits2<MacroProtocolResponse, MPR_Output>
                    (
                        "Output",
                        new ConvertTypeTraits<MPR_Output, ImmutableList<MPR_Command>>
                        (
                            obj => obj.Commands,
                            new ListTypeTraits<MPR_Command>(MPR_Command.TypeTraits),
                            lst => new MPR_Output(lst)
                        )
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<MacroProtocolResponse, MPR_Error>
                    (
                        "Error",
                        new ConvertTypeTraits<MPR_Error, ExceptionRecord>
                        (
                            obj => obj.ExceptionRecord,
                            ExceptionRecord.TypeTraits,
                            excr => new MPR_Error(excr)
                        )
                    )
                )
            );
        }

        public static ITypeTraits<MacroProtocolResponse> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<MacroProtocolResponse>> adapter =
            new Lazy<Adapter<MacroProtocolResponse>>(() => Adapter<MacroProtocolResponse>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<MacroProtocolResponse> Adapter => adapter.Value;
    }

    public sealed class MPR_Output : MacroProtocolResponse
    {
        private readonly ImmutableList<MPR_Command> commands;

        public MPR_Output(ImmutableList<MPR_Command> commands)
        {
            this.commands = commands;
        }

        public ImmutableList<MPR_Command> Commands => commands;
    }

    public sealed class MPR_Error : MacroProtocolResponse
    {
        private readonly ExceptionRecord er;

        public MPR_Error(ExceptionRecord er)
        {
            this.er = er;
        }

        public ExceptionRecord ExceptionRecord => er;
    }

    public sealed class ExceptionRecord
    {
        private readonly string type;
        private readonly string message;
        private readonly ImmutableList<ExceptionRecord> causes;

        public ExceptionRecord
        (
            string type,
            string message,
            ImmutableList<ExceptionRecord> causes
        )
        {
            this.type = type;
            this.message = message;
            this.causes = causes;
        }

        public string Type => type;

        public string Message => message;

        public ImmutableList<ExceptionRecord> Causes => causes;

        private static Lazy<ITypeTraits<ExceptionRecord>> typeTraits =
            new Lazy<ITypeTraits<ExceptionRecord>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<ExceptionRecord> GetTypeTraits()
        {
            var traitsRecursive = new RecursiveTypeTraits<ExceptionRecord>();

            var traits = new ConvertTypeTraits<ExceptionRecord, (string, string, ImmutableList<ExceptionRecord>)>
            (
                er => (er.Type, er.Message, er.Causes),
                new ValueTupleTypeTraits<string, string, ImmutableList<ExceptionRecord>>
                (
                    StringTypeTraits.Value,
                    StringTypeTraits.Value,
                    new ListTypeTraits<ExceptionRecord>(traitsRecursive)
                ),
                x => new ExceptionRecord(x.Item1, x.Item2, x.Item3)
            );

            traitsRecursive.Set(traits);

            return traits;
        }

        public static ITypeTraits<ExceptionRecord> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<ExceptionRecord>> adapter =
            new Lazy<Adapter<ExceptionRecord>>(() => Adapter<ExceptionRecord>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<ExceptionRecord> Adapter => adapter.Value;

        public static ExceptionRecord GetRecord(Exception exc)
        {
            ImmutableList<Exception> children;

            if (exc is AggregateException aexc)
            {
                children = aexc.InnerExceptions.ToImmutableList();
            }
#if NETSTANDARD2_0
            else if (!ReferenceEquals(exc.InnerException, null))
#else
            else if (exc.InnerException is not null)
#endif
            {
                children = ImmutableList<Exception>.Empty.Add(exc.InnerException);
            }
            else
            {
                children = ImmutableList<Exception>.Empty;
            }

            return new ExceptionRecord
            (
                ReflectionExtensions.GetTypeName(exc.GetType()),
                exc.Message ?? string.Empty,
                children.Select(GetRecord).ToImmutableList()
            );
        }
    }
}
