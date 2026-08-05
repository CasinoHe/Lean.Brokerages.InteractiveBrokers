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
using System.Collections.Concurrent;
using System.Collections.Generic;

using NUnit.Framework;

using QuantConnect;
using QuantConnect.Brokerages.InteractiveBrokers;
using IB = QuantConnect.Brokerages.InteractiveBrokers.Client;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    public class InteractiveBrokersBrokerageHelpersTests
    {
        [Test]
        public void GetsNextSunday()
        {
            var baseDate = new DateTime(2022, 12, 5); // Monday
            Assert.AreEqual(DayOfWeek.Monday, baseDate.DayOfWeek);
            var expectedNextSunday = new DateTime(2022, 12, 11); // Sunday
            Assert.AreEqual(DayOfWeek.Sunday, expectedNextSunday.DayOfWeek);

            for (var i = 0; i < 7; i++)
            {
                var date = baseDate.AddDays(i);
                var nextSunday = InteractiveBrokersBrokerage.GetNextSundayFromDate(date);

                Assert.AreEqual(expectedNextSunday, nextSunday);
            }
        }


        [TestCaseSource(nameof(StartDatesToComputeRestartDelay))]
        public void CalculatesTheWeeklyRestartDelay(DateTime currentDate, DateTime expectedSunday)
        {
            var restartTimeOfDay = new TimeSpan(9, 30, 0);
            var time = InteractiveBrokersBrokerage.ComputeNextWeeklyRestartTimeUtc(restartTimeOfDay, currentDate);

            var expectedTime = expectedSunday.Date.Add(restartTimeOfDay);

            Assert.AreEqual(expectedTime, time);
        }

        [TestCaseSource(nameof(EuropeanEquityMarkets))]
        public void EuropeanPrimaryListingCanSubscribeAndKeepsVenueIdentity(
            string market,
            string expectedPrimaryExchange)
        {
            var symbol = Symbol.Create("SAN", SecurityType.Equity, market);

            Assert.IsTrue(InteractiveBrokersBrokerage.CanSubscribe(symbol));
            Assert.AreEqual(
                expectedPrimaryExchange,
                InteractiveBrokersBrokerage.GetPrimaryExchangeForMarket(market));
        }

        [TestCaseSource(nameof(EuropeanEquityMarkets))]
        public void EuropeanPrimaryExchangeRoundTripsToLeanMarket(
            string expectedMarket,
            string primaryExchange)
        {
            Assert.AreEqual(
                expectedMarket,
                InteractiveBrokersBrokerage.GetMarketForPrimaryExchange(primaryExchange));
        }

        [TestCase("BM", Market.XMAD)]
        [TestCase("SBF", Market.XPAR)]
        public void IncomingDuplicateTickerPreservesPrimaryVenue(
            string primaryExchange,
            string expectedMarket)
        {
            var contract = new IBApi.Contract
            {
                Symbol = "SAN",
                SecType = IB.SecurityType.Stock,
                PrimaryExch = primaryExchange
            };

            Assert.AreEqual(
                expectedMarket,
                InteractiveBrokersBrokerage.GetMarketForContract(contract, SecurityType.Equity));
        }

        [TestCase(Market.XPAR, "SAN", "SAN1")]
        [TestCase(Market.XHEL, "NDA-FI", "NDA FI")]
        public void EuropeanEquityBrokerAliasesRoundTrip(
            string market,
            string leanTicker,
            string brokerageTicker)
        {
            Assert.AreEqual(
                brokerageTicker,
                InteractiveBrokersBrokerage.GetBrokerageEquitySymbol(market, leanTicker));
            Assert.AreEqual(
                leanTicker,
                InteractiveBrokersBrokerage.GetLeanEquitySymbol(market, brokerageTicker));
        }

        [Test]
        public void ContractCacheKeyIncludesPrimaryExchange()
        {
            var madrid = new IBApi.Contract
            {
                Symbol = "SAN",
                SecType = IB.SecurityType.Stock,
                Exchange = "SMART",
                Currency = "EUR",
                PrimaryExch = "BM"
            };
            var paris = new IBApi.Contract
            {
                Symbol = "SAN",
                SecType = IB.SecurityType.Stock,
                Exchange = "SMART",
                Currency = "EUR",
                PrimaryExch = "SBF"
            };

            Assert.AreNotEqual(
                InteractiveBrokersBrokerage.GetUniqueKey(madrid),
                InteractiveBrokersBrokerage.GetUniqueKey(paris));
        }

        [Test]
        public void UniqueResolvedContractIsCachedUnderOriginalRequestKey()
        {
            var request = new IBApi.Contract
            {
                Symbol = "AAPL",
                SecType = IB.SecurityType.Stock,
                Exchange = "SMART",
                Currency = "USD"
            };
            var resolvedDetails = new IBApi.ContractDetails
            {
                Contract = new IBApi.Contract
                {
                    Symbol = "AAPL",
                    SecType = IB.SecurityType.Stock,
                    Exchange = "SMART",
                    Currency = "USD",
                    PrimaryExch = "NASDAQ"
                }
            };
            var cache = new ConcurrentDictionary<string, IBApi.ContractDetails>();

            InteractiveBrokersBrokerage.CacheContractDetailsRequestAlias(
                cache,
                request,
                new[] { resolvedDetails },
                requestCompletedSuccessfully: true);

            Assert.AreSame(
                resolvedDetails,
                cache[InteractiveBrokersBrokerage.GetUniqueKey(request)]);
        }

        [Test]
        public void AmbiguousContractDetailsAreNotCachedUnderOriginalRequestKey()
        {
            var request = new IBApi.Contract
            {
                Symbol = "SAN",
                SecType = IB.SecurityType.Stock,
                Exchange = "SMART",
                Currency = "EUR"
            };
            var cache = new ConcurrentDictionary<string, IBApi.ContractDetails>();
            var matches = new[]
            {
                new IBApi.ContractDetails
                {
                    Contract = new IBApi.Contract
                    {
                        Symbol = "SAN",
                        SecType = IB.SecurityType.Stock,
                        Exchange = "SMART",
                        Currency = "EUR",
                        PrimaryExch = "BM"
                    }
                },
                new IBApi.ContractDetails
                {
                    Contract = new IBApi.Contract
                    {
                        Symbol = "SAN",
                        SecType = IB.SecurityType.Stock,
                        Exchange = "SMART",
                        Currency = "EUR",
                        PrimaryExch = "SBF"
                    }
                }
            };

            InteractiveBrokersBrokerage.CacheContractDetailsRequestAlias(
                cache,
                request,
                matches,
                requestCompletedSuccessfully: true);

            Assert.IsFalse(cache.ContainsKey(InteractiveBrokersBrokerage.GetUniqueKey(request)));
        }

        [Test]
        public void PartialContractDetailsBeforeErrorAreNotCachedUnderOriginalRequestKey()
        {
            AssertIncompleteContractDetailsAreNotCachedUnderOriginalRequestKey();
        }

        [Test]
        public void PartialContractDetailsBeforeTimeoutAreNotCachedUnderOriginalRequestKey()
        {
            AssertIncompleteContractDetailsAreNotCachedUnderOriginalRequestKey();
        }

        [Test]
        public void UnsupportedEquityMarketIsRejected()
        {
            var symbol = Symbol.Create("SAN", SecurityType.Equity, Market.CBOE);

            Assert.IsFalse(InteractiveBrokersBrokerage.CanSubscribe(symbol));
            Assert.IsNull(InteractiveBrokersBrokerage.GetPrimaryExchangeForMarket(symbol.ID.Market));
        }

        private static IEnumerable<TestCaseData> EuropeanEquityMarkets()
        {
            yield return new TestCaseData(Market.XAMS, "AEB");
            yield return new TestCaseData(Market.XBRU, "ENEXT");
            yield return new TestCaseData(Market.XETR, "IBIS");
            yield return new TestCaseData(Market.XHEL, "HEX");
            yield return new TestCaseData(Market.XMAD, "BM");
            yield return new TestCaseData(Market.XMIL, "BVME");
            yield return new TestCaseData(Market.XPAR, "SBF");
        }

        private static void AssertIncompleteContractDetailsAreNotCachedUnderOriginalRequestKey()
        {
            var request = new IBApi.Contract
            {
                Symbol = "SAN",
                SecType = IB.SecurityType.Stock,
                Exchange = "SMART",
                Currency = "EUR"
            };
            var partialDetails = new IBApi.ContractDetails
            {
                Contract = new IBApi.Contract
                {
                    Symbol = "SAN",
                    SecType = IB.SecurityType.Stock,
                    Exchange = "SMART",
                    Currency = "EUR",
                    PrimaryExch = "BM"
                }
            };
            var cache = new ConcurrentDictionary<string, IBApi.ContractDetails>();

            InteractiveBrokersBrokerage.CacheContractDetailsRequestAlias(
                cache,
                request,
                new[] { partialDetails },
                requestCompletedSuccessfully: false);

            Assert.IsFalse(cache.ContainsKey(InteractiveBrokersBrokerage.GetUniqueKey(request)));
        }

        // (start date, next Sunday)
        private static TestCaseData[] StartDatesToComputeRestartDelay => new[]
        {
            // Start on Monday
            new TestCaseData(
                new DateTime(2022, 8, 29, 12, 30, 45), // Monday
                new DateTime(2022, 9, 4)),   // Next Sunday
            // Sunday
            new TestCaseData(
                new DateTime(2022, 12, 4, 12, 30, 25),  // Sunday
                new DateTime(2022, 12, 4))   // Same Sunday
        };
    }
}
