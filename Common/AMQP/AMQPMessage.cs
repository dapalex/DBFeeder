using System;

namespace Common.AMQP
{
    public abstract class AMQPMessage
    {
        public abstract void SetMessageBody(params object[] parameters);
        public override string ToString()
        {
            return "Abstract AMQPMessage";
        }
    }

    public class AMQPUrlMessage : AMQPMessage
    {
        /// <summary>
        /// Details urls
        /// </summary>
        public string url { get; set; }
        public override void SetMessageBody(params object[] parameters)
        {
            this.url = (string) parameters[0];
        }

        public override string ToString()
        {
            return url;
        }
    }

    public class AMQPEntityMessage : AMQPMessage
    {
        /// <summary>
        /// Details urls
        /// </summary>
        public Dictionary<string, string> EntityData { get; set; }
        public override void SetMessageBody(params object[] parameters)
        {
            this.EntityData = (Dictionary<string, string>) parameters[0];
        }

        public override string ToString()
        {
            string retString = string.Empty;

            foreach (KeyValuePair<string, string> kvp in EntityData)
                retString += kvp.Key + ": " + kvp.Value + Environment.NewLine;

            return retString;
        }
    }
}
