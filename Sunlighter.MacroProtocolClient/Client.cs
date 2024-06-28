using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Sunlighter.MacroProtocol;
using Sunlighter.MacroProtocol.Sockets;
using Sunlighter.MacroProtocol.TypeTraits;

namespace Sunlighter.MacroProtocolClient
{
    public static class Client
    {
        public static void RunTransform
        (
            this object tt,
            IPEndPoint endPoint,
            string name,
            ImmutableList<string> args
        )
        {
            TextTransformationOutput output = new TextTransformationOutput(tt);

            Task t = MacroProtocolClientAdapter.WithClientAsync
            (
                endPoint,
                async server =>
                {
                    MacroProtocolResponse a = await server.GenerateAsync(name, args);
                    if (a is MPR_Output result)
                    {
                        foreach (MPR_Command cmd in result.Commands)
                        {
                            cmd.Visit
                            (
                                w => output.Write(w.Value),
                                wl => output.WriteLine(wl.Value),
                                () => output.WriteLine(),
                                pi => output.PushIndent(pi.Indent),
                                () => output.PopIndent()
                            );
                        }
                    }
                    else if (a is MPR_Error error)
                    {
                        output.PushIndent("//  ");

                        void writeError(ExceptionRecord er)
                        {
                            output.WriteLine($"{er.Type}: {er.Message}");
                            if (er.Causes.Count > 0)
                            {
                                output.PushIndent("    ");
                                foreach (ExceptionRecord cause in er.Causes)
                                {
                                    writeError(cause);
                                }
                                output.PopIndent();
                            }
                        }

                        writeError(error.ExceptionRecord);

                        output.PopIndent();
                    }
                    else
                    {
                        output.PushIndent("//  ");
                        output.WriteLine($"Unknown type of response {ReflectionExtensions.GetTypeName(a.GetType())}");
                    }
                }
            );

            t.Wait();
        }

        public static void RunTransform
        (
            this object tt,
            IPEndPoint endPoint,
            string name,
            IEnumerable<string> args
        )
        {
            RunTransform(tt, endPoint, name, args.ToImmutableList());
        }
    }
}
