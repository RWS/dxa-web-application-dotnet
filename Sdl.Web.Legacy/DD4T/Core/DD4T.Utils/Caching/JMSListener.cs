using Apache.NMS;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GreenZoo
{
    public class JMSListener
    {
        private static readonly int _defaultNumberOfRetriesToConnect = 10; 
        private static int _numberOfRetriesToConnect = -1;
        private static readonly int _defaultSecondsBetweenRetries = 10;
        private static int _secondsBetweenRetries = -1;
        private static string _hostname;
        private static int _port;
        private static string _topic;
        private ILogger Logger { get; set; }
        private List<ICacheAgent> CacheAgents;
        private int NumberOfRetries { get; set; }

        public JMSListener(ILogger logger, ICacheAgent cacheAgent)
        {
            CacheAgents = new List<ICacheAgent>();
            CacheAgents.Add(cacheAgent);
            Logger = logger;
        }

        protected static int NumberOfRetriesToConnect
        {
            get
            {
                if (_numberOfRetriesToConnect == -1)
                {
                    string s = ConfigurationManager.AppSettings["DD4T.JMS.NumberOfRetriesToConnect"];
                    if (string.IsNullOrEmpty(s))
                    {
                        _numberOfRetriesToConnect = _defaultNumberOfRetriesToConnect;
                    }
                    else
                    {
                        _numberOfRetriesToConnect = Convert.ToInt32(s);
                    }
                }
                return _numberOfRetriesToConnect;
            }
        }

        
        protected static int SecondsBetweenRetries
        {
            get
            {
                if (_secondsBetweenRetries == -1)
                {
                    string s = ConfigurationManager.AppSettings["DD4T.JMS.SecondsBetweenRetries"];
                    if (string.IsNullOrEmpty(s))
                    {
                        _secondsBetweenRetries = _defaultSecondsBetweenRetries;
                    }
                    else
                    {
                        _secondsBetweenRetries = Convert.ToInt32(s);
                    }
                }
                return _secondsBetweenRetries;
            }
        }

        private static string JMSHostname
        {
            get
            {
                if (_hostname == null)
                {
                    _hostname = ConfigurationManager.AppSettings["DD4T.JMS.Hostname"];
                }
                return _hostname;
            }
        }
        private static int JMSPort
        {
            get
            {
                if (_port == 0)
                {
                    _port = Convert.ToInt32(ConfigurationManager.AppSettings["DD4T.JMS.Port"]);
                }
                return _port;
            }
        }
        private static string JMSTopic
        {
            get
            {
                if (_topic == null)
                {
                    _topic = ConfigurationManager.AppSettings["DD4T.JMS.Topic"];
                }
                return _topic;
            }
        }

       

        public void SubscribeCacheAgent(ICacheAgent cacheAgent)
        {
            CacheAgents.Add(cacheAgent);
        }

        public void Start()
        {
            Thread worker = new Thread(DoWork);
            worker.IsBackground = true;
            worker.Start();
        }
        private void DoWork()
        {

            try
            {
                StartConnection();
            }
            catch (NMSConnectionException e)
            {
                Debug.WriteLine("Unable to connect to ActiveMq. {0}", e.ToString());
            }
            catch (NMSException e)
            {
                Debug.WriteLine("Unable to connect to ActiveMq. {0}", e.ToString());
            }
            finally
            {
                if (NumberOfRetries < NumberOfRetriesToConnect)
                {
                    NumberOfRetries++;
                    Debug.WriteLine("Trying to reconnect to JMS server in {0} seconds.... This is attempt {1} of {2}", SecondsBetweenRetries, NumberOfRetries, NumberOfRetriesToConnect);
                    Thread.Sleep(SecondsBetweenRetries * 1000); //Wait a couple of seconds before trying to reconnect
                    DoWork();
                }
                else
                {
                    Debug.WriteLine("Tried {0} times to connect to ActiveMq, but no luck. Restart the ActiveMq server and then restart the DPS.", NumberOfRetries);
                    Debug.WriteLine("The DPS will continue to serve content, but it will NOT invalidate the caches.");
                }
            }



        }
        private void StartConnection()
        {
            Uri connecturi = new Uri(string.Format("activemq:tcp://{0}:{1}", JMSHostname, JMSPort));

            //Console.WriteLine("About to connect to " + connecturi);

            // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
            IConnectionFactory factory = new NMSConnectionFactory(connecturi);

            using (IConnection connection = factory.CreateConnection())
            {
                connection.ClientId = "DD4TJMSListener-" + Guid.NewGuid().ToString();
                connection.ExceptionListener += connection_ExceptionListener;
                connection.Start();
                using (ISession session = connection.CreateSession(AcknowledgementMode.ClientAcknowledge))
                {
   
                    //IDestination destination = session.GetDestination(Topic);
                    IDestination destination = session.GetTopic(JMSTopic);
                    Logger.Debug("using destination: " + destination);
                    Debug.WriteLine("using destination: " + destination);

                    using (IMessageConsumer consumer = session.CreateConsumer(destination))
                    {
                        IMessage message;

                        try
                        {
                            while ((message = consumer.Receive(ReceiveTimeout)) != null)
                            {
                                OnInvalidate(message);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Error receiving messages: {0}", e.ToString());
                        }
                    }

                    //// Create a consumer and producer
                    //using (IMessageConsumer consumer = session.CreateDurableConsumer(topic, "DD4TJMSConsumer", "2 > 1", false))
                    //{
                    //    // Start the connection so that messages will be processed.
                    //    connection.Start();
                    //    consumer.Listener += new MessageListener(OnInvalidate);
                    //}
                    //// stay in this thread to keep the connection to the JMS server open
                    //// This method should only be called from a background thread!
                    //while (true)
                    //{

                    //}

                }
            }
        }


        private void connection_ExceptionListener(Exception exception)
        {
            Logger.Error("something is wrong with the JMS connection");
            Logger.Error("Exception: {0}", exception);
        }

        protected void OnInvalidate(IMessage receivedMsg)
        {
           
            IObjectMessage m2 = receivedMsg as IObjectMessage;
            Debug.WriteLine("message has the following raw content:");
           // Debug.WriteLine(m2.)
            try
            {
                object obj = m2.Body;
            }
            catch (Exception e)
            {
                Debug.WriteLine("caught exception while accessing message body: " + e.Message);
            }
            object obj2 = m2.ToObject();

            var msg = receivedMsg as IBytesMessage;
            var result = msg != null ? new UTF8Encoding().GetString(msg.Content) : "bytes message is null";

            ITextMessage message = receivedMsg as ITextMessage;
            if (message == null)
            {
                Logger.Debug(string.Format("message with id {0} is not a text message", receivedMsg.NMSMessageId));
                Debug.WriteLine(string.Format("message with id {0} is not a text message", receivedMsg.NMSMessageId));
                receivedMsg.Acknowledge();
                return;
            }

            Logger.Debug(string.Format("received text message with id {0} and text {1}", message.NMSMessageId, message.Text));
            Debug.WriteLine(string.Format("received text message with id {0} and text {1}", message.NMSMessageId, message.Text));

            try
            {
                var invalidationInformation = GetInvalidationInformation(message);
                foreach (string dcpUri in invalidationInformation.InvalidationDcps) 
                {
                    foreach (ICacheAgent cacheAgent in CacheAgents)
                    {
                        Logger.Debug(string.Format("telling cache agent {0} to remove DCP with uri {1}", cacheAgent.GetType().Name, dcpUri));
                        //cacheAgent.Remove(dcpUri)
                    }
                }
                foreach (InvalidationUrl url in invalidationInformation.InvalidationUrls)
                {
                    foreach (ICacheAgent cacheAgent in CacheAgents)
                    {
                        Logger.Debug(string.Format("telling cache agent {0} to remove page with url {1}", cacheAgent.GetType().Name, url.TcmUri));
                        //cacheAgent.Remove(url.TcmUri)
                    }
                }

                //LoggerService.Debug("<<Received message: finished processing");
            }
            catch (Exception e)
            {
                Logger.Error("error in invalidation transaction: {0}", e.ToString());
            }
            finally
            {
                message.Acknowledge();
                Logger.Debug(string.Format("acknowledged received message with id {0}", message.NMSMessageId));
            }
        }

        private InvalidationInformation GetInvalidationInformation(ITextMessage message)
        {
            XElement xml = null;
            try
            {
                xml = XElement.Parse(message.Text);
            }
            catch (Exception)
            {
                Logger.Error("Unable to parse invalidation xml!. Returning an empty collection. Nothing is invalidated!");
                //throw new Exception();
                return new InvalidationInformation();
            }

            var invalidationInformation = new InvalidationInformation();
            //Get all pages to invalidate
            Logger.Debug(string.Format("XML invalidation message: ", xml.ToString()));
            var allPageUrls = xml.Descendants().Where(e => e.Name.LocalName.Equals("url")).ToList();
            if (allPageUrls.Any())
            {
                var urlInfo = allPageUrls.Select(urlElem => new InvalidationUrl { Url = urlElem.Value, PublicationId = int.Parse(urlElem.Attribute("pubId").Value), TcmUri = string.Format("tcm:{0}-{1}-64", urlElem.Attribute("pubId").Value, urlElem.Attribute("itemId").Value) }).Cast<IInvalidationUrl>().ToList();
                //return new InvalidationInformation { InvalidationUrls = urlInfo };
                invalidationInformation.InvalidationUrls = urlInfo;
            }

            return invalidationInformation;
        }

        public TimeSpan ReceiveTimeout
        {
            get
            {
                return TimeSpan.FromDays(2);
            }
        }
    }

    public interface ICacheInvalidationInformation
    {
        IList<string> InvalidationDcps { get; set; }
        IList<IInvalidationUrl> InvalidationUrls { get; set; }
    }

    public interface IInvalidationUrl
    {
        string Url { get; set; }
        int PublicationId { get; set; }
        string TcmUri { get; set; }
    }

    public class InvalidationInformation : ICacheInvalidationInformation
    {
        private IList<IInvalidationUrl> invalidationUrls = new List<IInvalidationUrl>();
        public IList<IInvalidationUrl> InvalidationUrls { get { return invalidationUrls; } set { invalidationUrls = value; } }

        private IList<string> invalidationDcps = new List<string>();
        public IList<string> InvalidationDcps
        {
            get
            {
                return invalidationDcps;
            }
            set
            {
                invalidationDcps = value;
            }
        }

    }

    public class InvalidationUrl : IInvalidationUrl
    {
        public int PublicationId
        {
            get;
            set;
        }

        public string TcmUri
        {
            get;
            set;
        }

        public string Url
        {
            get;
            set;
        }
    }
}
