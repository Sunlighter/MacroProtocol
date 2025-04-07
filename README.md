<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# MacroProtocol

This project contains a skeleton for a code generation service. It does not generate any useful code, but all the
infrastructure is in place.

## Quick Start

To see this work, clone the repository and open the solution in Visual Studio. Run the `MacroProtocolTestServer`
project in the debugger. This will start a server that listens on the IPv6 loopback address on port `59905`.

Then (while the test server is running) open Solution Explorer, find the text template in the
`MacroProtocolClientTest` project, right-click the `GeneratedCode.tt` file, and select &ldquo;Run Custom Tool.&rdquo;
This will run the text template, which will contact the running server to cause it to generate code. The generated
code will be written to the `GeneratedCode.generated.cs` file.

## The Point

This setup allows code generation to be moved out of the T4 template into a separate process. This allows the code
generator to run a more recent version of dot-Net even though old-style T4 Text Templates are limited to running
dot-Net Framework (and dot-Net Standard 2.0). In this project, the code generator runs on dot-Net 8. It could easily
be made to run some other version. (With a little work, it should be possible to write a code generation service in a
non-dot-Net language... but this would require porting the Type Traits library to that language.)

I am aware that Microsoft has written a new command-line version of their [text templating
tool](https://devblogs.microsoft.com/dotnet/t4-command-line-tool-for-dotnet/), which worked at the time with .NET 6,
but I wanted something that I could use from Visual Studio.

Text templates are usually run at &ldquo;design time,&rdquo; which usually means they are run from Visual Studio. The
output of the text templating tool is usually checked into version control. The upshot is that the build process
(which might be part of a CI/CD setup) just compiles the generated code from version control; it does not have to run
the code generator, and the code generator can use features and services that are not available at build time. (Also,
the generated code is usually needed for IntelliSense while writing other code.)

Another option, which might be superior to the approach in this project, would be to write a completely stand-alone
code generator with a UI. It would be able to find XML files describing inputs and outputs, show when the outputs are
out of date, and allow the user to regenerate them.

My old Scheme in C# made extensive use of Text Templates for code generation. Most of my templates referenced a
`Macros.dll` library which was loaded into the templates and handled the actual code generation. It could parse XML
descriptions of the code to be generated, which could be supplied in a different file. I wrote this project in order
to try to make Text Templates useful with more recent versions of dot-Net.

## The Client (T4 Template)

The `MacroProtocolClientTest` project doesn't do anything useful when compiled, but it contains a text template which
contacts a code generation server. Most of the code is in the client library, `Sunlighter.MacroProtocolClient`, so
that the template itself can be short. It needs only to call the `RunTransform` function.

The template seems to load two versions of the `System.Collections.Immutable` library, and favors the older one when
you write C# code in the template. The newer one is needed in order to load the client library. This forced me to
create an overload of my `RunTransform` function which takes an `IEnumerable<T>`. It ends up iterating over an
`ImmutableList` of one kind in order to create an `ImmutableList` of the other kind.

## The Protocol

The `Sunlighter.MacroProtocol` library targets both .NET Standard 2.0 and .NET Core 8, and other targets can probably
be added. The library&rsquo;s purpose is to define the protocol. It now uses the `Sunlighter.TypeTraitsLib` library,
which makes it easy to define serializable types and protocols based on them.

The `MacroProtocolRequest` class has only one descendant; I wanted to keep the option open to add more.

The protocol could probably be easier to use.

## The Server

The `MacroProtocolTestServer` is a command-line program that listens on the IPv6 loopback on port `59905`. It is
possible to run this program in the Visual Studio debugger while also running text templates in Visual Studio which
call into it. It is also possible to find the `exe` and run it directly (so that the Visual Studio debugger is free
for other uses).
