# SocketNetwork
Offers generic simplified implementation of networking based on sockets. 

It handles all of the internal socket communications while allowing you to apply your own data serialization.

## ISocketEventHandler
<code>ISocketEventHandler</code> is used both by Server and Client to acquire <code>SocketEventArgs</code> object for <code>AcceptAsync</code>, <code>ConnectAsync</code>, <code>ReceiveAsync</code>, <code>SendAsync</code>, and <code>DisconnectAsync</code> operations.

You can use the default handler <code>SocketEventManager</code> which internally manages a Pool of <code>SocketEventArgs</code> that dynamically expend in size on-demend and re-use.

You can also use your own handler by implementing <code>ISocketEventHandler</code> in your class.

## Data Serialization and Deserialization
When data is received or sent on <code>SocketClient</code> object, the data is being serialized using the <code>SerializationHandler</code> property, which means you must implement <code>INetworkMessageSerializationHandler</code> to transform the data to the expected type.

## Server Side
Implementing server side requires of you to create a new instace of <code>SocketServer</code> or inherit from it in your own class, while calling <code>SocketServer.Start()</code> to start processing connections.

Managing new incoming connections is done via <code>ServerHandler</code> property so make sure to implement and assign it to apply your own logics.

It is mandatory to set <code>EventHandler</code> property by implementing <code>ISocketEventHandler</code>.

For in-depth look and example for a full server side implementation, you can take a look on the example project <code>Example.Server</code>.

## Client Side
Implementing client side requires of you to create a new instace of <code>SocketClient</code> or inherit from it in your own class, while calling <code>SocketClient.ConnectAsync()</code> to attemp to connect <code>SocketServer</code> instances.

Managing connection events or received data is done via <code>ClientHandler</code> property so make sure to implement and assign it to apply your own logics.

It is mandatory to set <code>EventHandler</code> property by implementing <code>ISocketEventHandler</code>.

For in-depth look and example for a full client side implementation, you can take a look on the example project <code>Example.Client</code>.

## Example Project
In the example projects you can check our how implementations of a communication protocol and packets obfuscation, for the purpose of a simple Chatting application.

# Nuget package
Install-Package RabanSoft.SocketNetwork

or

dotnet add package RabanSoft.SocketNetwork

or

https://www.nuget.org/packages/RabanSoft.SocketNetwork