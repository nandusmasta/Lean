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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// WaveTrend Oscillator is a momentum indicator that identifies overbought and oversold
    /// conditions by using weighted moving averages and volatility. The oscillator consists
    /// of two lines: the Wave Trend (tci) and a smoothed average (wt2).
    /// </summary>
    public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _channelPeriod;
        private readonly int _averagePeriod;
        private readonly int _smoothPeriod;
        private readonly decimal _channelMultiplier;

        private readonly ExponentialMovingAverage _exponentialSmoothedAverage;
        private readonly ExponentialMovingAverage _absoluteDeviation;
        private readonly ExponentialMovingAverage _channelIndex;
        private readonly SimpleMovingAverage _smoothedWaveTrend;

        /// <summary>
        /// Gets the WaveTrend (TCI) indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> WaveTrend { get; }

        /// <summary>
        /// Gets the WaveTrend smooth average (WT2) indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> WaveTrendSmooth => _smoothedWaveTrend;

        /// <summary>
        /// Indicates whether the indicator has enough data to be calculated.
        /// </summary>
        public override bool IsReady => _exponentialSmoothedAverage.IsReady && _absoluteDeviation.IsReady && _channelIndex.IsReady && _smoothedWaveTrend.IsReady;

        /// <summary>
        /// Required period for the indicator to have enough data to work
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new WaveTrendOscillator indicator with the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="channelPeriod">The period for the channel calculations</param>
        /// <param name="averagePeriod">The period for the average calculations</param>
        /// <param name="smoothPeriod">The period for smoothing the WT2 line</param>
        /// <param name="channelMultiplier">The multiplier for the channel width</param>
        public WaveTrendOscillator(string name, int channelPeriod = 10, int averagePeriod = 21,
        int smoothPeriod = 4, decimal channelMultiplier = 0.015m)
        : base(name)
        {
            _channelPeriod = channelPeriod;
            _averagePeriod = averagePeriod;
            _smoothPeriod = smoothPeriod;
            _channelMultiplier = channelMultiplier;

            _exponentialSmoothedAverage = new ExponentialMovingAverage(channelPeriod);
            _absoluteDeviation = new ExponentialMovingAverage(channelPeriod);
            _channelIndex = new ExponentialMovingAverage(averagePeriod);
            WaveTrend = _channelIndex;
            _smoothedWaveTrend = new SimpleMovingAverage(smoothPeriod);

            WarmUpPeriod = channelPeriod + averagePeriod + smoothPeriod;
        }

        /// <summary>
        /// Creates a new WaveTrendOscillator indicator with the specified parameters
        /// </summary>
        /// <param name="channelPeriod">The period for the channel calculations</param>
        /// <param name="averagePeriod">The period for the average calculations</param>
        /// <param name="smoothPeriod">The period for smoothing the WT2 line</param>
        /// <param name="channelMultiplier">The multiplier for the channel width</param>
        public WaveTrendOscillator(int channelPeriod = 10, int averagePeriod = 21,
        int smoothPeriod = 4, decimal channelMultiplier = 0.015m)
        : this($"WTO({channelPeriod},{averagePeriod},{smoothPeriod})",
              channelPeriod, averagePeriod, smoothPeriod, channelMultiplier)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input value for this indicator</param>
        /// <returns>The computed value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var typicalPrice = (input.High + input.Low + input.Close) / 3m;

            if (!_exponentialSmoothedAverage.Update(input.Time, typicalPrice))
            {
                return 0m;
            }

            var absoluteDistance = Math.Abs(typicalPrice - _exponentialSmoothedAverage.Current.Value);
            if (!_absoluteDeviation.Update(input.Time, absoluteDistance))
            {
                return 0m;
            }

            decimal channelIndexValue = 0m;
            if (_absoluteDeviation.Current.Value != 0m)
            {
                channelIndexValue = (typicalPrice - _exponentialSmoothedAverage.Current.Value) /
                                  (_channelMultiplier * _absoluteDeviation.Current.Value);
            }

            _channelIndex.Update(input.Time, channelIndexValue);
            _smoothedWaveTrend.Update(input.Time, _channelIndex.Current.Value);

            return _channelIndex.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _exponentialSmoothedAverage.Reset();
            _absoluteDeviation.Reset();
            _channelIndex.Reset();
            _smoothedWaveTrend.Reset();
            base.Reset();
        }
    }
}
