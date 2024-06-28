<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# MacroProtocol

This project contains a skeleton for a code generation service. It does not generate any useful code, but all the
infrastructure is in place.

The intention is that an old-style T4 Text Template can contact the service over the TCP loopback connection, send it
a request with parameters, and receive the generated code. (A parameter, for example, may include the entire contents
of an XML file describing the code to be generated.)

This project allows the code generator to run a more recent version of dot-Net (in a separate process) even though
old-style T4 Text Templates are limited to running dot-Net Framework (and dot-Net Standard 2.0). In this project, the
code generator runs on dot-Net 8. It could easily be made to run some other version. (With a little work, it should be
possible to write a code generation service in a non-dot-Net language...)

I am aware that Microsoft has written a new command-line version of their [text templating
tool](https://devblogs.microsoft.com/dotnet/t4-command-line-tool-for-dotnet/), which worked at the time with .NET 6,
but I wanted something that I could use from Visual Studio.

Text templates are usually run at &ldquo;design time,&rdquo; which means they are run from Visual Studio. The output
of the text templating tool is usually checked into version control. The upshot is that the build process (which might
be part of a CI/CD setup) just compiles the generated code from version control; it does not have to run the code
generator, and the code generator can use features and services that are not available at build time. (Also, the
generated code is usually needed for IntelliSense while writing other code.)

Another option, which might be superior to the approach in this project, would be to write a completely stand-alone
code generator with a UI. It would be able to find XML files describing inputs and outputs, show when the outputs are
out of date, and allow the user to regenerate them.

My old Scheme in C# made extensive use of Text Templates for code generation. Most of my templates referenced a
`Macros.dll` library which handled the actual code generation (and could parse XML descriptions of the code to be
generated, which could be supplied in a different file). I wrote this project in order to try to make Text Templates
useful with more recent versions of dot-Net.

## The Client

The `MacroProtocolClientTest` project doesn't do anything useful when compiled, but it contains a text template which
contacts a code generation server. Most of the code is in the client library, `Sunlighter.MacroProtocolClient`, so
that the template itself can be short. It needs only to call the `RunTransform` function.

The template seems to load two versions of the `System.Collections.Immutable` library, and favors the older one when
you write C# code in the template. The newer one is needed in order to load the client library. This forced me to
create an overload of my `RunTransform` function which takes an `IEnumerable<T>`. It ends up iterating over an
`ImmutableList` of one kind in order to create an `ImmutableList` of the other kind.

## The Protocol

The `Sunlighter.MacroProtocol` library targets both .NET Standard 2.0 and .NET Core 8, and other targets can probably
be added. The library&rsquo;s purpose is to define the protocol. I dragged together code from some of my other
unpublished projects in order to make this work, so there is a lot of code here that isn&rsquo;t actually used (but
could be in the future).

The protocol includes code for `ITypeTraits` which make it possible to compare, hash, and serialize any object
described by the traits. For many classes used by the protocol, I have added static properties which return the traits
of those classes.

(There is some degree of redundancy inherent in using the `ITypeTraits` classes, because `ITypeTraits` works like a
&ldquo;shadow type system.&rdquo; A code generator could help relieve the tedium created by this redundancy...)

The `MacroProtocolRequest` class has only one descendant; I wanted to keep the option open to add more.

The protocol could probably be easier to use.

## The Server

The `MacroProtocolTestServer` is a command-line program that listens on the IPv6 loopback on port `59905`. Although it
is safe to run it in the Visual Studio debugger, I don't know if Visual Studio can run text templates while it is
debugging something else, so I prefer to run this program outside of Visual Studio (which means finding the built
`exe` and double-clicking it).
