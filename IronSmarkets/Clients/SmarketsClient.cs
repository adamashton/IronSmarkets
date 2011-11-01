// Copyright (c) 2011 Smarkets Limited
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

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;

using log4net;
using ProtoBuf;

using IronSmarkets.Data;
using IronSmarkets.Events;
using IronSmarkets.Exceptions;
using IronSmarkets.Proto.Seto;
using IronSmarkets.Sessions;

using Eto = IronSmarkets.Proto.Eto;

namespace IronSmarkets.Clients
{
    public interface ISmarketsClient :
        IDisposable,
        IPayloadEvents<Payload>,
        IPayloadEndpoint<Payload>
    {
        bool IsDisposed { get; }

        ulong Login();
        ulong Logout();

        ulong Ping();

        ulong SubscribeMarket(Uuid market);
        ulong UnsubscribeMarket(Uuid market);

        ulong RequestOrdersForAccount();
        ulong RequestOrdersForMarket(Uuid market);

        IEventMap RequestEvents(EventQuery query);
        Data.Account GetAccountState();
        Data.Account GetAccountState(Uuid account);
    }

    internal sealed class SyncRequest<T>
    {
        private readonly ManualResetEvent _replied =
            new ManualResetEvent(false);

        private T _response;
        private Exception _responseException;

        public T Response {
            get
            {
                _replied.WaitOne();
                if (_responseException != null)
                {
                    throw _responseException;
                }
                return _response;
            }
            set
            {
                _response = value;
                _replied.Set();
            }
        }

        public void SetException(Exception exception)
        {
            _responseException = exception;
            _replied.Set();
        }
    }

    public sealed class SmarketsClient : ISmarketsClient
    {
        private static readonly ILog Log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IClientSettings _settings;
        private readonly ISession<Payload> _session;

        private int _disposed;
        private readonly Receiver<Payload> _receiver;

        private readonly IDictionary<ulong, SyncRequest<Proto.Seto.Events>> _eventsRequests =
            new Dictionary<ulong, SyncRequest<Proto.Seto.Events>>();
        private readonly IDictionary<ulong, SyncRequest<Proto.Seto.AccountState>> _accountRequests =
            new Dictionary<ulong, SyncRequest<Proto.Seto.AccountState>>();

        private SmarketsClient(IClientSettings settings)
        {
            _settings = settings;
            _session = new SeqSession(
                _settings.SocketSettings,
                _settings.SessionSettings);
            _session.PayloadReceived += (sender, args) =>
                OnPayloadReceived(args.Payload);
            _session.PayloadSent += (sender, args) =>
                OnPayloadSent(args.Payload);
            _receiver = new Receiver<Payload>(_session);
        }

        public static ISmarketsClient Create(IClientSettings settings)
        {
            return new SmarketsClient(settings);
        }

        public bool IsDisposed
        {
            get
            {
                return Thread.VolatileRead(ref _disposed) == 1;
            }
        }

        public event EventHandler<PayloadReceivedEventArgs<Payload>> PayloadReceived;
        public event EventHandler<PayloadReceivedEventArgs<Payload>> PayloadSent;

        ~SmarketsClient()
        {
            Dispose(false);
        }

        public void AddPayloadHandler(Predicate<Payload> predicate)
        {
            _session.AddPayloadHandler(predicate);
        }

        public void RemovePayloadHandler(Predicate<Payload> predicate)
        {
            _session.RemovePayloadHandler(predicate);
        }

        public void SendPayload(Payload payload)
        {
            _session.SendPayload(payload);
        }

        public ulong Login()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called Logout on disposed object");

            ulong seq = _session.Login();
            _receiver.Start();
            return seq;
        }

        public ulong Logout()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called Logout on disposed object");

