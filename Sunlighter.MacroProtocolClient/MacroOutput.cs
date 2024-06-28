using System;
using System.Collections.Generic;
using System.Text;

namespace Sunlighter.MacroProtocolClient
{
    public interface IMacroOutput
    {
        void Write(string str);
        void WriteLine(string str);
        void WriteLine();
        void PushIndent(string indent);
        void PopIndent();
    }

    public sealed class TextTransformationOutput : IMacroOutput
    {
        private readonly dynamic tt;

        public TextTransformationOutput(dynamic tt)
        {
            this.tt = tt;
        }

        public void Write(string str)
        {
            tt.Write(str);
        }

        public void WriteLine(string str)
        {
            tt.WriteLine(str);
        }

        public void WriteLine()
        {
            tt.WriteLine("");
        }

        public void PushIndent(string indent)
        {
            tt.PushIndent(indent);
        }

        public void PopIndent()
        {
            tt.PopIndent();
        }
    }

    public interface IEmittable
    {
        void EmitTo(IMacroOutput dest);
    }

    public static partial class MacroUtils
    {
        public static void Emit(this object tt, IEmittable e)
        {
            TextTransformationOutput tto = new TextTransformationOutput(tt);
            e.EmitTo(tto);
        }

        public static readonly Action NoAction = delegate () { };
    }
}
