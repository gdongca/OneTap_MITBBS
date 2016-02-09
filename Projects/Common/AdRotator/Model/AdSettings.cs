using System;
using System.Net;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
#if WINDOWS_PHONE
using System.Windows;
#endif

namespace AdRotator
{
    /// <summary>
    /// Class storing the list of <see cref="AdCultureDescriptor"/>s.
    /// </summary>
    public class AdSettings
    {
        /// <summary>
        /// String to identify the default culture
        /// </summary>
        public const string DEFAULT_CULTURE = "default";

        /// <summary>
        /// The list of the culture descriptors
        /// </summary>
        public List<AdCultureDescriptor> CultureDescriptors { get; set; }
    }
}
