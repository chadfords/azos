
using System;
using System.Collections.Generic;

using Azos.Apps;
using Azos.Conf;
using Azos.Collections;
using Azos.Instrumentation;
using Azos.Glue.Protocol;
using Azos.Time;


namespace Azos.Glue
{
    /// <summary>
    /// Represents a contract for Glue - a technology that provides asynchronous distributed component interconnection
    /// </summary>
    public interface IGlue : IApplicationComponent, ILocalizedTimeProvider
    {

        /// <summary>
        /// Retrieves a binding for node and throws if such binding is not known
        /// </summary>
        Binding GetNodeBinding(Node node);

        /// <summary>
        /// Retrieves a binding for node and throws if such binding is not known
        /// </summary>
        Binding GetNodeBinding(string node);

        /// <summary>
        /// Performs provider lookup by name
        /// </summary>
        IRegistry<Provider> Providers { get; }

        /// <summary>
        /// Performs binding lookup by name
        /// </summary>
        IRegistry<Binding> Bindings { get; }

        /// <summary>
        /// Performs ServerEndPoint lookup by name
        /// </summary>
        IRegistry<ServerEndPoint> Servers { get; }


        /// <summary>
        /// Registry of inspectors that deal with client-side messages
        /// </summary>
        OrderedRegistry<IClientMsgInspector> ClientMsgInspectors { get; }

        /// <summary>
        /// Registry of inspectors that deal with server-side messages
        /// </summary>
        OrderedRegistry<IServerMsgInspector> ServerMsgInspectors { get; }



        /// <summary>
        /// Specifies default ms timout for call dispatch only
        /// </summary>
        int DefaultDispatchTimeoutMs { get; set;}

        /// <summary>
        /// Specified default ms timeout for the calls
        /// </summary>
        int DefaultTimeoutMs { get; set;}


        /// <summary>
        /// Determines how much information should be logged about client-side operations
        /// </summary>
        Azos.Log.MessageType ClientLogLevel { get;  set;}

        /// <summary>
        /// Determines how much information should be logged about server-side operations
        /// </summary>
        Azos.Log.MessageType ServerLogLevel { get; set; }

        /// <summary>
        /// Specifies ms timout for non-threadsafe server instance lock
        /// </summary>
        int ServerInstanceLockTimeoutMs { get; set;}

    }

    public interface IGlueImplementation : IGlue, IDisposable, IConfigurable, IInstrumentable
    {
        void RegisterProvider(Provider p);
        void UnregisterProvider(Provider p);

        void RegisterBinding(Binding b);
        void UnregisterBinding(Binding b);

        void RegisterServerEndpoint(ServerEndPoint ep);
        void UnregisterServerEndpoint(ServerEndPoint ep);


        RequestMsg ClientDispatchingRequest(ClientEndPoint client, RequestMsg request);
        void ClientDispatchedRequest(ClientEndPoint client, RequestMsg request, CallSlot callSlot);

        void ClientDeliverAsyncResponse(ResponseMsg response);

        /// <summary>
        /// Asynchronously dispatch client request
        /// </summary>
        void ServerDispatchRequest(RequestMsg request);

        /// <summary>
        /// Handle client request synchronously
        /// </summary>
        ResponseMsg ServerHandleRequest(RequestMsg request);

        /// <summary>
        /// Handle failure of client request synchronously
        /// </summary>
        ResponseMsg ServerHandleRequestFailure(FID reqID, bool oneWay, Exception failure, object bindingSpecCtx);


        IConfigSectionNode GlueConfiguration { get; }
        IConfigSectionNode ProvidersConfigurationSection { get; }
        IEnumerable<IConfigSectionNode> ProviderConfigurations { get; }
        IConfigSectionNode BindingsConfigurationSection { get; }
        IEnumerable<IConfigSectionNode> BindingConfigurations { get; }
        IConfigSectionNode ServersConfigurationSection { get; }
        IEnumerable<IConfigSectionNode> ServerConfigurations { get; }
    }
}
