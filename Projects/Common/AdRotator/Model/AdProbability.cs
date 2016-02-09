using System;
using System.Net;
using System.Xml.Serialization;
using System.ComponentModel;

namespace AdRotator
{
    /// <summary>
    /// Describes the probability of and ad with AdType appearing.
    /// </summary>
    public class AdProbability:INotifyPropertyChanged
    {
        private double _probabilityValue;

        private double _totalProbabilityValues;

        private AdType _adType;

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlAttribute("Probability")]
        /// <summary>
        /// The probability to show the ad. This can be any double, though for simplicity
        ///     reasons it's advised that all Probability values in <see cref='AdCultureDescriptor'/> 
        ///     add up to 100.
        /// </summary>
        public double ProbabilityValue
        {
            get
            {
                return _probabilityValue;
            }
            set
            {
                var oldValue = _probabilityValue;
                _probabilityValue = value;
                if (_probabilityValue != oldValue)
                {
                    OnPropertyChanged("Probability");
                }
            }
        }

        public double ProbabilityPercentage
        {
            get
            {
                if (TotalProbabilityValues == 0)
                {
                    return 0;
                }
                return (ProbabilityValue / TotalProbabilityValues) * 100;
            }
        }

        [XmlIgnore]
        /// <summary>
        /// The probability values of all the ads that this ad might appear together with.
        /// </summary>
        public double TotalProbabilityValues
        {
            get
            {
                return _totalProbabilityValues;
            }
            set
            {
                var oldValue = _totalProbabilityValues;
                _totalProbabilityValues = value;
                if (_totalProbabilityValues != oldValue)
                {
                    OnPropertyChanged("TotalProbabilityValues");
                }
            }
        }

        [XmlAttribute("AdType")]
        /// <summary>
        /// The type of the ad that <see cref='ProbabilityValue'/> is associated with.
        /// </summary>
        public AdType AdType
        {
            get
            {
                return _adType;
            }
            set
            {
                var oldValue = _adType;
                _adType = value;
                if (_adType != oldValue)
                {
                    OnPropertyChanged("AdType");
                }
            }
        }

        public AdProbability()
        {
        }

        public AdProbability(AdType adType, int probability)
        {
            AdType = adType;
            ProbabilityValue = probability;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
