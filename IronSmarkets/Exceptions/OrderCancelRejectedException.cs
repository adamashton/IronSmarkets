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

using System;
using System.Collections;
using System.Collections.Generic;

namespace IronSmarkets.Exceptions
{
    public class OrderCancelRejectedException : Exception
    {
        private static readonly IDictionary<Proto.Seto.OrderCancelRejectedReason, string> Messages =
            new Dictionary<Proto.Seto.OrderCancelRejectedReason, string> {
            { Proto.Seto.OrderCancelRejectedReason.ORDERCANCELREJECTEDNOTFOUND, "Not found" },
            { Proto.Seto.OrderCancelRejectedReason.ORDERCANCELREJECTEDNOTLIVE, "Not live" }
        };

        private readonly string _errorMessage;

        public string ErrorMessage { get { return _errorMessage; } }

        private OrderCancelRejectedException(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public override string Message {
            get {
                return _errorMessage;
            }
        }

        public override IDictionary Data {
            get {
                return new Dictionary<string, object> {
                    { "ErrorMessage", ErrorMessage }
                };
            }
        }

        internal static OrderCancelRejectedException FromSeto(Proto.Seto.OrderCancelRejected seto)
        {
            return new OrderCancelRejectedException(Messages[seto.Reason]);
        }
    }
}
