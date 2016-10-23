using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Track.Models
{
    public class Task
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public double Duration { get; set; }

        [DataMember]
        public string Color { get; set; }

        [XmlIgnore]
        public bool Active { get; set; }

        [XmlIgnore]
        public string DurationHours
        {
            get { return (Duration/60.0/60.0).ToString("0.0"); }
        }
    }
}
