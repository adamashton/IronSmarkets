// Copyright (c) 2011-2012 Smarkets Limited
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
using System.Linq;

namespace IronSmarkets.Data
{
    public class MarketInfo : IEquatable<MarketInfo>
    {
        private readonly Uid _uid;
        private readonly string _slug;
        private readonly string _name;
        private readonly string _shortname;
        private readonly DateTime? _startDateTime;
        private readonly DateTime? _endDateTime;
        private readonly IEnumerable<KeyValuePair<Uid, string>> _entities;
        private readonly IEnumerable<ContractInfo> _contracts;

        public Uid Uid { get { return _uid; } }
        public string Slug { get { return _slug; } }
        public string Name { get { return _name; } }
        public string Shortname { get { return _shortname; } }
        public DateTime? StartDateTime { get { return _startDateTime; } }
        public DateTime? EndDateTime { get { return _endDateTime; } }
        public IEnumerable<KeyValuePair<Uid, string>> Entities { get { return _entities; } }
        public IEnumerable<ContractInfo> Contracts { get { return _contracts; } }

        private MarketInfo(
            Uid uid,
            string slug,
            string name,
            string shortname,
            DateTime? startDateTime,
            DateTime? endDateTime,
            IEnumerable<KeyValuePair<Uid, string>> entities,
            IEnumerable<ContractInfo> contracts)
        {
            _uid = uid;
            _slug = slug;
            _name = name;
            _shortname = shortname;
            _startDateTime = startDateTime;
            _endDateTime = endDateTime;
            _entities = entities;
            _contracts = contracts;
        }

        internal static MarketInfo FromSeto(Proto.Seto.MarketInfo info)
        {
            return new MarketInfo(
                Uid.FromUuid128(info.Market),
                info.Slug,
                info.Name,
                info.Shortname,
                SetoMap.FromDateTime(info.StartDate, info.StartTime),
                SetoMap.FromDateTime(info.EndDate, info.EndTime),
                EntityRelationships.FromEntities(info.Entities),
                info.Contracts.Select(ContractInfo.FromSeto));
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            bool result = obj is MarketInfo;
            if (result)
            {
                MarketInfo other = (MarketInfo)obj;
                result = Equals(other);
            }
            return result;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Uid.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(MarketInfo other)
        {
            return this.Uid == other.Uid;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("MarketInfo [{0}]: {1}", this.Uid, this.Name);
        }
    }
}
