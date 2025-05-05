using Sunlighter.MacroProtocol;
using Sunlighter.TypeTraitsLib.Networking;
using System.Collections.Immutable;
using System.Net;

namespace MacroProtocolTestServer
{
    internal sealed class Program
    {
        public const int port = 59905;

        static void Main(string[] args)
        {
            Console.WriteLine($"Listening on port {port}");

            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    Task t0 = Task.Run
                    (
                        () => TcpUtility.RunServer
                        (
                            new IPEndPoint(IPAddress.IPv6Loopback, port),
                            MacroProtocolRequest.TypeTraits,
                            MacroProtocolResponse.TypeTraits,
                            MacroProtocolServerAdapter.GetPeerHandler((peer, cToken) => new GeneratorService(peer)),
                            cts.Token
                        )
                    );

                    Console.Write("Press [Enter] to terminate server: ");
                    Console.ReadLine();

                    cts.Cancel();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine();
                Console.WriteLine("***** Exception! *****");
                Console.WriteLine();
                Console.WriteLine(exc);
            }
        }
    }

    internal sealed class GeneratorService : IMacroProtocol
    {
        private readonly IPEndPoint peer;

        public GeneratorService(IPEndPoint peer)
        {
            this.peer = peer;
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public Task<MacroProtocolResponse> GenerateAsync(string commandName, ImmutableList<string> args)
        {
            return Task.FromResult
            (
                OutputBuilder.GenerateResponse
                (
                    dest =>
                    {
                        dest.PushIndent("//  ");
                        dest.WriteLine($"Now: {DateTime.Now:f}");
                        dest.WriteLine($"Peer IP: {peer}");
                        dest.WriteLine($"Command: {commandName}");
                        foreach(int i in Enumerable.Range(0, args.Count))
                        {
                            dest.WriteLine($"Arg {i}: {args[i]}");
                        }
                        dest.PopIndent();
                    }
                )
            );
        }

        public void Dispose()
        {
            // do nothing
        }
    }

    internal interface IMacroOutput
    {
        void Write(string str);
        void WriteLine(string str);
        void WriteLine();
        void PushIndent(string indent);
        void PopIndent();
    }

    internal sealed class OutputBuilder : IMacroOutput
    {
        private readonly ImmutableList<TextCommand>.Builder builder;

        public OutputBuilder()
        {
            builder = ImmutableList<TextCommand>.Empty.ToBuilder();
        }

        public void PopIndent()
        {
            builder.Add(TC_PopIndent.Value);
        }

        public void PushIndent(string indent)
        {
            builder.Add(new TC_PushIndent(indent));
        }

        public void Write(string str)
        {
            builder.Add(new TC_Write(str));
        }

        public void WriteLine(string str)
        {
            builder.Add(new TC_WriteLine(str));
        }

        public void WriteLine()
        {
            builder.Add(TC_NewLine.Value);
        }

        public ImmutableList<TextCommand> Commands => builder.ToImmutable();

        public static MacroProtocolResponse GenerateResponse(Action<IMacroOutput> proc)
        {
            try
            {
                OutputBuilder b = new OutputBuilder();
                proc(b);
                return new MPA_Output(b.Commands);
            }
            catch(Exception exc)
            {
                return new MPA_Error(ExceptionRecord.GetRecord(exc));
            }
        }
    }
}
