using Sunlighter.MacroProtocol.TypeTraits;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Sunlighter.MacroProtocol
{
    public abstract class MPR_Command
    {
        private static Lazy<ITypeTraits<MPR_Command>> typeTraits =
            new Lazy<ITypeTraits<MPR_Command>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<MPR_Command> GetTypeTraits()
        {
            return new UnionTypeTraits<MPR_Command>
            (
                ImmutableList<IUnionCaseTypeTraits<MPR_Command>>.Empty.Add
                (
                    new UnionCaseTypeTraits2<MPR_Command, MPR_Write>
                    (
                        "Write",
                        new ConvertTypeTraits<MPR_Write, string>(w => w.Value, StringTypeTraits.Value, s => new MPR_Write(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<MPR_Command, MPR_WriteLine>
                    (
                        "WriteLine",
                        new ConvertTypeTraits<MPR_WriteLine, string>(w => w.Value, StringTypeTraits.Value, s => new MPR_WriteLine(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<MPR_Command, MPR_NewLine>
                    (
                        "NewLine",
                        new UnitTypeTraits<MPR_NewLine>(0x2328467Fu, MPR_NewLine.Value)
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<MPR_Command, MPR_PushIndent>
                    (
                        "PushIndent",
                        new ConvertTypeTraits<MPR_PushIndent, string>(p => p.Indent, StringTypeTraits.Value, s => new MPR_PushIndent(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<MPR_Command, MPR_PopIndent>
                    (
                        "PopIndent",
                        new UnitTypeTraits<MPR_PopIndent>(0xCA798ABCu, MPR_PopIndent.Value)
                    )
                )
            );
        }

        public static ITypeTraits<MPR_Command> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<MPR_Command>> adapter =
            new Lazy<Adapter<MPR_Command>>(() => Adapter<MPR_Command>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<MPR_Command> Adapter => adapter.Value;

        public abstract void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        );
    }

    public sealed class MPR_Write : MPR_Command
    {
        private readonly string value;

        public MPR_Write(string value)
        {
            this.value = value;
        }

        public string Value => value;

        public override void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onWrite(this);
        }
    }

    public sealed class MPR_WriteLine : MPR_Command
    {
        private readonly string value;

        public MPR_WriteLine(string value)
        {
            this.value = value;

        }

        public string Value => value;

        public override void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onWriteLine(this);
        }
    }

    public sealed class MPR_NewLine : MPR_Command
    {
        private readonly static MPR_NewLine value = new MPR_NewLine();

        private MPR_NewLine() { }

        public static MPR_NewLine Value => value;

        public override void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onNewLine();
        }
    }

    public sealed class MPR_PushIndent : MPR_Command
    {
        private readonly string indent;

        public MPR_PushIndent(string indent)
        {
            this.indent = indent;
        }

        public string Indent => indent;

        public override void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onPushIndent(this);
        }
    }

    public sealed class MPR_PopIndent : MPR_Command
    {
        private readonly static MPR_PopIndent value = new MPR_PopIndent();

        private MPR_PopIndent() { }

        public static MPR_PopIndent Value => value;

        public override void Visit
        (
            Action<MPR_Write> onWrite,
            Action<MPR_WriteLine> onWriteLine,
            Action onNewLine,
            Action<MPR_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onPopIndent();
        }
    }
}
