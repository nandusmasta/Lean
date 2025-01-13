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

        private readonly ExponentialMovingAverage _esa;
        private readonly ExponentialMovingAverage _d;
        private readonly ExponentialMovingAverage _ci;
        private readonly SimpleMovingAverage _wt2;

        /// <summary>
        /// Gets the WaveTrend (TCI) indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> WaveTrend { get; }

        /// <summary>
        /// Gets the WaveTrend smooth average (WT2) indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> WaveTrendSmooth => _wt2;

        /// <summary>
        /// Indicates whether the indicator has enough data to be calculated.
        /// </summary>
        public override bool IsReady => _esa.IsReady && _d.IsReady && _ci.IsReady && _wt2.IsReady;

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

            _esa = new ExponentialMovingAverage(channelPeriod);
            _d = new ExponentialMovingAverage(channelPeriod);
            _ci = new ExponentialMovingAverage(averagePeriod);
            WaveTrend = _ci;
            _wt2 = new SimpleMovingAverage(smoothPeriod);

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
            var hlc3 = (input.High + input.Low + input.Close) / 3m;

            _esa.Update(input.Time, hlc3);
            if (!_esa.IsReady) return 0m;

            var absDistance = Math.Abs(hlc3 - _esa.Current.Value);
            _d.Update(input.Time, absDistance);
            if (!_d.IsReady) return 0m;

            var ciValue = _d.Current.Value == 0 ? 0 :
                (hlc3 - _esa.Current.Value) / (_channelMultiplier * _d.Current.Value);
            _ci.Update(input.Time, ciValue);

            _wt2.Update(input.Time, _ci.Current.Value);

            return _ci.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _esa.Reset();
            _d.Reset();
            _ci.Reset();
            _wt2.Reset();
            base.Reset();
        }
    }
}
