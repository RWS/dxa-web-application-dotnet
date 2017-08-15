//using System;
//using System.Web.Caching;
//using System.Threading;
//using DD4T.Factories;
//using DD4T.ContentModel.Factories;

//namespace DD4T.Mvc.Caching
//{
//    public class SchemaComponentsCacheDependency : CacheDependency
//    {
//        private Timer timer;

//        public string[] SchemaUris { get; private set; }

//        public DateTime LastPublishDate { get; private set; }

//        public SchemaComponentsCacheDependency(int pollTime, string[] schemaUris)
//        {
//            timer = new Timer(
//                new TimerCallback(CheckDependencyCallback),
//                this, 0, pollTime);
//            SchemaUris = schemaUris;
//            IComponentFactory componentFactory = new ComponentFactory();
//            LastPublishDate = DateTime.Now; // TODO: get real last publish date (see line below)
//            // LastPublishDate = componentFactory.LastPublished(schemaUris);
//        }

//        private void CheckDependencyCallback(object sender)
//        {
//            IComponentFactory componentFactory = new ComponentFactory();
//            DateTime lastPublishedDate = DateTime.Now; // TODO: get real last publish date (see line below)
//            // DateTime lastPublishedDate = componentFactory.LastPublished(SchemaUris);
//            if (lastPublishedDate > LastPublishDate)
//            {
//                base.NotifyDependencyChanged(this, EventArgs.Empty);
//                timer.Dispose();
//            }
//        }

//        protected override void DependencyDispose()
//        {
//            timer.Dispose();
//            base.DependencyDispose();
//        }
//    }
//}
