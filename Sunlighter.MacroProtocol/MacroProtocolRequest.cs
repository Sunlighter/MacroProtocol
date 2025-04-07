using System;
using System.Collections.Immutable;
using System.Threading;
using Sunlighter.TypeTraitsLib;

namespace Sunlighter.MacroProtocol
{
    public abstract class MacroProtocolRequest
    {
        private static Lazy<ITypeTraits<MacroProtocolRequest>> typeTraits =
            new Lazy<ITypeTraits<MacroProtocolRequest>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<MacroProtocolRequest> GetTypeTraits()
        {
            return new UnionTypeTraits<string, MacroProtocolRequest>
            (
                StringTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<string, MacroProtocolRequest>>.Empty.Add
                (
                    new UnionCaseTypeTraits2<string, MacroProtocolRequest, MPQ_Generate>
                    (
                        "Generate",
                        new ConvertTypeTraits<MPQ_Generate, (string, ImmutableList<string>)>
                        (
                            obj => (obj.CommandName, obj.Arguments),
                            new ValueTupleTypeTraits<string, ImmutableList<string>>
                            (
                                StringTypeTraits.Value,
                                new ListTypeTraits<string>(StringTypeTraits.Value)
                            ),
                            x => new MPQ_Generate(x.Item1, x.Item2)
                        )
                    )
                )
            );
        }

        public static ITypeTraits<MacroProtocolRequest> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<MacroProtocolRequest>> adapter =
            new Lazy<Adapter<MacroProtocolRequest>>(() => Adapter<MacroProtocolRequest>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<MacroProtocolRequest> Adapter => adapter.Value;
    }

    public sealed class MPQ_Generate : MacroProtocolRequest
    {
        private readonly string commandName;
        private readonly ImmutableList<string> args;

        public MPQ_Generate(string commandName, ImmutableList<string> args)
        {
            this.commandName = commandName;
            this.args = args;
        }

        public string CommandName => commandName;

        public ImmutableList<string> Arguments => args;
    }
}
