/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    public class InteractiveBrokersBrokerageCancellationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void WaitForCancellationResponseReportsWhetherBrokerageResponded(bool signalResponse)
        {
            using var response = new ManualResetEventSlim(signalResponse);

            var received = InteractiveBrokersBrokerage.WaitForCancellationResponse(
                response,
                TimeSpan.FromMilliseconds(20));

            Assert.AreEqual(signalResponse, received);
        }
    }
}