            ulong seq = _session.Logout();
            _receiver.Stop();
            return seq;
        }

        public ulong Ping()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called Ping on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADETO,
                EtoPayload = new Eto.Payload {
                    Type = Eto.PayloadType.PAYLOADPING
                }
            };

            SendPayload(payload);
            return payload.EtoPayload.Seq;
        }

        public ulong SubscribeMarket(Uuid market)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called SubscribeMarket on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADMARKETSUBSCRIBE,
                MarketSubscribe = new MarketSubscribe {
                    Market = market.ToUuid128()
                }
            };

            SendPayload(payload);
            return payload.EtoPayload.Seq;
        }

        public ulong UnsubscribeMarket(Uuid market)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called UnsubscribeMarket on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADMARKETUNSUBSCRIBE,
                MarketUnsubscribe = new MarketUnsubscribe {
                    Market = market.ToUuid128()
                }
            };

            SendPayload(payload);
            return payload.EtoPayload.Seq;
        }

        public ulong RequestOrdersForMarket(Uuid market)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called RequestOrdersForMarket on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADORDERSFORMARKETREQUEST,
                OrdersForMarketRequest = new OrdersForMarketRequest {
                    Market = market.ToUuid128()
                }
            };

            SendPayload(payload);
            return payload.EtoPayload.Seq;
        }

        public ulong RequestOrdersForAccount()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called RequestOrdersForAccount on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADORDERSFORACCOUNTREQUEST,
                OrdersForAccountRequest = new OrdersForAccountRequest()
            };

            SendPayload(payload);
            return payload.EtoPayload.Seq;
        }

        public IEventMap RequestEvents(EventQuery query)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called RequestEvents on disposed object");

            var payload = new Payload {
                Type = PayloadType.PAYLOADEVENTSREQUEST,
                EventsRequest = query.ToEventsRequest()
            };

            SendPayload(payload);
            var seq = payload.EtoPayload.Seq;
            var req = new SyncRequest<Proto.Seto.Events>();
            _eventsRequests[seq] = req;
            return EventMap.FromSeto(req.Response);
        }

        public Data.Account GetAccountState()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called GetAccount on disposed object");

            return GetAccountState(new AccountStateRequest());
        }

        public Data.Account GetAccountState(Uuid account)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(
                    "SmarketsClient",
                    "Called GetAccount on disposed object");

            return GetAccountState(
                new AccountStateRequest {
                    Account = account.ToUuid128()
                });
        }

        private Data.Account GetAccountState(AccountStateRequest request)
        {
            var payload = new Payload {
                Type = PayloadType.PAYLOADACCOUNTSTATEREQUEST,
                AccountStateRequest = request
            };
            SendPayload(payload);
            var seq = payload.EtoPayload.Seq;
            var req = new SyncRequest<Proto.Seto.AccountState>();
            _accountRequests[seq] = req;
            return Data.Account.FromSeto(req.Response);
        }

        private void OnPayloadReceived(Payload payload)
        {
            EventHandler<PayloadReceivedEventArgs<Payload>> ev = PayloadReceived;
            if (ev != null)
                ev(this, new PayloadReceivedEventArgs<Payload>(
                       payload.EtoPayload.Seq,
                       payload));
            if (payload.Type == PayloadType.PAYLOADHTTPFOUND)
            {
                HandleEventsHttpFound(payload);
            }

            if (payload.Type == PayloadType.PAYLOADACCOUNTSTATE)
            {
                HandleAccountState(payload);
            }
        }

        private void OnPayloadSent(Payload payload)
        {
            EventHandler<PayloadReceivedEventArgs<Payload>> ev = PayloadSent;
            if (ev != null)
                ev(this, new PayloadReceivedEventArgs<Payload>(
                       payload.EtoPayload.Seq,
                       payload));
        }

        private void HandleAccountState(Payload payload)
        {
            SyncRequest<Proto.Seto.AccountState> req;
            if (_accountRequests.TryGetValue(
                    payload.EtoPayload.Seq,
                    out req)) {
                req.Response = payload.AccountState;
            }
            else
            {
                Log.Warn(
                    "Received ACCOUNT_STATE payload " +
                    "but could find original request");
            }
        }

        private void HandleEventsHttpFound(Payload payload)
        {
            SyncRequest<Proto.Seto.Events> req;
            if (_eventsRequests.TryGetValue(
                    payload.EtoPayload.Seq,
                    out req)) {
                BeginFetchHttpFound(req, payload);
            }
            else
            {
                Log.Warn(
                    "Received HTTP_FOUND payload " +
                    "but could find original request");
            }
        }

        private IAsyncResult BeginFetchHttpFound<T>(
            SyncRequest<T> syncRequest, Payload payload)
        {
            var url = payload.HttpFound.Url;
            if (Log.IsDebugEnabled) Log.Debug(
                string.Format("Fetching payload from URL {0}", url));
            var req = (HttpWebRequest)WebRequest.Create(url);
            var state = new Tuple<HttpWebRequest, SyncRequest<T>>(
                req, syncRequest);
            var result = req.BeginGetResponse(
                FetchHttpFoundCallback<T>, state);
            ThreadPool.RegisterWaitForSingleObject(
                result.AsyncWaitHandle,
                HttpFoundTimeoutCallback<T>,
                state, _settings.HttpRequestTimeout, true);
            return result;
        }

        private void FetchHttpFoundCallback<T>(IAsyncResult result)
        {
            var stateTuple = (Tuple<HttpWebRequest, SyncRequest<T>>)result.AsyncState;
            if (Log.IsDebugEnabled) Log.Debug(
                string.Format("Web request callback for URL {0}", stateTuple.Item1.RequestUri));
            try
            {
                using (var resp = stateTuple.Item1.EndGetResponse(result))
                {
                    if (Log.IsDebugEnabled) Log.Debug("Received a response, deserializing");
                    Stream receiveStream = resp.GetResponseStream();
                    stateTuple.Item2.Response = Serializer.Deserialize<T>(receiveStream);
                }
            }
            catch (WebException wex)
            {
                if (wex.Message == "Aborted.")
                {
                    stateTuple.Item2.SetException(
                        new RequestTimedOutException(_settings.HttpRequestTimeout));
                }
                else
                {
                    stateTuple.Item2.SetException(wex);
                }
            }
            catch (Exception ex)
            {
                stateTuple.Item2.SetException(ex);
            }
        }

        private static void HttpFoundTimeoutCallback<T>(object state, bool timedOut)
        {
            if (timedOut)
            {
                var stateTuple = (Tuple<HttpWebRequest, SyncRequest<T>>)state;
                stateTuple.Item1.Abort();
            }
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    var dispSession = _session as IDisposable;
                    if (dispSession != null)
                        dispSession.Dispose();
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    internal sealed class Receiver<T> where T : IPayload
    {
        private static readonly ILog Log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Thread _loop;
        private readonly ISession<T> _session;

        private volatile bool _complete;

        public Receiver(ISession<T> session)
        {
            _session = session;
            _loop = new Thread(Loop) {
                Name = "receiver"
            };
        }

        public void Start()
        {
            _complete = false;
            _loop.Start();
        }

        public void Stop()
        {
            _complete = true;
            _loop.Join();
        }

        private void Loop()
        {
            while (!_complete)
            {
                try
                {
                    var payload = _session.Receive();
                    if (payload.IsLogoutConfirmation())
                    {
                        Log.Info(
                            "Received a logout confirmation; " +
                            "assuming socket will close");
                        _complete = true;
                    }
                }
                catch (IOException ex)
                {
                    Log.Warn("Receiving socket timed out", ex);
                }
            }
        }
    }
}
