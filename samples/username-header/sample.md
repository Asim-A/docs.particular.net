---
title: Appending username using headers
summary: Using message headers to append the current username to every message.
reviewed: 2019-11-19
component: Core
related:
- nservicebus/pipeline/message-mutators
- nservicebus/messaging/headers
---

This sample demonstrates how to append the current username to the outgoing messages and how to extract that value when messages are handled. The current principal is made available by using a principal accessor registered through dependency injection.

NOTE: This sample doesn't use `Thread.CurrentPrincipal` because of the behavior of `Thread.CurrentPrincipal` in combination with asynchronous code is dependent on the framework version the code is executed on. For more information, refer to the excellent guideline [Migrate from ClaimsPrincipal.Current of ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/migration/claimsprincipal-current).

### Fake principal

For demonstration purposes, before sending a message, the `principalAccessor.CurrentPrincipal` is replaced with a new instance. In a production scenario, the `principalAccessor.CurrentPrincipal` would be either the impersonated user from the IIS or the current user sending a message.

snippet: send-message

The snippet above uses two concurrent sends to demonstrate how the current principle is properly propagated into the message session.

## Custom header with a mutator

The recommended approach for capturing the current user is to create a transport mutator that extracts the current identity and then adds it to the header collection of every outgoing message.

### Outgoing message mutator

The outgoing mutator extracts `principalAccessor.CurrentPrincipal.Identity.Name` and adds it to the message headers.

snippet: username-header-mutator

#### Register the outgoing message mutator

snippet: component-registration-sender

### Incoming message mutator

The incoming mutator extracts the username header from the message and sets the `principalAccessor.CurrentPrincipal`.

snippet: set-principal-from-header-mutator

#### Register the incoming message mutator

snippet: component-registration-receiver

This sample doesn't register the outgoing message mutator for the receiver. If desired, the outgoing message mutator could be registered on the receiver as well, which would automatically add the username header to all messages sent.

### The Handler

From within a handler (or saga), this value can be used as follows:

snippet: handler-using-custom-header
