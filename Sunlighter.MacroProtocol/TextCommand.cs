using Sunlighter.TypeTraitsLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Sunlighter.MacroProtocol
{
    public abstract class TextCommand
    {
        private static Lazy<ITypeTraits<TextCommand>> typeTraits =
            new Lazy<ITypeTraits<TextCommand>>(GetTypeTraits, LazyThreadSafetyMode.ExecutionAndPublication);

        private static ITypeTraits<TextCommand> GetTypeTraits()
        {
            return new UnionTypeTraits<string, TextCommand>
            (
                StringTypeTraits.Value,
                ImmutableList<IUnionCaseTypeTraits<string, TextCommand>>.Empty.Add
                (
                    new UnionCaseTypeTraits2<string, TextCommand, TC_Write>
                    (
                        "Write",
                        new ConvertTypeTraits<TC_Write, string>(w => w.Value, StringTypeTraits.Value, s => new TC_Write(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<string, TextCommand, TC_WriteLine>
                    (
                        "WriteLine",
                        new ConvertTypeTraits<TC_WriteLine, string>(w => w.Value, StringTypeTraits.Value, s => new TC_WriteLine(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<string, TextCommand, TC_NewLine>
                    (
                        "NewLine",
                        new UnitTypeTraits<TC_NewLine>(0x2328467Fu, TC_NewLine.Value)
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<string, TextCommand, TC_PushIndent>
                    (
                        "PushIndent",
                        new ConvertTypeTraits<TC_PushIndent, string>(p => p.Indent, StringTypeTraits.Value, s => new TC_PushIndent(s))
                    )
                )
                .Add
                (
                    new UnionCaseTypeTraits2<string, TextCommand, TC_PopIndent>
                    (
                        "PopIndent",
                        new UnitTypeTraits<TC_PopIndent>(0xCA798ABCu, TC_PopIndent.Value)
                    )
                )
            );
        }

        public static ITypeTraits<TextCommand> TypeTraits => typeTraits.Value;

        private static Lazy<Adapter<TextCommand>> adapter =
            new Lazy<Adapter<TextCommand>>(() => Adapter<TextCommand>.Create(typeTraits.Value), LazyThreadSafetyMode.ExecutionAndPublication);

        public static Adapter<TextCommand> Adapter => adapter.Value;

        public abstract void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        );
    }

    public sealed class TC_Write : TextCommand
    {
        private readonly string value;

        public TC_Write(string value)
        {
            this.value = value;
        }

        public string Value => value;

        public override void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onWrite(this);
        }
    }

    public sealed class TC_WriteLine : TextCommand
    {
        private readonly string value;

        public TC_WriteLine(string value)
        {
            this.value = value;

        }

        public string Value => value;

        public override void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onWriteLine(this);
        }
    }

    public sealed class TC_NewLine : TextCommand
    {
        private readonly static TC_NewLine value = new TC_NewLine();

        private TC_NewLine() { }

        public static TC_NewLine Value => value;

        public override void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onNewLine();
        }
    }

    public sealed class TC_PushIndent : TextCommand
    {
        private readonly string indent;

        public TC_PushIndent(string indent)
        {
            this.indent = indent;
        }

        public string Indent => indent;

        public override void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onPushIndent(this);
        }
    }

    public sealed class TC_PopIndent : TextCommand
    {
        private readonly static TC_PopIndent value = new TC_PopIndent();

        private TC_PopIndent() { }

        public static TC_PopIndent Value => value;

        public override void Visit
        (
            Action<TC_Write> onWrite,
            Action<TC_WriteLine> onWriteLine,
            Action onNewLine,
            Action<TC_PushIndent> onPushIndent,
            Action onPopIndent
        )
        {
            onPopIndent();
        }
    }
}
