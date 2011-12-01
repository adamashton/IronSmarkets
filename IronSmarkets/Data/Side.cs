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

namespace IronSmarkets.Data
{
    public struct Side : IEquatable<Side>
    {
        public static readonly Side Buy  = new Side("buy");
        public static readonly Side Sell = new Side("sell");

        private readonly string _side;

        private Side(string side)
        {
            _side = side;
        }

        public override int GetHashCode()
        {
            return _side.GetHashCode();
        }

        public override bool Equals(object right)
        {
            if (ReferenceEquals(right, null))
                return false;

            if (GetType() != right.GetType())
                return false;

            return Equals((Side)right);
        }

        public bool Equals(Side other)
        {
            return _side == other._side;
        }

        public override string ToString()
        {
            return _side;
        }

        internal Proto.Seto.Side ToSeto()
        {
            return _side == "buy" ? Proto.Seto.Side.SIDEBUY : Proto.Seto.Side.SIDESELL;
        }

        public static bool operator==(Side left, Side right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(Side left, Side right)
        {
            return !left.Equals(right);
        }
    }
}
