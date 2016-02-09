using System;
using System.Net;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AdRotator
{

    public class AdCultureDescriptor
    {
        [XmlAttribute("CultureName")]
        /// <summary>
        /// The name of the culture, e.g. en-US
        /// </summary>
        public string CultureName { get; set; }

        [XmlElement("Probabilities")]
        /// <summary>
        /// Listing of the probabilities for ads
        /// </summary>
        public List<AdProbability> AdProbabilities { get; set; }
    }
}
