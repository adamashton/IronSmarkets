// Copyright (c) 2012 Smarkets Limited
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using log4net;

namespace IronSmarkets.Clients
{
#if NET40
    internal interface IRpcHandler<out TResponse, in TState>
#else
    internal interface IRpcHandler<TResponse, TState>
#endif
    {
        IResponse<TResponse> BeginRequest(Proto.Seto.Payload payload, TState state);
        void Handle(Proto.Seto.Payload payload);
    }

    internal abstract class RpcHandler<TPayload, TResponse, TState> : IRpcHandler<TResponse, TState>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RpcHandler<TPayload, TResponse, TState>));

        private readonly object _lock = new object();

        protected readonly ISmarketsClient Client;

        protected RpcHandler(ISmarketsClient client)
        {
            Client = client;
        }

        public IResponse<TResponse> BeginRequest(Proto.Seto.Payload payload, TState state)
        {
            lock (_lock)
            {
                // XXX: At the moment, SendPayload needs to be inside
                // the lock because the receiver thread could
                // theoretically receive the payload before the
                // SyncRequest is added to the dictionary. Although
                // very unlikely, I am opting for safer in this case
                // (as well as other cases which exhibit the same
                // pattern). It may make sense instead to track the
                // last sequence number for these types of requests in
                // a volatile long variable and spin-wait in the
                // receiver where necessary.
                Client.SendPayload(payload);
                var req = NewRequest(payload.EtoPayload.Seq, state);
                AddRequest(payload, req);
                return req;
            }
        }

        public void Handle(Proto.Seto.Payload payload)
        {
            SyncRequest<TPayload, TResponse, TState> req;
            lock (_lock)
            {
                req = GetRequest(payload);
            }
            if (req != null)
                Extract(req, payload);
            else
            {
                Log.Warn(
                    "Received payload " +
                    "but could not find original request");
            }
        }

        protected abstract void Extract(SyncRequest<TPayload, TResponse, TState> request, Proto.Seto.Payload payload);
        protected abstract void AddRequest(Proto.Seto.Payload payload, SyncRequest<TPayload, TResponse, TState> request);
        protected abstract SyncRequest<TPayload, TResponse, TState> GetRequest(Proto.Seto.Payload payload);
        protected abstract SyncRequest<TPayload, TResponse, TState> NewRequest(ulong sequence, TState state);
    }
}
