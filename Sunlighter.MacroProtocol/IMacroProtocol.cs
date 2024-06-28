using Sunlighter.MacroProtocol.Sockets;
using Sunlighter.OptionLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sunlighter.MacroProtocol
{
    public interface IMacroProtocol : IDisposable
    {
        Task<MacroProtocolResponse> GenerateAsync(string commandName, ImmutableList<string> args);

        Task CloseAsync();
    }

    public sealed class MacroProtocolClientAdapter : IMacroProtocol
    {
        private readonly IObjectStream<MacroProtocolResponse, MacroProtocolRequest> channel;

        private MacroProtocolClientAdapter
        (
            IObjectStream<MacroProtocolResponse, MacroProtocolRequest> channel
        )
        {
            this.channel = channel;
        }

        public async Task<MacroProtocolResponse> GenerateAsync(string commandName, ImmutableList<string> args)
        {
            MacroProtocolRequest q = new MPR_Generate(commandName, args);

            await channel.Write(q);

            Option<MacroProtocolResponse> a = await channel.ReadObjectOrEof();

            if (a.HasValue)
            {
                return a.Value;
            }
            else
            {
                throw new MacroProtocolException("Response not received");
            }
        }

        public async Task CloseAsync()
        {
            channel.WriteEof();

            Option<MacroProtocolResponse> a = await channel.ReadObjectOrEof();

            if (a.HasValue)
            {
                throw new MacroProtocolException("Unexpected response to EOF");
            }
            else
            {
                // do nothing
            }
        }

        public void Dispose()
        {
            channel.Dispose();
        }

        public static Task WithClientAsync(IPEndPoint endPoint, Func<IMacroProtocol, Task> func)
        {
            return TcpUtility.RunClient
            (
                endPoint,
                MacroProtocolRequest.TypeTraits,
                MacroProtocolResponse.TypeTraits,
                channel => func(new MacroProtocolClientAdapter(channel))
            );
        }
    }

    public sealed class MacroProtocolServerAdapter
    {
        public static Func<IPEndPoint, IObjectStream<MacroProtocolRequest, MacroProtocolResponse>, CancellationToken, Task> GetPeerHandler
        (
            Func<IPEndPoint, CancellationToken, IMacroProtocol> serviceProvider
        )
        {
            async Task channelHandlePeer(IPEndPoint endPoint, IObjectStream<MacroProtocolRequest, MacroProtocolResponse> channel, CancellationToken cToken)
            {
                IMacroProtocol server = serviceProvider(endPoint, cToken);

                while(true)
                {
                    cToken.ThrowIfCancellationRequested();

                    Option<MacroProtocolRequest> q = await channel.ReadObjectOrEof();
                    if (q.HasValue)
                    {
                        if (q.Value is MPR_Generate qGenerate)
                        {
                            MacroProtocolResponse a = await server.GenerateAsync(qGenerate.CommandName, qGenerate.Arguments);
                            await channel.Write(a);
                        }
                        else
                        {
                            await channel.Write
                            (
                                new MPR_Error
                                (
                                    new ExceptionRecord("--", "Unknown request type", ImmutableList<ExceptionRecord>.Empty)
                                )
                            );
                        }
                    }
                    else
                    {
                        channel.WriteEof();
                        channel.Dispose();
                        break;
                    }
                }
            }

            return channelHandlePeer;
        }
    }
}
