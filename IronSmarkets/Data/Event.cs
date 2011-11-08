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

namespace IronSmarkets.Data
{
    public class Event
    {
        private readonly EventInfo _info;
        private readonly IMarketMap _markets;
        private readonly List<Event> _children =
            new List<Event>();

        public EventInfo Info { get { return _info; } }
        public IMarketMap Markets { get { return _markets; } }
        public ICollection<Event> Children { get { return _children.AsReadOnly(); } }

        // Optional
        public Event Parent { get; set; }

        private Event(EventInfo info, IMarketMap markets)
        {
            _info = info;
            _markets = markets;
        }

        public void AddChild(Event child)
        {
            _children.Add(child);
        }

        internal static Event FromSeto(Proto.Seto.EventInfo info)
        {
            return new Event(
                EventInfo.FromSeto(info),
                MarketMap.FromMarkets(info.Markets));
        }
    }
}