using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace Track.Models
{
    public class Session
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly Timer timer;
        [DataMember] public DateTime Today = DateTime.Today;
        private Task activeTask;
        private TimeSpan rememberedDuration;

        public Session()
        {
            Tasks = new List<Task>();
            timer = new Timer(100);
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            stopwatch.Start();
        }

        [DataMember]
        public TimeSpan TotalDuration
        {
            get { return stopwatch.Elapsed; }
        }

        [DataMember]
        public List<Task> Tasks { get; set; }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (activeTask != null)
            {
                activeTask.Duration = activeTask.Duration += (stopwatch.Elapsed - rememberedDuration).TotalSeconds;
            }
            rememberedDuration = stopwatch.Elapsed;
        }

        public event EventHandler TaskAdded;

        public void AddTask(string name)
        {
            name = name.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (Tasks.All(t => t.Name.ToLower() != name.ToLower()))
                {
                    Tasks.Add(new Task {Name = name});
                    if (TaskAdded != null)
                    {
                        TaskAdded(Tasks[Tasks.Count - 1], null);
                    }
                }
            }
        }

        public void Start(string name)
        {
            if (activeTask != null)
            {
                activeTask.Active = false;
            }
            foreach (Task task in Tasks)
            {
                if (task.Name == name)
                {
                    activeTask = task;
                    activeTask.Active = true;
                    break;
                }
            }
        }

        public void Stop()
        {
            if (activeTask != null)
            {
                activeTask.Active = false;
            }
            activeTask = null;
        }

        public string Serialize()
        {
            var stream1 = new MemoryStream();
            var textWriter = new StringWriter();

            var xmlSerializer = new XmlSerializer(typeof (Session));
            var ser = new DataContractJsonSerializer(typeof (Session));
            xmlSerializer.Serialize(textWriter, this);
            return textWriter.ToString();
        }

        public static Session Deserialize(string serialized)
        {
            var xmlSerializer = new XmlSerializer(typeof (Session));

            Session deserialized = null;
            using (TextReader reader = new StringReader(serialized))
            {
                try
                {
                    deserialized = xmlSerializer.Deserialize(reader) as Session;                    
                }
                catch
                {
                }
            }

            return deserialized;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var task in Tasks)
            {
                sb.Append(" " + task.Name + ": " + task.DurationHours + ";");
            }
            return sb.ToString();
        }
    }
}