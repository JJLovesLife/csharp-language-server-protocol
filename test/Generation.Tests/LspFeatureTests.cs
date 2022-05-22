using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc.Generators;
using Xunit;

namespace Generation.Tests
{
    [UsesVerify]
    public class LspFeatureTests
    {
//        [Fact(Skip = "for testing"]
        [Fact]
        public async Task Supports_Params_Type_As_Source()
        {
            var source = FeatureFixture.ReadSource("Workspace.WorkspaceSymbolsFeature.cs");
            await Verify(
                new
                {
                    RegistrationOptionsGenerator = await GenerationHelpers.GenerateAsync<RegistrationOptionsGenerator>(source),
                    StronglyTypedGenerator = await GenerationHelpers.GenerateAsync<StronglyTypedGenerator>(source),
                    GenerateHandlerMethodsGenerator = await GenerationHelpers.GenerateAsync<GenerateHandlerMethodsGenerator>(source),
                    AutoImplementParamsGenerator = await GenerationHelpers.GenerateAsync<AutoImplementParamsGenerator>(source),
                    AssemblyCapabilityKeyAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyCapabilityKeyAttributeGenerator>(source),
                    AssemblyJsonRpcHandlersAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyJsonRpcHandlersAttributeGenerator>(source),
                    EnumLikeStringGenerator = await GenerationHelpers.GenerateAsync<EnumLikeStringGenerator>(source),
                });
        }

        [Fact]
        public async Task Supports_Generating_Custom_Language_Extensions()
        {
            var source = @"
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

#nullable enable
namespace Lsp.Tests.Integration.Fixtures
{
    [Parallel, Method(""tests/run"", Direction.ClientToServer)]
    [
        GenerateHandler,
        GenerateHandlerMethods(typeof(ILanguageServerRegistry)),
        GenerateRequestMethods(typeof(ILanguageClient))
    ]
    [RegistrationOptions(typeof(UnitTestRegistrationOptions)), Capability(typeof(UnitTestCapability))]
    public partial class UnitTest : IRequest
    {
        public string Name { get; set; } = null!;
    }

    [Parallel, Method(""tests/discover"", Direction.ClientToServer)]
    [
        GenerateHandler,
        GenerateHandlerMethods(typeof(ILanguageServerRegistry)),
        GenerateRequestMethods(typeof(ILanguageClient))
    ]
    [RegistrationOptions(typeof(UnitTestRegistrationOptions)), Capability(typeof(UnitTestCapability))]
    public partial class DiscoverUnitTestsParams : IPartialItemsRequest<Container<UnitTest>, UnitTest>, IWorkDoneProgressParams
    {
        public ProgressToken? PartialResultToken { get; set; } = null!;
        public ProgressToken? WorkDoneToken { get; set; } = null!;
    }

    [CapabilityKey(""workspace"", ""unitTests"")]
    public partial class UnitTestCapability : DynamicCapability
    {
        public string Property { get; set; } = null!;
    }

    [GenerateRegistrationOptions(""unitTestDiscovery"")]
    public partial class UnitTestRegistrationOptions : IWorkDoneProgressOptions
    {
        [Optional] public bool SupportsDebugging { get; set; }
    }
}
#nullable restore";

            await Verify(
                new
                {
                    RegistrationOptionsGenerator = await GenerationHelpers.GenerateAsync<RegistrationOptionsGenerator>(source),
                    StronglyTypedGenerator = await GenerationHelpers.GenerateAsync<StronglyTypedGenerator>(source),
                    GenerateHandlerMethodsGenerator = await GenerationHelpers.GenerateAsync<GenerateHandlerMethodsGenerator>(source),
                    AutoImplementParamsGenerator = await GenerationHelpers.GenerateAsync<AutoImplementParamsGenerator>(source),
                    AssemblyCapabilityKeyAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyCapabilityKeyAttributeGenerator>(source),
                    AssemblyJsonRpcHandlersAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyJsonRpcHandlersAttributeGenerator>(source),
                    EnumLikeStringGenerator = await GenerationHelpers.GenerateAsync<EnumLikeStringGenerator>(source),
                });
        }

        [Fact]
        public async Task Supports_Generating_Void_Task_Return()
        {
            var source = @"
// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated a code generator.
// </auto-generated>
// ------------------------------------------------------------------------------

using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

#nullable enable
namespace Lsp.Tests.Integration.Fixtures
{
    [Parallel]
    [Method(ClientNames.RegisterCapability, Direction.ServerToClient)]
    [
        GenerateHandler(""OmniSharp.Extensions.LanguageServer.Protocol.Client"", Name = ""RegisterCapability""),
        GenerateHandlerMethods(typeof(ILanguageClientRegistry),
        GenerateRequestMethods(typeof(IClientLanguageServer), typeof(ILanguageServer))
    ]
    public class RegistrationParams : IJsonRpcRequest
    {
        public RegistrationContainer Registrations { get; set; } = null!;
    }

    /// <summary>
    /// General parameters to to regsiter for a capability.
    /// </summary>
    [DebuggerDisplay(""{"" + nameof(DebuggerDisplay) + "",nq}"")]
    [GenerateContainer]
    public partial class Registration
    {
    }
}
#nullable restore";

            await Verify(
                new
                {
                    RegistrationOptionsGenerator = await GenerationHelpers.GenerateAsync<RegistrationOptionsGenerator>(source),
                    StronglyTypedGenerator = await GenerationHelpers.GenerateAsync<StronglyTypedGenerator>(source),
                    GenerateHandlerMethodsGenerator = await GenerationHelpers.GenerateAsync<GenerateHandlerMethodsGenerator>(source),
                    AutoImplementParamsGenerator = await GenerationHelpers.GenerateAsync<AutoImplementParamsGenerator>(source),
                    AssemblyCapabilityKeyAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyCapabilityKeyAttributeGenerator>(source),
                    AssemblyJsonRpcHandlersAttributeGenerator = await GenerationHelpers.GenerateAsync<AssemblyJsonRpcHandlersAttributeGenerator>(source),
                    EnumLikeStringGenerator = await GenerationHelpers.GenerateAsync<EnumLikeStringGenerator>(source),
                });
        }
    }
}
